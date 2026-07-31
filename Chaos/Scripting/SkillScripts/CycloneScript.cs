#region
using Chaos.Collections;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry.Abstractions.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.AislingScripts;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     A channeled Berserker skill that locks the caster in place and spins clockwise (North, East, South, West),
///     striking whatever hostile creature occupies the tile directly in front on each swing. The number of
///     rotations scales with the skill's own level (1 rotation at level 1-2, up to 4 rotations at level 7+), so it
///     evolves the more it's used. Rage generation is not granted directly - it flows through the existing
///     <see cref="BerserkerRageScript" /> hard-capped mechanism whenever a hit lands, same as any other attack.
/// </summary>
public class CycloneScript : ConfigurableSkillScriptBase
{
    private static readonly Direction[] RotationSequence =
    [
        Direction.Up,
        Direction.Right,
        Direction.Down,
        Direction.Left
    ];

    private readonly List<PendingHit> PendingHits = [];

    /// <inheritdoc />
    public CycloneScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        if (context.Source is not Aisling source)
            return;

        var map = context.TargetMap;
        var originalDirection = source.Direction;
        var hitCount = GetRotationCount() * RotationSequence.Length;
        var startIndex = Math.Max(Array.IndexOf(RotationSequence, originalDirection), 0);

        source.Trackers.Tags[BerserkerRageScript.CyclingTag] = bool.TrueString;

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, source);

        ExecuteHit(source, map, RotationSequence[startIndex]);

        for (var i = 1; i < hitCount; i++)
            PendingHits.Add(
                new PendingHit(
                    source,
                    map,
                    RotationSequence[(startIndex + i) % RotationSequence.Length],
                    originalDirection,
                    i == (hitCount - 1),
                    TimeSpan.FromMilliseconds(HitIntervalMs * i)));
    }

    /// <summary>
    ///     1 rotation (4 hits) at level 1-2, 2 rotations at level 3-4, 3 rotations at level 5-6, 4 rotations at
    ///     level 7+
    /// </summary>
    private int GetRotationCount() =>
        Subject.Level switch
        {
            <= 2 => 1,
            <= 4 => 2,
            <= 6 => 3,
            _    => 4
        };

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
                ExecuteHit(pending.Source, pending.Map, pending.Direction);

            if (pending.IsFinalHit)
            {
                pending.Source.Turn(pending.OriginalDirection, forced: true);
                pending.Source.Trackers.Tags.TryRemove(BerserkerRageScript.CyclingTag, out _);
            }
        }
    }

    private void ExecuteHit(Aisling source, MapInstance map, Direction direction)
    {
        source.Turn(direction, forced: true);

        var targetPoint = source.DirectionalOffset(direction);
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

    private sealed class PendingHit(
        Aisling source,
        MapInstance map,
        Direction direction,
        Direction originalDirection,
        bool isFinalHit,
        TimeSpan remaining)
    {
        public Direction Direction { get; } = direction;
        public bool IsFinalHit { get; } = isFinalHit;
        public MapInstance Map { get; } = map;
        public Direction OriginalDirection { get; } = originalDirection;
        public TimeSpan Remaining { get; set; } = remaining;
        public Aisling Source { get; } = source;
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each creature struck by a swing
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
    ///     The number of milliseconds between each hit
    /// </summary>
    public int HitIntervalMs { get; init; } = 250;

    /// <summary>
    ///     Sound played once, on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
