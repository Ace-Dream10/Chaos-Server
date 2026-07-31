#region
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.MonsterScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.MonsterScripts;

/// <summary>
///     Same pulse pattern as <see cref="StaciasJudgmentPulseScript" />, but reapplies an effect (e.g. Slow) to
///     nearby hostiles instead of dealing damage. Used by ground-hazard spells like Bog.
/// </summary>
public class AreaEffectPulseScript : ConfigurableMonsterScriptBase
{
    private TimeSpan Elapsed;
    private TimeSpan SinceLastPulse;

    /// <inheritdoc />
    public AreaEffectPulseScript(Monster subject, IEffectFactory effectFactory)
        : base(subject)
        => EffectFactory = effectFactory;

    /// <summary>
    ///     The creature that summoned the hazard - used to evaluate the hostile-target filter. Set by the summoning
    ///     script right after the hazard is spawned.
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

        if ((Caster == null) || string.IsNullOrEmpty(EffectKey))
            return;

        foreach (var monster in Subject.MapInstance.GetEntitiesWithinRange<Monster>(Subject, PulseRange))
        {
            if (!monster.IsAlive || !Filter.IsValidTarget(Caster, monster))
                continue;

            var effect = EffectFactory.Create(EffectKey);

            if (EffectDurationMs.HasValue)
                effect.SetDuration(TimeSpan.FromMilliseconds(EffectDurationMs.Value));

            monster.Effects.Apply(Caster, effect, this);
        }
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the hazard on each pulse
    /// </summary>
    public Animation? Animation { get; set; }

    /// <summary>
    ///     How long, in milliseconds, the hazard pulses for before going inert (should match decoyExpiration's
    ///     durationMs so it stops pulsing right as it's removed from the map)
    /// </summary>
    public int DurationMs { get; init; } = 6000;

    /// <summary>
    ///     Optional duration override applied to the created effect
    /// </summary>
    public int? EffectDurationMs { get; set; }

    /// <summary>
    ///     The effect key reapplied to nearby hostiles on each pulse
    /// </summary>
    public string? EffectKey { get; set; }

    public IEffectFactory EffectFactory { get; init; }

    /// <summary>
    ///     The filter used to determine which nearby monsters the pulse affects
    /// </summary>
    public TargetFilter Filter { get; set; }

    /// <summary>
    ///     The number of milliseconds between pulses
    /// </summary>
    public int PulseIntervalMs { get; init; } = 500;

    /// <summary>
    ///     The radius around the hazard affected by each pulse
    /// </summary>
    public int PulseRange { get; set; }
    #endregion
}
