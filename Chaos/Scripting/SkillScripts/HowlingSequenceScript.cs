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
///     One of Beast's 7 specialization actives, and one of its 3 evolving abilities - "rapid multi-hit claw combo"
///     per the locked design. Strikes the target directly in front multiple times in rapid succession, tracked via
///     a delayed-pending-hit list (same pattern as StaciasPulseScript/AlphaStrikeScript). Evolving tiers scale the
///     number of hits and the per-hit damage.
/// </summary>
public class HowlingSequenceScript : ConfigurableSkillScriptBase
{
    private readonly List<PendingHit> PendingHits = [];

    /// <inheritdoc />
    public HowlingSequenceScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    private IApplyDamageScript ApplyDamageScript { get; }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var targetPoint = source.DirectionalOffset(source.Direction);

        var target = map.GetEntitiesAtPoints<Creature>(targetPoint)
                        .FirstOrDefault(creature => Filter.IsValidTarget(source, creature));

        if (target == null)
        {
            context.SourceAisling?.SendOrangeBarMessage("No target in front of you.");

            return;
        }

        var tier = GetTierValues();

        source.AnimateBody(BodyAnimation);

        var damagePerHit = tier.DamagePerHit + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.STR) * (DamageStatMultiplier ?? 1));

        for (var i = 0; i < tier.HitCount; i++)
            PendingHits.Add(new PendingHit(TimeSpan.FromMilliseconds(HitIntervalMs * i), source, map, target, damagePerHit));

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, Point.From(source));
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (PendingHits.Count == 0)
            return;

        for (var i = PendingHits.Count - 1; i >= 0; i--)
        {
            var pending = PendingHits[i];
            pending.Remaining -= delta;

            if (pending.Remaining > TimeSpan.Zero)
                continue;

            PendingHits.RemoveAt(i);

            if (!pending.Source.IsAlive || !pending.Target.IsAlive)
                continue;

            ApplyDamageScript.ApplyDamage(pending.Source, pending.Target, this, pending.Damage);

            if (Animation != null)
                pending.Target.Animate(Animation, pending.Source.Id);
        }
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Per the locked Floor Schedule, Howling Sequence doesn't
    ///     intro until Floor 6: Floor6(Level&lt;=12)=I(obtain,3 hits), Floor7(&lt;=14)=II(4 hits,harder),
    ///     Floor8(&lt;=16)=III(5 hits,harder still), Floor9+(&gt;16)=IV(max,6 hits,hardest).
    /// </summary>
    private (int HitCount, int DamagePerHit) GetTierValues() =>
        Subject.Level switch
        {
            <= 12 => (3, 40),
            <= 14 => (4, 50),
            <= 16 => (5, 60),
            _     => (6, 70)
        };

    private sealed class PendingHit(TimeSpan remaining, Creature source, MapInstance map, Creature target, int damage)
    {
        public int Damage { get; } = damage;
        public MapInstance Map { get; } = map;
        public TimeSpan Remaining { get; set; } = remaining;
        public Creature Source { get; } = source;
        public Creature Target { get; } = target;
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the target on each hit
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The stat used to scale bonus damage per hit
    /// </summary>
    public Stat? DamageStat { get; init; }

    /// <summary>
    ///     The multiplier applied to <see cref="DamageStat" /> when calculating bonus damage per hit
    /// </summary>
    public decimal? DamageStatMultiplier { get; init; }

    /// <summary>
    ///     The filter used to determine whether the tile directly in front holds a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

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
