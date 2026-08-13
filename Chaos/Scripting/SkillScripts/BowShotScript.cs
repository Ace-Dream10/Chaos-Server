#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Common;
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

public class BowShotScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public BowShotScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;

        if ((source is Aisling aisling) && !HasBowEquipped(aisling))
        {
            aisling.SendOrangeBarMessage("You need a bow equipped.");

            return;
        }

        if (!source.StatSheet.TrySubtractMp(ManaCost))
        {
            if (source is Aisling manaAisling)
                manaAisling.SendOrangeBarMessage("Not enough mana.");

            return;
        }

        if (source is Aisling attackerAisling)
            attackerAisling.Client.SendAttributes(StatUpdateType.Vitality);

        source.AnimateBody(BodyAnimation);

        var map = context.TargetMap;
        var endPoint = source.DirectionalOffset(source.Direction, Range);

        var points = source.GetDirectPath(endPoint)
                            .Skip(1);

        foreach (var point in points)
        {
            if (map.IsWall(point) || map.IsBlockingReactor(point))
                return;

            var creature = map.GetEntitiesAtPoints<Creature>(point)
                              .TopOrDefault();

            if (creature != null)
            {
                if (!Filter.IsValidTarget(source, creature))
                    return;

                var damage = CalculateDamage(source);

                if (damage > 0)
                    ApplyDamageScript.ApplyDamage(source, creature, this, damage);

                if (Animation != null)
                    creature.Animate(Animation, source.Id);

                if (Sound.HasValue)
                    map.PlaySound(Sound.Value, Point.From(creature));

                return;
            }
        }
    }

    private static bool HasBowEquipped(Aisling aisling)
    {
        var weapon = aisling.Equipment[EquipmentSlot.Weapon];

        return (weapon != null) && weapon.Template.Category.EqualsI("bow");
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
    public IApplyDamageScript ApplyDamageScript { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.BaseDamage" />
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.DamageStat" />
    public Stat? DamageStat { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.DamageStatMultiplier" />
    public decimal? DamageStatMultiplier { get; init; }

    /// <summary>
    ///     The filter used to determine whether the first creature encountered is a valid (hostile) target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The MP cost to use this skill
    /// </summary>
    public int ManaCost { get; init; }

    /// <summary>
    ///     The maximum number of tiles the arrow will travel looking for a target
    /// </summary>
    public int Range { get; init; }

    /// <summary>
    ///     The sound played on hit
    /// </summary>
    public byte? Sound { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.AnimationAbilityComponent.IAnimationComponentOptions.Animation" />
    public Animation? Animation { get; init; }
    #endregion
}
