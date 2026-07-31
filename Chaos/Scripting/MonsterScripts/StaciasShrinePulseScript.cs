#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.MonsterScripts.Abstractions;
#endregion

namespace Chaos.Scripting.MonsterScripts;

/// <summary>
///     Evolves based on the caster's stacias_shrine spell level (set by StaciasShrineScript right after spawning):
///     level 1-3 just heals, 4-6 adds a brief FlatSkillDamage buff per pulse, 7-9 also adds a brief damage shield,
///     and 10 also adds a brief AtkSpeedPct buff. The buff/shield window is tracked per-Aisling in
///     <see cref="ActiveBuffs" /> and cleared after <see cref="BuffDurationMs" /> regardless of whether the shield
///     was consumed, since it's meant to be a brief per-pulse boost, not a persistent one.
/// </summary>
public class StaciasShrinePulseScript : ConfigurableMonsterScriptBase
{
    private readonly List<ActiveBuff> ActiveBuffs = [];
    private TimeSpan Elapsed;
    private TimeSpan SinceLastPulse;

    /// <inheritdoc />
    public StaciasShrinePulseScript(Monster subject)
        : base(subject)
        => ApplyHealScript = FunctionalScripts.ApplyHealing.ApplyHealScript.Create();

    private IApplyHealScript ApplyHealScript { get; }

    /// <summary>
    ///     The creature that summoned the shrine - used to evaluate the friendly-target filter. Set by the summoning
    ///     script right after the shrine is spawned.
    /// </summary>
    public Creature? Caster { get; set; }

    /// <summary>
    ///     The level of the caster's stacias_shrine spell at the time it was cast - determines which tier of pulse
    ///     effects apply. Set by the summoning script right after the shrine is spawned.
    /// </summary>
    public int SpellLevel { get; set; } = 1;

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        UpdateActiveBuffs(delta);

        Elapsed += delta;
        SinceLastPulse += delta;

        if (Elapsed >= TimeSpan.FromMilliseconds(DurationMs))
            return;

        if (SinceLastPulse < TimeSpan.FromMilliseconds(PulseIntervalMs))
            return;

        SinceLastPulse = TimeSpan.Zero;

        if (Animation != null)
            Subject.Animate(Animation);

        if (Caster == null)
            return;

        var tier = SpellLevel switch
        {
            <= 3  => 1,
            <= 6  => 2,
            <= 9  => 3,
            _     => 4
        };

        foreach (var aisling in Subject.MapInstance.GetEntitiesWithinRange<Aisling>(Subject, HealRange))
        {
            if (!Filter.IsValidTarget(Caster, aisling))
                continue;

            ApplyHealScript.ApplyHeal(Caster, aisling, this, HealPerTick);

            if (tier >= 2)
                ApplyTieredBuff(aisling, tier);
        }
    }

    private void ApplyTieredBuff(Aisling aisling, int tier)
    {
        var bonus = new Attributes
        {
            FlatSkillDamage = DamageBuffAmount,
            AtkSpeedPct = tier >= 4 ? AtkSpeedBuffAmount : 0
        };

        aisling.StatSheet.AddBonus(bonus);

        if (tier >= 3)
            aisling.Trackers.Tags[ApplyAttackDamageScript.ShrineShieldTag] = ShieldAmount.ToString();

        ActiveBuffs.Add(new ActiveBuff(aisling, bonus, tier >= 3, TimeSpan.FromMilliseconds(BuffDurationMs)));
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }

    private void UpdateActiveBuffs(TimeSpan delta)
    {
        if (ActiveBuffs.Count == 0)
            return;

        for (var i = ActiveBuffs.Count - 1; i >= 0; i--)
        {
            var buff = ActiveBuffs[i];
            buff.Remaining -= delta;

            if (buff.Remaining > TimeSpan.Zero)
                continue;

            ActiveBuffs.RemoveAt(i);

            buff.Aisling.StatSheet.SubtractBonus(buff.Bonus);

            if (buff.HasShield)
                buff.Aisling.Trackers.Tags.TryRemove(ApplyAttackDamageScript.ShrineShieldTag, out _);

            buff.Aisling.Client.SendAttributes(StatUpdateType.Full);
        }
    }

    private sealed class ActiveBuff(Aisling aisling, Attributes bonus, bool hasShield, TimeSpan remaining)
    {
        public Aisling Aisling { get; } = aisling;
        public Attributes Bonus { get; } = bonus;
        public bool HasShield { get; } = hasShield;
        public TimeSpan Remaining { get; set; } = remaining;
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the shrine on each pulse
    /// </summary>
    public Animation? Animation { get; set; }

    /// <summary>
    ///     The AtkSpeedPct granted per pulse at tier 4 (spell level 10)
    /// </summary>
    public int AtkSpeedBuffAmount { get; set; } = 10;

    /// <summary>
    ///     How long, in milliseconds, the per-pulse damage buff/shield/attack speed buff lasts before clearing
    /// </summary>
    public int BuffDurationMs { get; set; } = 2000;

    /// <summary>
    ///     The FlatSkillDamage granted per pulse at tier 2+ (spell level 4+)
    /// </summary>
    public int DamageBuffAmount { get; set; } = 10;

    /// <summary>
    ///     How long, in milliseconds, the shrine pulses for before going inert (should match decoyExpiration's
    ///     durationMs so it stops pulsing right as it's removed from the map)
    /// </summary>
    public int DurationMs { get; init; } = 5000;

    /// <summary>
    ///     The filter used to determine which nearby Aislings are valid heal targets
    /// </summary>
    public TargetFilter Filter { get; set; }

    /// <summary>
    ///     The amount healed on each pulse
    /// </summary>
    public int HealPerTick { get; set; } = 60;

    /// <summary>
    ///     The radius around the shrine that gets healed on each pulse
    /// </summary>
    public int HealRange { get; set; } = 3;

    /// <summary>
    ///     The number of milliseconds between pulses
    /// </summary>
    public int PulseIntervalMs { get; init; } = 500;

    /// <summary>
    ///     The damage absorbed by the shield granted per pulse at tier 3+ (spell level 7+)
    /// </summary>
    public int ShieldAmount { get; set; } = 50;
    #endregion
}
