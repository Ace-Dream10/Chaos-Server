#region
using Chaos.Collections;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     Same delayed-callback pattern as Rain of Arrows, but each hit lands on a random tile within range of the
///     center point instead of a fixed target - a scattered barrage rather than a focused volley.
/// </summary>
public class RandomAreaBarrageScript : ConfigurableSpellScriptBase
{
    private readonly List<PendingHit> PendingHits = [];

    /// <inheritdoc />
    public RandomAreaBarrageScript(Spell subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var origin = context.TargetPoint;

        source.AnimateBody(BodyAnimation);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, origin);

        for (var i = 0; i < HitCount; i++)
            PendingHits.Add(
                new PendingHit(
                    source,
                    map,
                    origin,
                    TimeSpan.FromMilliseconds(TotalDurationMs / (double)HitCount * (i + 1))));
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
                ExecuteHit(pending);
        }
    }

    private void ExecuteHit(PendingHit pending)
    {
        var map = pending.Map;
        var source = pending.Source;

        var candidatePoints = pending.Origin
                                     .SpiralSearch(Range)
                                     .ToArray();

        var point = candidatePoints[Random.Shared.Next(candidatePoints.Length)];

        if (Animation != null)
            map.ShowAnimation(Animation.GetPointAnimation(point, source.Id));

        var target = map.GetEntitiesAtPoints<Creature>(point)
                        .TopOrDefault();

        if ((target == null) || !Filter.IsValidTarget(source, target))
            return;

        var damage = (BaseDamage ?? 0)
                     + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.INT) * (DamageStatMultiplier ?? 1));

        if (damage > 0)
            ApplyDamageScript.ApplyDamage(source, target, this, damage, Element);

        if ((SlowDurationMs > 0) && target.IsAlive)
        {
            var slowEffect = new SlowEffect();
            slowEffect.SetDuration(TimeSpan.FromMilliseconds(SlowDurationMs));
            target.Effects.Apply(source, slowEffect, this);
        }
    }

    private sealed class PendingHit(Creature source, MapInstance map, Point origin, TimeSpan remaining)
    {
        public MapInstance Map { get; } = map;
        public Point Origin { get; } = origin;
        public TimeSpan Remaining { get; set; } = remaining;
        public Creature Source { get; } = source;
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each hit tile
    /// </summary>
    public Animation? Animation { get; init; }

    public IApplyDamageScript ApplyDamageScript { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt per hit
    /// </summary>
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster on cast
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
    ///     The element of the damage dealt
    /// </summary>
    public Element? Element { get; init; }

    /// <summary>
    ///     The filter used to determine whether a creature on a randomly-chosen tile is a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     How many hits land total, spread evenly across <see cref="TotalDurationMs" />
    /// </summary>
    public int HitCount { get; init; } = 8;

    /// <summary>
    ///     The radius, around the center point, within which each hit's tile is randomly chosen
    /// </summary>
    public int Range { get; init; } = 4;

    /// <summary>
    ///     How long, in milliseconds, hit creatures are slowed for. 0 = no slow
    /// </summary>
    public int SlowDurationMs { get; init; }

    /// <summary>
    ///     Sound played once, on cast
    /// </summary>
    public byte? Sound { get; init; }

    /// <summary>
    ///     How long, in milliseconds, the barrage lasts from first hit to last
    /// </summary>
    public int TotalDurationMs { get; init; } = 5000;
    #endregion
}
