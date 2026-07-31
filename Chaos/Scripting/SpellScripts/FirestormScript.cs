#region
using Chaos.Collections;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     Fire spreads outward from a target tile in rings - the center tile burns immediately, its orthogonal
///     neighbors catch after <see cref="RingDelayMs" />, their neighbors after another <see cref="RingDelayMs" />,
///     and so on for <see cref="Rings" /> rings total. Each tile only ever burns once.
/// </summary>
public class FirestormScript : ConfigurableSpellScriptBase
{
    private readonly List<PendingRing> PendingRings = [];

    /// <inheritdoc />
    public FirestormScript(Spell subject)
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

        var alreadyHit = new HashSet<Point>
        {
            origin
        };

        HitPoint(source, map, origin);

        var currentRing = new List<Point>
        {
            origin
        };

        for (var stage = 1; stage <= Rings; stage++)
        {
            var nextRing = new List<Point>();

            foreach (var point in currentRing)
            foreach (var neighbor in GetOrthogonalNeighbors(point))
                if (alreadyHit.Add(neighbor))
                    nextRing.Add(neighbor);

            if (nextRing.Count > 0)
                PendingRings.Add(new PendingRing(source, map, nextRing, TimeSpan.FromMilliseconds(RingDelayMs * stage)));

            currentRing = nextRing;
        }
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (PendingRings.Count == 0)
            return;

        foreach (var pending in PendingRings)
            pending.Remaining -= delta;

        while ((PendingRings.Count > 0) && (PendingRings[0].Remaining <= TimeSpan.Zero))
        {
            var pending = PendingRings[0];
            PendingRings.RemoveAt(0);

            if (!pending.Source.IsAlive)
                continue;

            foreach (var point in pending.Points)
                HitPoint(pending.Source, pending.Map, point);
        }
    }

    private void HitPoint(Creature source, MapInstance map, Point point)
    {
        if (map.IsWall(point))
            return;

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
    }

    private static IEnumerable<Point> GetOrthogonalNeighbors(Point point)
    {
        yield return point.DirectionalOffset(Direction.Up);
        yield return point.DirectionalOffset(Direction.Down);
        yield return point.DirectionalOffset(Direction.Left);
        yield return point.DirectionalOffset(Direction.Right);
    }

    private sealed class PendingRing(Creature source, MapInstance map, List<Point> points, TimeSpan remaining)
    {
        public MapInstance Map { get; } = map;
        public List<Point> Points { get; } = points;
        public TimeSpan Remaining { get; set; } = remaining;
        public Creature Source { get; } = source;
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each tile as it catches fire
    /// </summary>
    public Animation? Animation { get; init; }

    public IApplyDamageScript ApplyDamageScript { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt per tile
    /// </summary>
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster
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
    ///     The number of milliseconds between each ring catching fire
    /// </summary>
    public int RingDelayMs { get; init; } = 500;

    /// <summary>
    ///     How many rings of tiles spread outward from the center
    /// </summary>
    public int Rings { get; init; } = 2;

    /// <summary>
    ///     Sound played at the center point on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
