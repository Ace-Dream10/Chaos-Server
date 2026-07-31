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
///     A generic traveling line effect (a tornado/wave/lance that moves tile by tile in the caster's facing
///     direction), damaging whatever it passes over. Optionally applies Slow and/or knocks hit creatures further in
///     the travel direction. Reused for tidal-wave/tornado/lance-style spells that all share this exact shape.
/// </summary>
public class TravelingLineScript : ConfigurableSpellScriptBase
{
    private readonly List<PendingStep> PendingSteps = [];

    /// <inheritdoc />
    public TravelingLineScript(Spell subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, source);

        var endPoint = source.DirectionalOffset(source.Direction, Range);

        var points = source.GetDirectPath(endPoint)
                           .Skip(1)
                           .ToList();

        for (var i = 0; i < points.Count; i++)
            PendingSteps.Add(new PendingStep(source, map, points[i], TimeSpan.FromMilliseconds(TravelIntervalMs * (i + 1))));
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (PendingSteps.Count == 0)
            return;

        foreach (var pending in PendingSteps)
            pending.Remaining -= delta;

        while ((PendingSteps.Count > 0) && (PendingSteps[0].Remaining <= TimeSpan.Zero))
        {
            var pending = PendingSteps[0];
            PendingSteps.RemoveAt(0);

            if (pending.Source.IsAlive)
                ExecuteStep(pending);
        }
    }

    private void ExecuteStep(PendingStep pending)
    {
        var map = pending.Map;
        var point = pending.Point;
        var source = pending.Source;

        if (Animation != null)
            map.ShowAnimation(Animation.GetPointAnimation(point, source.Id));

        var targets = map.GetEntitiesAtPoints<Creature>(point)
                         .Where(creature => Filter.IsValidTarget(source, creature))
                         .ToArray();

        foreach (var target in targets)
        {
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

            if ((KnockbackTiles > 0) && target.IsAlive)
            {
                var landingPoint = Point.From(target)
                                        .DirectionalOffset(source.Direction, KnockbackTiles);

                if (map.IsWalkable(landingPoint, target, false))
                    target.WarpTo(landingPoint);
            }
        }
    }

    private sealed class PendingStep(Creature source, MapInstance map, Point point, TimeSpan remaining)
    {
        public MapInstance Map { get; } = map;
        public Point Point { get; } = point;
        public TimeSpan Remaining { get; set; } = remaining;
        public Creature Source { get; } = source;
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each tile as the line passes over it
    /// </summary>
    public Animation? Animation { get; init; }

    public IApplyDamageScript ApplyDamageScript { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt per tile
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
    ///     The multiplier applied to <see cref="DamageStat" /> when calculating bonus damage per tile
    /// </summary>
    public decimal? DamageStatMultiplier { get; init; }

    /// <summary>
    ///     The element of the damage dealt
    /// </summary>
    public Element? Element { get; init; }

    /// <summary>
    ///     The filter used to determine whether a creature on a given tile is a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     How many tiles each hit creature is knocked back, continuing in the travel direction. 0 = no knockback
    /// </summary>
    public int KnockbackTiles { get; init; }

    /// <summary>
    ///     The number of tiles the line travels forward
    /// </summary>
    public int Range { get; init; } = 5;

    /// <summary>
    ///     How long, in milliseconds, hit creatures are slowed for. 0 = no slow
    /// </summary>
    public int SlowDurationMs { get; init; }

    /// <summary>
    ///     Sound played once, on cast
    /// </summary>
    public byte? Sound { get; init; }

    /// <summary>
    ///     The number of milliseconds between the line landing on each successive tile
    /// </summary>
    public int TravelIntervalMs { get; init; } = 150;
    #endregion
}
