#region
using Chaos.Collections;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry.Abstractions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Renamed from "Lancer's Charge" - one of Bastion's 5 evolving abilities (tier scales with the skill's own
///     level, using the same level-bracket convention <see cref="CycloneScript" />/<see cref="BerserkerGateScript" />
///     already established: 1-2/3-4/5-6/7+ &#8594; tier I-IV). Per the design's "more range, more damage, better
///     engage" note, all three scale with tier. Tier III+ additionally realizes "knock enemies aside": any other
///     hostile creature adjacent to the impact point is shoved back one tile, away from the charge's direction of
///     travel.
/// </summary>
/// <remarks>
///     The design's other evolution idea - "leave a cracked path" (a lingering hazard along the charge lane) - is
///     deliberately NOT built here. The design doc's own phrasing ("Eventually could knock enemies aside or leave
///     a cracked path") frames both as soft possibilities rather than a committed pair, unlike Iron Cairn's
///     explicit "must reach massive battlefield control by tier IV" mandate. Knock-aside was picked as the
///     concrete tier III/IV differentiator; the cracked-path hazard is flagged here as a deferred follow-up, not
///     forgotten.
/// </remarks>
public class BastionsChargeScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public BastionsChargeScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var tier = GetTierValues();

        source.AnimateBody(BodyAnimation);

        var endPoint = source.DirectionalOffset(source.Direction, tier.RushDistance);

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

                    var damage = CalculateDamage(source, tier.BaseDamage, tier.DamageStatMultiplier);

                    if (damage > 0)
                        ApplyDamageScript.ApplyDamage(source, creature, this, damage);

                    //Tier III+: stun the primary target on impact
                    if (tier.StunOnImpact)
                    {
                        var rootEffect = new RootEffect();
                        rootEffect.SetDuration(TimeSpan.FromMilliseconds(StunDurationMs));
                        creature.Effects.Apply(source, rootEffect, this);
                    }

                    PlayHitEffects(context, creature);

                    //Tier IV: AoE damage + stun on nearby enemies
                    if (tier.AoeOnImpact)
                        KnockAsideNearby(map, source, creature, point);
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

    /// <summary>
    ///     Tier III+ "knock enemies aside": every other hostile creature adjacent to the impact point is pushed
    ///     one tile further away from it, if the landing tile is walkable.
    /// </summary>
    private void KnockAsideNearby(MapInstance map, Creature source, Creature primaryTarget, IPoint impactPoint)
    {
        var nearby = map.GetEntitiesWithinRange<Creature>(impactPoint, 1)
                        .Where(creature => (creature != primaryTarget) && Filter.IsValidTarget(source, creature));

        foreach (var creature in nearby)
        {
            var pushDirection = Point.From(creature)
                                      .DirectionalRelationTo(impactPoint);
            var landingPoint = creature.DirectionalOffset(pushDirection);

            if (map.IsWalkable(landingPoint, creature, false))
                creature.WarpTo(landingPoint);
        }
    }

    /// <summary>
    ///     Tier-specific values, now read from scriptVars (one template per tier) instead of Subject.Level.
    ///     See ELYSIUM_CLASS_DESIGN.md Bastion §Bastion's Charge I-IV.
    /// </summary>
    private (int RushDistance, int BaseDamage, decimal DamageStatMultiplier, bool StunOnImpact, bool AoeOnImpact) GetTierValues() =>
        (RushDistance, BaseDamage, DamageStatMultiplier, StunOnImpact, AoeOnImpact);

    private int CalculateDamage(Creature source, int baseDamage, decimal damageStatMultiplier)
    {
        if (!DamageStat.HasValue)
            return baseDamage;

        var statValue = source.StatSheet.GetEffectiveStat(DamageStat.Value);

        return baseDamage + Convert.ToInt32(statValue * damageStatMultiplier);
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

    /// <summary>
    ///     Whether this tier also damages/stuns nearby enemies on impact (Tier IV).
    /// </summary>
    public bool AoeOnImpact { get; init; }

    /// <summary>
    ///     Flat base damage dealt to the target on impact.
    /// </summary>
    public int BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster when the charge begins
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.DamageStat" />
    public Stat? DamageStat { get; init; }

    /// <summary>
    ///     Multiplier applied to the damage stat value and added to BaseDamage.
    /// </summary>
    public decimal DamageStatMultiplier { get; init; }

    /// <summary>
    ///     The filter used to determine whether the first creature encountered is a valid (hostile) target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The number of tiles the charge travels. I=3, II=5, III=5, IV=5 (or as configured).
    /// </summary>
    public int RushDistance { get; init; }

    /// <summary>
    ///     Sound played on hit
    /// </summary>
    public byte? Sound { get; init; }

    /// <summary>
    ///     Duration in ms of the stun (Root) applied on impact at Tier III+.
    /// </summary>
    public int StunDurationMs { get; init; }

    /// <summary>
    ///     Whether this tier stuns the target on impact (Tier III+).
    /// </summary>
    public bool StunOnImpact { get; init; }

    /// <summary>
    ///     Sound played during the rush on each tile traversed (optional, separate from hit sound)
    /// </summary>
    public byte? RushSound { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.AnimationAbilityComponent.IAnimationComponentOptions.Animation" />
    public Animation? Animation { get; init; }
    #endregion
}
