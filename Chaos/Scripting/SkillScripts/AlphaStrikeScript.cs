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
///     Blinks between up to <see cref="MaxTargets" /> randomly-chosen hostile monsters within range, striking each
///     one on arrival, then blinks back to the original position and facing once done. Same delayed-callback
///     pattern as Cyclone.
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
        var originalPoint = Point.From(source);
        var originalDirection = source.Direction;

        var candidates = map.GetEntitiesWithinRange<Monster>(source, Range)
                           .Where(monster => Filter.IsValidTarget(source, monster))
                           .ToArray();

        Random.Shared.Shuffle(candidates);

        var selected = candidates.Take(MaxTargets)
                                 .ToArray();

        if (selected.Length == 0)
        {
            context.SourceAisling?.SendOrangeBarMessage("No targets in range.");

            return;
        }

        source.AnimateBody(BodyAnimation);

        ExecuteBlink(source, map, selected[0]);

        for (var i = 1; i < selected.Length; i++)
            PendingBlinks.Add(new PendingBlink(source, map, selected[i], originalPoint, originalDirection, false, TimeSpan.FromMilliseconds(BlinkIntervalMs * i)));

        PendingBlinks.Add(
            new PendingBlink(
                source,
                map,
                null,
                originalPoint,
                originalDirection,
                true,
                TimeSpan.FromMilliseconds(BlinkIntervalMs * selected.Length)));

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, originalPoint);
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

            if (!pending.Source.IsAlive)
                continue;

            if (pending.IsReturnStep)
            {
                pending.Source.WarpTo(pending.OriginalPoint);
                pending.Source.Turn(pending.OriginalDirection, forced: true);
            } else if ((pending.Target != null) && pending.Target.IsAlive)
                ExecuteBlink(pending.Source, pending.Map, pending.Target);
        }
    }

    private void ExecuteBlink(Creature source, MapInstance map, Monster target)
    {
        var targetPoint = Point.From(target);
        var landingPoint = FindAdjacentWalkablePoint(map, source, targetPoint);

        source.WarpTo(landingPoint);
        source.Turn(landingPoint.DirectionalRelationTo(targetPoint), forced: true);

        var damage = (BaseDamage ?? 0)
                     + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.STR) * (DamageStatMultiplier ?? 1));

        if (damage > 0)
            ApplyDamageScript.ApplyDamage(source, target, this, damage);

        if (Animation != null)
            target.Animate(Animation, source.Id);
    }

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
        Monster? target,
        Point originalPoint,
        Direction originalDirection,
        bool isReturnStep,
        TimeSpan remaining)
    {
        public bool IsReturnStep { get; } = isReturnStep;
        public MapInstance Map { get; } = map;
        public Direction OriginalDirection { get; } = originalDirection;
        public Point OriginalPoint { get; } = originalPoint;
        public TimeSpan Remaining { get; set; } = remaining;
        public Creature Source { get; } = source;
        public Monster? Target { get; } = target;
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
    ///     The number of milliseconds between each blink-strike
    /// </summary>
    public int BlinkIntervalMs { get; init; } = 200;

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
    ///     The maximum number of targets struck per cast
    /// </summary>
    public int MaxTargets { get; init; } = 3;

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
