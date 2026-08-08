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
///     Renamed/repurposed from "Shield Bash" (Floor 1, shield-focused starter trio). The locked design's own
///     description is just "Knockback" - no further elaboration surfaced when re-checked, so this is a real
///     positional knockback (push the target back a fixed distance, away from the caster) rather than the
///     minimal "keep Shield Bash's root, just relabel it" swap. Push logic mirrors
///     <see cref="Chaos.Scripting.SpellScripts.KnockbackAoeScript" />'s technique (directional offset away from
///     the source + a walkability check before landing).
/// </summary>
public class ShieldThrustScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public ShieldThrustScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        if ((source is Aisling aisling) && !HasShieldEquipped(aisling))
        {
            aisling.SendOrangeBarMessage("You need a shield equipped.");

            return;
        }

        source.AnimateBody(BodyAnimation);

        var targetPoint = source.DirectionalOffset(source.Direction);

        var creature = map.GetEntitiesAtPoints<Creature>(targetPoint)
                          .TopOrDefault();

        if ((creature == null) || !Filter.IsValidTarget(source, creature))
            return;

        var damage = CalculateDamage(source);

        if (damage > 0)
            ApplyDamageScript.ApplyDamage(source, creature, this, damage);

        if (creature.IsAlive && (KnockbackTiles > 0))
        {
            var landingPoint = creature.DirectionalOffset(source.Direction, KnockbackTiles);

            if (map.IsWalkable(landingPoint, creature, false))
                creature.WarpTo(landingPoint);
        }

        if (Animation != null)
            creature.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, Point.From(creature));
    }

    private static bool HasShieldEquipped(Aisling aisling) => aisling.Equipment[EquipmentSlot.Shield] != null;

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
    ///     The animation played on the target on hit
    /// </summary>
    public Animation? Animation { get; init; }

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
    ///     The filter used to determine whether the creature directly in front is a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     How many tiles the target is knocked back, away from the caster
    /// </summary>
    public int KnockbackTiles { get; init; } = 2;

    /// <summary>
    ///     Sound played on hit
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
