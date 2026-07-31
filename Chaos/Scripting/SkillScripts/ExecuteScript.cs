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

public class ExecuteScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public ExecuteScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        var targetPoint = source.DirectionalOffset(source.Direction);

        var creature = map.GetEntitiesAtPoints<Creature>(targetPoint)
                          .TopOrDefault();

        if ((creature == null) || !Filter.IsValidTarget(source, creature))
            return;

        var damage = creature.StatSheet.HealthPercent <= ExecuteThresholdPct
            ? creature.StatSheet.CurrentHp * 3
            : CalculateDamage(source);

        if (damage > 0)
            ApplyDamageScript.ApplyDamage(source, creature, this, damage);

        //Bloodlust's execute-chaining reset lives in AssassinFrenzyScript, on the tick after this kill is
        //observed - doing it here was a no-op, since Skill.Use() unconditionally calls BeginCooldown() right
        //after OnUse returns, which immediately re-establishes a fresh cooldown over anything set here.

        if (Animation != null)
            creature.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, Point.From(creature));
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
    ///     If the target's HP is at or below this percentage, the hit deals lethal damage instead of normal damage
    /// </summary>
    public decimal ExecuteThresholdPct { get; init; } = 20;

    /// <summary>
    ///     The filter used to determine whether the creature directly in front is a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     Sound played on hit
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
