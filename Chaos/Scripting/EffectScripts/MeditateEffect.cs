#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts.Abstractions;
using Chaos.Time;
using Chaos.Time.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     A stand-still channel: restores MP over time and buffs spell damage, but breaks immediately if the caster
///     moves - see the <see cref="Update" /> override, which compares <see cref="Chaos.Collections.Trackers.LastWalk" />
///     against a snapshot taken in <see cref="OnApplied" /> every tick, ahead of the normal MP-tick interval. One of
///     the Martial Artist's 2 shared evolving abilities (alongside Martial Form) - <see cref="MpPerTick" /> and
///     <see cref="FlatSpellDamageBonus" /> are set by MeditateScript before applying (extended from the original
///     flat version's hardcoded constants), floor-gated I/II/III/IV per ELYSIUM_CLASS_DESIGN.md's combined
///     Meditate+Form table (Meditate caps at IV, no Floor10 change - it's already maxed by Floor8).
/// </summary>
public sealed class MeditateEffect : IntervalEffectBase
{
    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 14,
        AnimationSpeed = 100
    };

    private bool WasInterrupted;
    private DateTime? WalkSnapshot;

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(10000);

    /// <inheritdoc />
    protected override IIntervalTimer Interval { get; } = new IntervalTimer(TimeSpan.FromMilliseconds(2000));

    /// <summary>
    ///     The bonus applied to FlatSpellDamage while meditating
    /// </summary>
    public int FlatSpellDamageBonus { get; set; } = 15;

    /// <summary>
    ///     The amount of MP restored per tick
    /// </summary>
    public int MpPerTick { get; set; } = 20;

    /// <inheritdoc />
    public override byte Icon => 52;

    /// <inheritdoc />
    public override string Name => "Meditate";

    /// <inheritdoc />
    public override bool ShouldApply(Creature source, Creature target)
    {
        if (target.Effects.TryGetEffect("Martial Form", out _))
        {
            if (source is Aisling aisling)
                aisling.SendActiveMessage("You cannot meditate while transformed.");

            return false;
        }

        return base.ShouldApply(source, target);
    }

    /// <inheritdoc />
    public override void OnApplied()
    {
        WalkSnapshot = Subject.Trackers.LastWalk;

        Subject.StatSheet.AddBonus(new Attributes { FlatSpellDamage = FlatSpellDamageBonus });
        AislingSubject?.SendActiveMessage("You sit and begin meditating, restoring your Chi.");
    }

    /// <inheritdoc />
    public override void OnTerminated()
    {
        Subject.StatSheet.SubtractBonus(new Attributes { FlatSpellDamage = FlatSpellDamageBonus });

        AislingSubject?.SendActiveMessage(
            WasInterrupted ? "Your meditation is broken." : "Your meditation ends, your mind clear.");
    }

    /// <inheritdoc />
    protected override void OnIntervalElapsed()
    {
        if (!Subject.IsAlive)
            return;

        Subject.StatSheet.AddMp(MpPerTick);
        AislingSubject?.Client.SendAttributes(StatUpdateType.Vitality);
        Subject.Animate(ApplyAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (Subject.Trackers.LastWalk != WalkSnapshot)
        {
            WasInterrupted = true;
            Remaining = TimeSpan.Zero;

            return;
        }

        base.Update(delta);
    }
}
