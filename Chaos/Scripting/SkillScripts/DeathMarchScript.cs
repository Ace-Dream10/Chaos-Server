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
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Marches the caster forward one tile at a time, cutting down anything in the path. Unlike Bastion's Charge, a
///     hostile creature never stops the march - only a wall or blocking reactor does.
/// </summary>
public class DeathMarchScript : ConfigurableSkillScriptBase
{
    private readonly List<PendingStep> PendingSteps = [];

    /// <inheritdoc />
    public DeathMarchScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        var endPoint = source.DirectionalOffset(source.Direction, MarchDistance);

        var points = source.GetDirectPath(endPoint)
                           .Skip(1)
                           .ToList();

        for (var i = 0; i < points.Count; i++)
            PendingSteps.Add(new PendingStep(source, map, points[i], TimeSpan.FromMilliseconds(StepIntervalMs * (i + 1))));
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

            if (!pending.Source.IsAlive)
            {
                PendingSteps.Clear();

                break;
            }

            if (!ExecuteStep(pending))
            {
                PendingSteps.Clear();

                break;
            }
        }
    }

    /// <summary>
    ///     Returns false if the march should stop here (a wall or blocking reactor was hit)
    /// </summary>
    private bool ExecuteStep(PendingStep pending)
    {
        var map = pending.Map;
        var source = pending.Source;
        var point = pending.Point;

        if (map.IsWall(point) || map.IsBlockingReactor(point))
            return false;

        var target = map.GetEntitiesAtPoints<Creature>(point).TopOrDefault();

        if ((target != null) && Filter.IsValidTarget(source, target))
        {
            var damage = (BaseDamage ?? 0)
                         + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.STR) * (DamageStatMultiplier ?? 1));

            if (damage > 0)
                ApplyDamageScript.ApplyDamage(source, target, this, damage);

            if (Animation != null)
                target.Animate(Animation, source.Id);
        }

        source.WarpTo(point);

        return true;
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
    ///     The animation played on each creature struck
    /// </summary>
    public Animation? Animation { get; init; }

    public IApplyDamageScript ApplyDamageScript { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt per tile
    /// </summary>
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster when the march begins
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
    ///     The filter used to determine whether a creature on a given tile is a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The maximum number of tiles the caster marches forward
    /// </summary>
    public int MarchDistance { get; init; } = 3;

    /// <summary>
    ///     The number of milliseconds between each step
    /// </summary>
    public int StepIntervalMs { get; init; } = 300;
    #endregion
}
