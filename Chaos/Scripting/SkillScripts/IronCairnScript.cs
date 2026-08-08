#region
using Chaos.Collections;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Geometry.Abstractions;
using Chaos.Geometry.Abstractions.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Bastion's "iconic ability" per the locked design (its own phrasing) - genuinely new content, no prior
///     Lancer-era equivalent existed under any name. One of Bastion's 5 evolving abilities; tier scales with the
///     skill's own level, using the same level-bracket convention already established elsewhere
///     (1-2/3-4/5-6/7+ &#8594; tier I-IV) - level-gating stands in for real floor-gating here, same temporary
///     convention as every other Bastion/Berserker evolving ability, pending the single batched
///     level-to-floor conversion pass once actual floor maps exist.
///     <br />
///     Built on <c>shield_wall.json</c>'s frontal-cone Root application as the tier I code starting point (per
///     explicit direction to reuse it as a starting point, not a ceiling) - tier I/II keep that frontal-cone
///     footprint ("a small earth eruption"), but tier III/IV switch to a full circle centered on the caster and
///     add a lingering aftershock pulse, so the ability actually reaches "a massive battlefield control tool" by
///     tier IV rather than staying capped at shield_wall's original single-shot cone.
/// </summary>
public class IronCairnScript : ConfigurableSkillScriptBase
{
    private readonly List<AftershockZone> ActiveAftershocks = [];

    /// <inheritdoc />
    public IronCairnScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var tier = GetTierValues();

        source.AnimateBody(BodyAnimation);

        var origin = Point.From(source);

        var points = tier.Shape switch
        {
            AoeShape.Circle => new Circle(origin, tier.Range).GetPoints()
                                                              .ToList(),
            _ => ResolveFrontalCone(origin, source.Direction, tier.Range)
        };

        var struckHostiles = new List<Creature>();

        foreach (var point in points)
        {
            if (map.IsWall(point))
                continue;

            foreach (var creature in map.GetEntitiesAtPoints<Creature>(point))
            {
                if (!Filter.IsValidTarget(source, creature) || struckHostiles.Contains(creature))
                    continue;

                struckHostiles.Add(creature);

                var damage = (BaseDamage ?? 0)
                             + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.CON) * tier.DamageMultiplier);

                if (damage > 0)
                    ApplyDamageScript.ApplyDamage(source, creature, this, damage);

                var rootEffect = new RootEffect();
                rootEffect.SetDuration(TimeSpan.FromMilliseconds(tier.RootDurationMs));
                creature.Effects.Apply(source, rootEffect, this);

                if (tier.KnocksBackFromCenter && creature.IsAlive)
                {
                    var pushDirection = Point.From(creature)
                                              .DirectionalRelationTo(origin);
                    var landingPoint = creature.DirectionalOffset(pushDirection);

                    if (map.IsWalkable(landingPoint, creature, false))
                        creature.WarpTo(landingPoint);
                }

                if (Animation != null)
                    creature.Animate(Animation, source.Id);
            }

            if (AnimatePoints && (Animation != null))
                map.ShowAnimation(Animation.GetPointAnimation(point, source.Id));
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, origin);

        //Tier IV "massive battlefield control": the eruption leaves an aftershock behind - anything that lingers
        //in or wanders back into the footprint keeps getting re-rooted for a few seconds after the initial burst
        if (tier.AftershockDurationMs > 0)
            ActiveAftershocks.Add(
                new AftershockZone(
                    map,
                    source,
                    points,
                    TimeSpan.FromMilliseconds(tier.AftershockDurationMs),
                    TimeSpan.FromMilliseconds(tier.AftershockPulseIntervalMs)));
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (ActiveAftershocks.Count == 0)
            return;

        for (var i = ActiveAftershocks.Count - 1; i >= 0; i--)
        {
            var zone = ActiveAftershocks[i];
            zone.Remaining -= delta;
            zone.SinceLastPulse += delta;

            if (!zone.Source.IsAlive || (zone.Remaining <= TimeSpan.Zero))
            {
                ActiveAftershocks.RemoveAt(i);

                continue;
            }

            if (zone.SinceLastPulse < zone.PulseInterval)
                continue;

            zone.SinceLastPulse = TimeSpan.Zero;

            foreach (var point in zone.Points)
            foreach (var creature in zone.Map.GetEntitiesAtPoints<Creature>(point))
            {
                if (!creature.IsAlive || !Filter.IsValidTarget(zone.Source, creature))
                    continue;

                var rootEffect = new RootEffect();
                rootEffect.SetDuration(zone.PulseInterval + TimeSpan.FromMilliseconds(250));
                creature.Effects.Apply(zone.Source, rootEffect, this);
            }
        }
    }

    private static List<Point> ResolveFrontalCone(Point origin, Direction direction, int range)
    {
        var options = new AoeShapeOptions
        {
            Source = origin,
            Range = range,
            Direction = direction
        };

        return AoeShape.FrontalCone.ResolvePoints(options)
                       .ToList();
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Tier I/II keep shield_wall's original frontal-cone
    ///     footprint ("a small earth eruption"); tier III/IV widen into a full circle centered on the caster with
    ///     outward knockback, and tier IV adds the lingering aftershock pulse - the concrete realization of
    ///     "a massive battlefield control tool."
    /// </summary>
    private (AoeShape Shape, int Range, decimal DamageMultiplier, int RootDurationMs, bool KnocksBackFromCenter, int AftershockDurationMs, int
        AftershockPulseIntervalMs) GetTierValues() =>
        Subject.Level switch
        {
            <= 2 => (AoeShape.FrontalCone, 2, 1.5m, 1500, false, 0, 0),
            <= 4 => (AoeShape.FrontalCone, 3, 2.0m, 2500, false, 0, 0),
            <= 6 => (AoeShape.Circle, 2, 2.5m, 3000, true, 0, 0),
            _    => (AoeShape.Circle, 3, 3.0m, 4000, true, 6000, 1500)
        };

    private sealed class AftershockZone(MapInstance map, Creature source, List<Point> points, TimeSpan remaining, TimeSpan pulseInterval)
    {
        public MapInstance Map { get; } = map;
        public List<Point> Points { get; } = points;
        public TimeSpan PulseInterval { get; } = pulseInterval;
        public TimeSpan Remaining { get; set; } = remaining;
        public TimeSpan SinceLastPulse { get; set; } = TimeSpan.Zero;
        public Creature Source { get; } = source;
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each struck creature, and (if <see cref="AnimatePoints" />) on every footprint
    ///     tile
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     Whether to play <see cref="Animation" /> on every footprint tile in addition to each struck creature
    /// </summary>
    public bool AnimatePoints { get; init; }

    public IApplyDamageScript ApplyDamageScript { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt to each struck creature
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
    ///     The filter used to determine which struck creatures are valid targets
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     Sound played at the eruption's origin on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
