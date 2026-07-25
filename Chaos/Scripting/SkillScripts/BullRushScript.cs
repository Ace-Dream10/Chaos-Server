#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry.Abstractions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

public class BullRushScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public BullRushScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        // Play jump/charge body animation at the start of the rush
        source.AnimateBody(BodyAnimation.JumpAttack);

        var endPoint = source.DirectionalOffset(source.Direction, RushDistance);

        var points = source.GetDirectPath(endPoint)
                            .Skip(1);

        var lastWalkablePoint = Point.From(source);

        foreach (var point in points)
        {
            if (map.IsWall(point) || map.IsBlockingReactor(point))
            {
                source.WarpTo(lastWalkablePoint);
                PlayHitEffects(context, lastWalkablePoint, null);

                return;
            }

            var creature = map.GetEntitiesAtPoints<Creature>(point)
                              .TopOrDefault();

            if (creature != null)
            {
                if (Filter.IsValidTarget(source, creature))
                {
                    source.WarpTo(lastWalkablePoint);

                    var damage = CalculateDamage(source);

                    if (damage > 0)
                        ApplyDamageScript.ApplyDamage(source, creature, this, damage);

                    PlayHitEffects(context, creature);
                } else
                {
                    source.WarpTo(lastWalkablePoint);
                    PlayHitEffects(context, lastWalkablePoint, creature);
                }

                return;
            }

            // Trail animation and sound during the rush
            if (Animation != null)
                map.ShowAnimation(Animation.GetPointAnimation(point, source.Id));

            if (RushSound.HasValue)
                map.PlaySound(RushSound.Value, point);

            lastWalkablePoint = point;
        }

        source.WarpTo(lastWalkablePoint);
    }

    private int CalculateDamage(Creature source)
    {
        var damage = BaseDamage ?? 0;

        if (!DamageStat.HasValue)
            return damage;

        var statValue = source.StatSheet.GetEffectiveStat(DamageStat.Value);

        damage += DamageStatMultiplier.HasValue ? Convert.ToInt32(statValue * DamageStatMultiplier.Value) : statValue;

        return damage;
    }

    private void PlayHitEffects(ActivationContext context, Creature creature)
    {
        if (Sound.HasValue)
            context.TargetMap.PlaySound(Sound.Value, Point.From(creature));

        if (Animation != null)
            creature.Animate(Animation, context.Source.Id);
    }

    private void PlayHitEffects(ActivationContext context, IPoint point, Creature? creature)
    {
        if (Sound.HasValue)
            context.TargetMap.PlaySound(Sound.Value, point);

        if (Animation != null)
        {
            if (creature != null)
                creature.Animate(Animation, context.Source.Id);
            else
                context.TargetMap.ShowAnimation(Animation.GetPointAnimation(point, context.Source.Id));
        }
    }

    #region ScriptVars
    public IApplyDamageScript ApplyDamageScript { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.BaseDamage" />
    public int? BaseDamage { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.DamageStat" />
    public Stat? DamageStat { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.DamageStatMultiplier" />
    public decimal? DamageStatMultiplier { get; init; }

    /// <summary>
    ///     The filter used to determine whether the first creature encountered is a valid (hostile) target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The maximum number of tiles the caster will rush forward
    /// </summary>
    public int RushDistance { get; init; } = 5;

    /// <summary>
    ///     Sound played on hit
    /// </summary>
    public byte? Sound { get; init; }

    /// <summary>
    ///     Sound played during the rush on each tile traversed (optional, separate from hit sound)
    /// </summary>
    public byte? RushSound { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.AnimationAbilityComponent.IAnimationComponentOptions.Animation" />
    public Animation? Animation { get; init; }
    #endregion
}
