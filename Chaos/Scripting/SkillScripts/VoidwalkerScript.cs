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

public class VoidwalkerScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public VoidwalkerScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        var endPoint = source.DirectionalOffset(source.Direction, Range);

        var points = source.GetDirectPath(endPoint)
                            .Skip(1);

        Creature? target = null;

        foreach (var point in points)
        {
            if (map.IsWall(point) || map.IsBlockingReactor(point))
                break;

            var entity = map.GetEntitiesAtPoints<Creature>(point)
                            .TopOrDefault();

            if (entity != null)
            {
                if (Filter.IsValidTarget(source, entity))
                    target = entity;

                break;
            }
        }

        if (target == null)
        {
            context.SourceAisling?.SendOrangeBarMessage("No target found.");

            return;
        }

        //smoke/shadow puff at the departure tile, sent first so it has the best chance of rendering
        //before the client's view snaps to the destination point on WarpTo
        if (CastEffect != null)
            map.ShowAnimation(CastEffect.GetPointAnimation(context.SourcePoint, source.Id));

        source.AnimateBody(BodyAnimation);

        if (CasterAnimation.HasValue)
            source.Animate(
                new Animation
                {
                    TargetAnimation = CasterAnimation.Value,
                    AnimationSpeed = 100
                },
                source.Id);

        //get the direction that vectors behind the target relative to the source
        var behindTargetDirection = target.DirectionalRelationTo(context.SourcePoint);

        var teleported = false;

        //for each direction around the target, starting with the direction behind the target
        foreach (var direction in behindTargetDirection.AsEnumerable())
        {
            var destinationPoint = target.DirectionalOffset(direction);

            if (!map.IsWalkable(destinationPoint, source, false))
                continue;

            source.WarpTo(destinationPoint);
            var newDirection = target.DirectionalRelationTo(source);
            source.Turn(newDirection);
            teleported = true;

            break;
        }

        if (!teleported)
            return;

        source.AnimateBody(ArrivalBodyAnimation);

        var damage = CalculateDamage(source);

        if (damage > 0)
            ApplyDamageScript.ApplyDamage(source, target, this, damage);

        if (Animation != null)
            target.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, Point.From(target));
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

    #region ScriptVars
    /// <summary>
    ///     The body animation played by the caster upon arrival behind the target
    /// </summary>
    public BodyAnimation ArrivalBodyAnimation { get; init; }

    /// <summary>
    ///     The animation played on the target on hit
    /// </summary>
    public Animation? Animation { get; init; }

    public IApplyDamageScript ApplyDamageScript { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.BaseDamage" />
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster when the skill is used
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The self-animation id played on the caster when the skill is used
    /// </summary>
    public ushort? CasterAnimation { get; init; }

    /// <summary>
    ///     The animation played centered on the caster's position at the moment of casting
    /// </summary>
    public Animation? CastEffect { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.DamageStat" />
    public Stat? DamageStat { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.DamageStatMultiplier" />
    public decimal? DamageStatMultiplier { get; init; }

    /// <summary>
    ///     The filter used to determine whether the first creature encountered in the scan is a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The maximum number of tiles scanned in front of the caster for a target
    /// </summary>
    public int Range { get; init; }

    /// <summary>
    ///     Sound played on hit
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
