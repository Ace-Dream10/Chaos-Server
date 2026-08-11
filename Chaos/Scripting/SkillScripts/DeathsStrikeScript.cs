#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
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
///     Renamed from Voidwalker (kept its exact teleport-behind-and-strike mechanic - a direct match for the locked
///     design's "instantly appear behind your target and deliver a devastating opening strike"). Adds the locked
///     design's other stated condition: bonus damage when used from Hide (<see cref="VisibilityType.Hidden" />, the
///     same visibility flag <see cref="Chaos.Scripting.EffectScripts.HideEffects.HideEffect" /> sets) or to
///     initiate combat (the target has no aggro on the caster yet, checked via <see cref="AggroList.GetAggro" /> -
///     0 means the caster isn't already on its aggro list, i.e. this strike is what starts the fight).
///     <see cref="OpeningStrikeMultiplier" /> is a placeholder, not balance-tested.
/// </summary>
public class DeathsStrikeScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public DeathsStrikeScript(Skill subject)
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

        //was this strike thrown from Hide, or does it initiate combat? checked BEFORE the caster becomes visible/
        //aggroed below, since either of those would otherwise self-invalidate the very condition being checked
        var fromHide = source.Visibility is VisibilityType.Hidden;
        var initiatesCombat = (target is Monster targetMonster) && (targetMonster.AggroList.GetAggro(source) == 0);
        var isOpeningStrike = fromHide || initiatesCombat;

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

        if (isOpeningStrike)
            damage = Convert.ToInt32(damage * OpeningStrikeMultiplier);

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
    ///     The multiplier applied to damage when the strike is thrown from Hide or initiates combat - placeholder,
    ///     not balance-tested
    /// </summary>
    public decimal OpeningStrikeMultiplier { get; init; } = 2m;

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
