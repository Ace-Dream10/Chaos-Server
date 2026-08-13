#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Scripting.AislingScripts.Abstractions;
using Chaos.Scripting.EffectScripts;
#endregion

namespace Chaos.Scripting.AislingScripts;

/// <summary>
///     For Assassins, builds kill energy (stored as MP) from landing killing blows on monsters. No passive decay -
///     it's banked until it auto-triggers Bloodlust. Hard-capped at <see cref="HardCap" /> regardless of the
///     character's real max mp (set directly via SetMp, bypassing the normal AddMp/EffectiveMaximumMp clamp
///     entirely), so a bad max-mp stat from gear or anything else can never let this exceed the cap. No visual
///     while just banking kill energy - the aura only pulses once Bloodlust is actually active, so it doesn't sit
///     on the player constantly any time they have leftover energy banked. Also neutralizes the game's normal
///     passive HP/MP regeneration (<see cref="Chaos.Formulae.Regen.DefaultRegenFormula" />, which ticks MP for
///     every creature that isn't at full HP) for the MP stat specifically, since Assassins repurpose MP as kill
///     energy and it should only ever change from kills or being auto-spent on Bloodlust - otherwise Bloodlust
///     could activate off of ordinary regen alone, without any kills.
/// </summary>
/// <remarks>
///     Bloodlust itself was reworked this session from a manually-cast active (spend banked energy, duration
///     proportional to the amount spent) into a true always-on passive, per the locked design's actual wording -
///     "kills generate Bloodlust... upon reaching maximum Bloodlust, you automatically enter a killing frenzy."
///     The underlying kill-energy resource system below (banking, hard cap, the Execute-cooldown-bypass hook) is
///     exactly the "already built, reusable" mechanic the class design doc calls out - only the trigger changed:
///     it now fires itself the instant the bank hits <see cref="HardCap" />, instead of waiting for a manual
///     Bloodlust cast. <see cref="AutoTriggerDurationMs" /> is a placeholder, not balance-tested.
/// </remarks>
public class AssassinFrenzyScript : AislingScriptBase
{
    private const int AutoTriggerDurationMs = 6000;
    private const int EnergyPerKill = 20;
    private const int HardCap = 100;
    private static readonly TimeSpan AuraPulseInterval = TimeSpan.FromMilliseconds(1000);

    private static readonly Animation BuildingAura = new()
    {
        TargetAnimation = 250,
        AnimationSpeed = 100
    };

    private static readonly Animation ReadyAura = new()
    {
        TargetAnimation = 251,
        AnimationSpeed = 100
    };

    private int? LastKnownMp;
    private DateTime? LastObservedKillTime;
    private TimeSpan SinceLastAuraPulse = TimeSpan.Zero;

    /// <inheritdoc />
    public AssassinFrenzyScript(Aisling subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (Subject.UserStatSheet.BaseClass != BaseClass.Assassin)
        {
            LastKnownMp = null;

            return;
        }

        var lastKillTime = Subject.Trackers.LastKillTime;
        var killedSinceLastTick = lastKillTime.HasValue && (lastKillTime != LastObservedKillTime);

        if (killedSinceLastTick)
        {
            LastObservedKillTime = lastKillTime;

            //Bloodlust lets Execute chain infinitely as long as kills keep landing. This has to happen here, on a
            //later tick after Skill.Use() has already finished, rather than inside ExecuteScript.OnUse itself -
            //Skill.Use() unconditionally calls BeginCooldown() right after OnUse returns, which would immediately
            //re-establish a fresh cooldown and undo any Elapsed reset attempted from within OnUse.
            if (Subject.Effects.Contains("Bloodlust")
                && string.Equals(Subject.Trackers.LastUsedSkill?.Template.TemplateKey, "execute", StringComparison.OrdinalIgnoreCase)
                && Subject.SkillBook.TryGetObjectByTemplateKey("execute", out var executeSkill))
            {
                //Elapsed = null alone satisfies server-side CanUse(), but the client was already sent a full
                //12-second cooldown packet from the original use and nothing corrects that visually. Push Elapsed
                //just past Cooldown instead (still satisfies CanUse() via the Elapsed > Cooldown branch) and
                //re-send the cooldown packet, which now computes ~0 seconds remaining and clears the client's
                //cooldown display. SendCooldown() itself no-ops when Elapsed is null, so that route can't do this.
                executeSkill.Elapsed = (executeSkill.Cooldown ?? TimeSpan.Zero) + TimeSpan.FromMilliseconds(1);
                Subject.Client.SendCooldown(executeSkill);
            }
        }

        var currentMp = Subject.StatSheet.CurrentMp;

        //neutralize passive regen (or anything else) that crept mp up without a kill - also covers the very
        //FIRST tick after becoming an Assassin (LastKnownMp not yet set): the old `LastKnownMp.HasValue &&` guard
        //skipped this check entirely on that first tick, letting one round of ordinary passive regen slip through
        //and get silently adopted as "banked kill energy" once LastKnownMp was set below - confirmed root cause
        //of "Assassin starts with ~20 MP for no reason".
        if (!killedSinceLastTick && (currentMp > (LastKnownMp ?? 0)))
            Subject.StatSheet.SubtractMp(currentMp - (LastKnownMp ?? 0));

        if (killedSinceLastTick)
            Subject.StatSheet.SetMp(Math.Min(Subject.StatSheet.CurrentMp + EnergyPerKill, HardCap));

        //Bloodlust is a true always-on passive - it triggers itself the instant banked kill energy hits the hard
        //cap, consuming all of it, rather than waiting for a manual cast
        if ((Subject.StatSheet.CurrentMp >= HardCap) && !Subject.Effects.Contains("Bloodlust") && Subject.SkillBook.TryGetObjectByTemplateKey("bloodlust", out _))
        {
            var frenzyEffect = new BloodlustEffect();
            frenzyEffect.SetDuration(TimeSpan.FromMilliseconds(AutoTriggerDurationMs));
            Subject.Effects.Apply(Subject, frenzyEffect, this);
            Subject.StatSheet.SetMp(0);
        }

        var finalMp = Subject.StatSheet.CurrentMp;

        if (finalMp != LastKnownMp)
            Subject.Client.SendAttributes(StatUpdateType.Vitality);

        LastKnownMp = finalMp;

        UpdateAuraVisual(delta);
    }

    private void UpdateAuraVisual(TimeSpan delta)
    {
        //no aura while just banking kill energy - only while Bloodlust itself is active
        if (!Subject.Effects.Contains("Bloodlust"))
            return;

        SinceLastAuraPulse += delta;

        if (SinceLastAuraPulse < AuraPulseInterval)
            return;

        SinceLastAuraPulse = TimeSpan.Zero;

        var currentMp = Subject.StatSheet.CurrentMp;

        if (currentMp >= HardCap)
            Subject.Animate(ReadyAura, Subject.Id);
        else if (currentMp > 0)
            Subject.Animate(BuildingAura, Subject.Id);
    }
}
