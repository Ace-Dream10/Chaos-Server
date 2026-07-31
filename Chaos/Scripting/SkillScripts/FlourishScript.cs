#region
using Chaos.Collections;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     A rapid series of forward strikes on the tile directly in front of the caster. Unlike Cyclone, facing never
///     changes and the caster isn't locked in place - it's a fast fencing combo, not a channel.
/// </summary>
public class FlourishScript : ConfigurableSkillScriptBase
{
    private readonly List<PendingHit> PendingHits = [];

    /// <inheritdoc />
    public FlourishScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, source);

        ExecuteHit(source, map);

        for (var i = 1; i < HitCount; i++)
            PendingHits.Add(new PendingHit(source, map, TimeSpan.FromMilliseconds(HitIntervalMs * i)));
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (PendingHits.Count == 0)
            return;

        foreach (var pending in PendingHits)
            pending.Remaining -= delta;

        while ((PendingHits.Count > 0) && (PendingHits[0].Remaining <= TimeSpan.Zero))
        {
            var pending = PendingHits[0];
            PendingHits.RemoveAt(0);

            if (pending.Source.IsAlive)
                ExecuteHit(pending.Source, pending.Map);
        }
    }

    private void ExecuteHit(Creature source, MapInstance map)
    {
        var targetPoint = source.DirectionalOffset(source.Direction);
        var target = map.GetEntitiesAtPoints<Creature>(targetPoint).TopOrDefault();

        if ((target == null) || !Filter.IsValidTarget(source, target))
            return;

        var damage = (BaseDamage ?? 0)
                     + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.STR) * (DamageStatMultiplier ?? 1));

        if (damage > 0)
            ApplyDamageScript.ApplyDamage(source, target, this, damage);

        if (Animation != null)
            target.Animate(Animation, source.Id);
    }

    private sealed class PendingHit(Creature source, MapInstance map, TimeSpan remaining)
    {
        public MapInstance Map { get; } = map;
        public TimeSpan Remaining { get; set; } = remaining;
        public Creature Source { get; } = source;
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the target on each hit
    /// </summary>
    public Animation? Animation { get; init; }

    public IApplyDamageScript ApplyDamageScript { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt per hit
    /// </summary>
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster when the skill is used
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The stat used to scale bonus damage
    /// </summary>
    public Stat? DamageStat { get; init; }

    /// <summary>
    ///     The multiplier applied to <see cref="DamageStat" /> when calculating bonus damage per hit
    /// </summary>
    public decimal? DamageStatMultiplier { get; init; }

    /// <summary>
    ///     The filter used to determine whether the tile directly in front of the caster holds a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     How many hits land total, including the immediate first one
    /// </summary>
    public int HitCount { get; init; } = 5;

    /// <summary>
    ///     The number of milliseconds between each hit
    /// </summary>
    public int HitIntervalMs { get; init; } = 150;

    /// <summary>
    ///     Sound played once, on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
