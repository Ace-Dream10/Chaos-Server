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
///     Same pulse pattern as <see cref="StaciasShrinePulseScript" />, but damages nearby hostiles instead of
///     healing allies
/// </summary>
public class StaciasJudgmentPulseScript : ConfigurableMonsterScriptBase
{
    private TimeSpan Elapsed;
    private TimeSpan SinceLastPulse;

    /// <inheritdoc />
    public StaciasJudgmentPulseScript(Monster subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    private IApplyDamageScript ApplyDamageScript { get; }

    /// <summary>
    ///     The creature that summoned the shrine - used to evaluate the hostile-target filter and to scale damage
    ///     off of. Set by the summoning script right after the shrine is spawned.
    /// </summary>
    public Creature? Caster { get; set; }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
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

        var damage = BaseDamage + Convert.ToInt32(Caster.StatSheet.GetEffectiveStat(DamageStat) * DamageStatMultiplier);

        foreach (var monster in Subject.MapInstance.GetEntitiesWithinRange<Monster>(Subject, DamageRange))
        {
            if (!monster.IsAlive || !Filter.IsValidTarget(Caster, monster))
                continue;

            ApplyDamageScript.ApplyDamage(Caster, monster, this, damage);
        }
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the shrine on each pulse
    /// </summary>
    public Animation? Animation { get; set; }

    /// <summary>
    ///     The flat portion of the damage dealt per pulse
    /// </summary>
    public int BaseDamage { get; set; } = 40;

    /// <summary>
    ///     The stat used to scale bonus damage
    /// </summary>
    public Stat DamageStat { get; set; } = Stat.WIS;

    /// <summary>
    ///     The multiplier applied to DamageStat when calculating bonus damage
    /// </summary>
    public decimal DamageStatMultiplier { get; set; } = 2;

    /// <summary>
    ///     The radius around the shrine damaged on each pulse
    /// </summary>
    public int DamageRange { get; set; } = 3;

    /// <summary>
    ///     How long, in milliseconds, the shrine pulses for before going inert (should match decoyExpiration's
    ///     durationMs so it stops pulsing right as it's removed from the map)
    /// </summary>
    public int DurationMs { get; set; } = 7000;

    /// <summary>
    ///     The filter used to determine which nearby monsters are valid damage targets
    /// </summary>
    public TargetFilter Filter { get; set; }

    /// <summary>
    ///     The number of milliseconds between pulses
    /// </summary>
    public int PulseIntervalMs { get; set; } = 500;
    #endregion
}
