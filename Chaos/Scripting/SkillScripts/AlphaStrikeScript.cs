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
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     One of Beast's 7 specialization actives, and one of its 3 evolving abilities - "strike multiple enemies,
///     ending behind the final target" per the locked design. Blinks between up to a tier-scaled number of
///     randomly-chosen hostile monsters within range, striking each one on arrival, then lands BEHIND the final
///     target (the tile on the opposite side of the target's own facing, falling back to any adjacent walkable
///     tile) rather than returning to the caster's starting position - a mechanical fix versus the ability's
///     original build, which incorrectly warped back to the origin point instead. Same delayed-callback pattern as
///     Cyclone. Evolving tiers scale the target count and speed up the interval between blinks - see
///     <see cref="GetTierValues" />.
/// </summary>
public class AlphaStrikeScript : ConfigurableSkillScriptBase
{
    private readonly List<PendingBlink> PendingBlinks = [];

    /// <inheritdoc />
    public AlphaStrikeScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var tier = GetTierValues();

        //GetEntitiesWithinRange can yield the same entity more than once (its shape resolution isn't guaranteed
        //deduplicated) - Distinct() here so a single monster near multiple resolved points can't consume more
        //than one of Alpha Strike's target slots
        var candidates = map.GetEntitiesWithinRange<Monster>(source, Range)
                           .Where(monster => Filter.IsValidTarget(source, monster))
                           .Distinct()
                           .ToArray();

        Random.Shared.Shuffle(candidates);

        var selected = candidates.Take(tier.MaxTargets)
                                 .ToArray();

        if (selected.Length == 0)
        {
            context.SourceAisling?.SendOrangeBarMessage("No targets in range.");

            return;
        }

        source.AnimateBody(BodyAnimation);

        ExecuteBlink(source, map, selected[0], landBehind: selected.Length == 1);

        for (var i = 1; i < selected.Length; i++)
        {
            var isFinal = i == (selected.Length - 1);
            PendingBlinks.Add(new PendingBlink(source, map, selected[i], isFinal, TimeSpan.FromMilliseconds(tier.BlinkIntervalMs * i)));
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, Point.From(source));
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (PendingBlinks.Count == 0)
            return;

        foreach (var pending in PendingBlinks)
            pending.Remaining -= delta;

        while ((PendingBlinks.Count > 0) && (PendingBlinks[0].Remaining <= TimeSpan.Zero))
        {
            var pending = PendingBlinks[0];
            PendingBlinks.RemoveAt(0);

            if (!pending.Source.IsAlive || !pending.Target.IsAlive)
                continue;

            ExecuteBlink(pending.Source, pending.Map, pending.Target, pending.LandBehind);
        }
    }

    private void ExecuteBlink(Creature source, MapInstance map, Monster target, bool landBehind)
    {
        var targetPoint = Point.From(target);
        var landingPoint = landBehind ? MartialArtistMechanics.FindLandingBehindTarget(map, source, target) : FindAdjacentWalkablePoint(map, source, targetPoint);

        source.WarpTo(landingPoint);
        source.Turn(landingPoint.DirectionalRelationTo(targetPoint), forced: true);

        var damage = (BaseDamage ?? 0)
                     + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.STR) * (DamageStatMultiplier ?? 1));

        if (damage > 0)
            ApplyDamageScript.ApplyDamage(source, target, this, damage);

        if (Animation != null)
            target.Animate(Animation, source.Id);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Per the locked Floor Schedule, Alpha Strike doesn't intro
    ///     until Floor 7 - the LAST of Beast's 3 evolving specialization actives to unlock, landing its max tier
    ///     right alongside Martial Form's own Floor 10 finale: Floor7(Level&lt;=14)=I(obtain,3 targets),
    ///     Floor8(&lt;=16)=II(4 targets,faster), Floor9(&lt;=18)=III(5 targets,faster still),
    ///     Floor10+(&gt;18)=IV(max,6 targets,fastest).
    /// </summary>
    private (int MaxTargets, int BlinkIntervalMs) GetTierValues() =>
        Subject.Level switch
        {
            <= 14 => (3, 250),
            <= 16 => (4, 200),
            <= 18 => (5, 175),
            _     => (6, 150)
        };

    private static Point FindAdjacentWalkablePoint(MapInstance map, Creature source, Point targetPoint)
    {
        foreach (var direction in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
        {
            var candidate = targetPoint.DirectionalOffset(direction);

            if (map.IsWalkable(candidate, source, false))
                return candidate;
        }

        return targetPoint;
    }

    private sealed class PendingBlink(
        Creature source,
        MapInstance map,
        Monster target,
        bool landBehind,
        TimeSpan remaining)
    {
        public bool LandBehind { get; } = landBehind;
        public MapInstance Map { get; } = map;
        public TimeSpan Remaining { get; set; } = remaining;
        public Creature Source { get; } = source;
        public Monster Target { get; } = target;
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each struck target
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
    ///     The filter used to determine which nearby monsters are valid targets
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The radius around the caster within which targets are chosen
    /// </summary>
    public int Range { get; init; } = 5;

    /// <summary>
    ///     Sound played once, on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
