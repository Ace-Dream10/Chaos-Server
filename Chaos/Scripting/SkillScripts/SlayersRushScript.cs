#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
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
///     A short rush, modeled on Lancer's Charge, that crashes through every hostile in its path (like Berserker
///     Charge) instead of stopping at the first one - each one hit takes damage, a brief root, and a stack of
///     Severance, fitting the Slayer's "rush in, start stacking" identity.
/// </summary>
public class SlayersRushScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public SlayersRushScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        var endPoint = source.DirectionalOffset(source.Direction, RushDistance);
        var points = source.GetDirectPath(endPoint).Skip(1);

        var lastWalkablePoint = Point.From(source);

        foreach (var point in points)
        {
            if (map.IsWall(point) || map.IsBlockingReactor(point))
                break;

            var creature = map.GetEntitiesAtPoints<Creature>(point).TopOrDefault();

            if ((creature != null) && Filter.IsValidTarget(source, creature))
            {
                var damage = CalculateDamage(source);

                if (damage > 0)
                    ApplyDamageScript.ApplyDamage(source, creature, this, damage);

                var rootEffect = new RootEffect();
                rootEffect.SetDuration(TimeSpan.FromMilliseconds(RootDurationMs));
                creature.Effects.Apply(source, rootEffect, this);

                var severanceEffect = new SeveranceEffect { StacksToApply = SeveranceStacksToApply };
                creature.Effects.Apply(source, severanceEffect, this);

                if (Animation != null)
                    creature.Animate(Animation, source.Id);

                if (Sound.HasValue)
                    map.PlaySound(Sound.Value, point);
            }

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

    #region ScriptVars
    /// <summary>
    ///     The animation played on each creature struck
    /// </summary>
    public Animation? Animation { get; init; }

    public IApplyDamageScript ApplyDamageScript { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.BaseDamage" />
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster when the rush begins
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.DamageStat" />
    public Stat? DamageStat { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.DamageStatMultiplier" />
    public decimal? DamageStatMultiplier { get; init; }

    /// <summary>
    ///     The filter used to determine whether each creature crossed is a valid (hostile) target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     How long, in milliseconds, each hit creature is rooted for
    /// </summary>
    public int RootDurationMs { get; init; } = 1000;

    /// <summary>
    ///     The number of tiles the caster rushes forward
    /// </summary>
    public int RushDistance { get; init; } = 2;

    /// <summary>
    ///     The number of Severance stacks applied to each hit creature
    /// </summary>
    public int SeveranceStacksToApply { get; init; } = 1;

    /// <summary>
    ///     Sound played on each hit
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
