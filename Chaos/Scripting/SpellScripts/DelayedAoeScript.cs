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
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     Same delayed-impact pattern as Deadcenter, but the payload is an AoE burst around a ground point rather than
///     a hit on a single locked-on target.
/// </summary>
public class DelayedAoeScript : ConfigurableSpellScriptBase
{
    private readonly List<PendingBlast> PendingBlasts = [];

    /// <inheritdoc />
    public DelayedAoeScript(Spell subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var point = context.TargetPoint;

        source.AnimateBody(BodyAnimation);

        if (Animation != null)
            map.ShowAnimation(Animation.GetPointAnimation(point, source.Id));

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, point);

        PendingBlasts.Add(new PendingBlast(source, map, point, TimeSpan.FromMilliseconds(DelayMs)));
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (PendingBlasts.Count == 0)
            return;

        for (var i = PendingBlasts.Count - 1; i >= 0; i--)
        {
            var pending = PendingBlasts[i];
            pending.Remaining -= delta;

            if (pending.Remaining > TimeSpan.Zero)
                continue;

            PendingBlasts.RemoveAt(i);

            if (pending.Source.IsAlive)
                Explode(pending);
        }
    }

    private void Explode(PendingBlast pending)
    {
        var map = pending.Map;
        var source = pending.Source;

        if (ExplosionAnimation != null)
            map.ShowAnimation(ExplosionAnimation.GetPointAnimation(pending.Point, source.Id));

        foreach (var target in map.GetEntitiesWithinRange<Creature>(pending.Point, Range))
        {
            if (!Filter.IsValidTarget(source, target))
                continue;

            var damage = (BaseDamage ?? 0)
                         + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.INT) * (DamageStatMultiplier ?? 1));

            if (damage > 0)
                ApplyDamageScript.ApplyDamage(source, target, this, damage, Element);
        }
    }

    private sealed class PendingBlast(Creature source, MapInstance map, Point point, TimeSpan remaining)
    {
        public MapInstance Map { get; } = map;
        public Point Point { get; } = point;
        public TimeSpan Remaining { get; set; } = remaining;
        public Creature Source { get; } = source;
    }

    #region ScriptVars
    /// <summary>
    ///     The lock-on/telegraph animation played at the target point immediately on cast
    /// </summary>
    public Animation? Animation { get; init; }

    public IApplyDamageScript ApplyDamageScript { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt
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
    ///     The multiplier applied to <see cref="DamageStat" /> when calculating bonus damage
    /// </summary>
    public decimal? DamageStatMultiplier { get; init; }

    /// <summary>
    ///     How long, in milliseconds, between cast and the blast actually landing
    /// </summary>
    public int DelayMs { get; init; } = 2000;

    /// <summary>
    ///     The element of the damage dealt
    /// </summary>
    public Element? Element { get; init; }

    /// <summary>
    ///     The animation played at the target point when the blast actually lands
    /// </summary>
    public Animation? ExplosionAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine whether each creature caught in the blast is a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The radius of the blast around the target point
    /// </summary>
    public int Range { get; init; } = 2;

    /// <summary>
    ///     Sound played at the target point on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
