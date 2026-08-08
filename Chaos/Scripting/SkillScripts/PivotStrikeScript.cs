#region
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
///     Genuinely new (Floor 3, "moved earlier for early mobility"). The locked design's description is just
///     "Reposition behind target" - no further elaboration surfaced when re-checked, same as Shield Thrust. The
///     reposition itself reuses Assassin's <see cref="AmbushScript" /> algorithm (walk the direct path toward the
///     target, then try each point behind/around it in turn for a walkable landing spot) rather than being built
///     from scratch. Unlike Ambush, which is a pure repositioning utility with no damage of its own, this is
///     named "Pivot <b>Strike</b>" - since the locked design doesn't spell out a damage component, a modest hit on
///     landing was added to justify the name. Flagged as an interpretation, not a literal requirement from the
///     doc.
/// </summary>
public class PivotStrikeScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public PivotStrikeScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        var endPoint = source.DirectionalOffset(source.Direction, RangeTiles);

        var points = source.GetDirectPath(endPoint)
                            .Skip(1);

        foreach (var point in points)
        {
            if (map.IsWall(point) || map.IsBlockingReactor(point))
                return;

            var target = map.GetEntitiesAtPoints<Creature>(point)
                            .TopOrDefault();

            if ((target == null) || !Filter.IsValidTarget(source, target))
                continue;

            var behindTargetDirection = target.DirectionalRelationTo(context.SourcePoint);

            foreach (var direction in behindTargetDirection.AsEnumerable())
            {
                var destinationPoint = target.DirectionalOffset(direction);

                if (!map.IsWalkable(destinationPoint, source, false))
                    continue;

                source.WarpTo(destinationPoint);
                source.Turn(target.DirectionalRelationTo(source));

                var damage = (BaseDamage ?? 0)
                             + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.STR) * (DamageStatMultiplier ?? 1));

                if (damage > 0)
                    ApplyDamageScript.ApplyDamage(source, target, this, damage);

                if (Animation != null)
                    target.Animate(Animation, source.Id);

                if (Sound.HasValue)
                    map.PlaySound(Sound.Value, target);

                return;
            }

            return;
        }
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the target once struck from behind
    /// </summary>
    public Animation? Animation { get; init; }

    public IApplyDamageScript ApplyDamageScript { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt on landing
    /// </summary>
    public int? BaseDamage { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.DamageStat" />
    public Stat? DamageStat { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.DamageStatMultiplier" />
    public decimal? DamageStatMultiplier { get; init; }

    /// <summary>
    ///     The filter used to determine whether the first creature encountered along the path is a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The maximum number of tiles searched for a target to reposition behind
    /// </summary>
    public int RangeTiles { get; init; } = 4;

    /// <summary>
    ///     Sound played on landing/striking
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
