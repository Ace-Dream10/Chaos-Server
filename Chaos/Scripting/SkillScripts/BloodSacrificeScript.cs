#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     One of Beast's 7 specialization actives (also known as Kelberoth Strike) - "sacrifice Health to deal
///     massive damage based on the Health sacrificed" per the locked design. A direct build: costs a percentage of
///     the caster's CURRENT Hp (so it scales down naturally as the caster gets low, rather than being able to
///     suicide-cast at 1 Hp for free) and converts the sacrificed amount into bonus damage at
///     <see cref="DamagePerHpSacrificedPct" />. Not one of Beast's 3 evolving abilities - flat.
/// </summary>
public class BloodSacrificeScript : ConfigurableSkillScriptBase
{
    private readonly IApplyDamageScript ApplyDamageScript;

    /// <inheritdoc />
    public BloodSacrificeScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        var target = context.TargetCreature;

        if ((target is not { IsAlive: true }) || !Filter.IsValidTarget(source, target))
        {
            context.SourceAisling?.SendOrangeBarMessage("You must select a valid target.");

            return;
        }

        if (context.SourcePoint.ManhattanDistanceFrom(context.TargetPoint) > Range)
        {
            context.SourceAisling?.SendOrangeBarMessage("Your target is too far away.");

            return;
        }

        var hpSacrificed = Convert.ToInt32(source.StatSheet.CurrentHp * (HpSacrificePct / 100m));

        if (source.StatSheet.CurrentHp <= hpSacrificed)
        {
            context.SourceAisling?.SendOrangeBarMessage("Not enough Health to sacrifice.");

            return;
        }

        source.StatSheet.SubtractHp(hpSacrificed);
        context.SourceAisling?.Client.SendAttributes(StatUpdateType.Vitality);

        source.AnimateBody(BodyAnimation);

        var damage = (BaseDamage ?? 0)
                     + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.STR) * (DamageStatMultiplier ?? 1))
                     + Convert.ToInt32(hpSacrificed * (DamagePerHpSacrificedPct / 100m));

        if (damage > 0)
            ApplyDamageScript.ApplyDamage(source, target, this, damage);

        if (Animation != null)
            target.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.TargetPoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the target on hit
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt
    /// </summary>
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The stat used to scale bonus damage
    /// </summary>
    public Stat? DamageStat { get; init; }

    /// <summary>
    ///     The multiplier applied to <see cref="DamageStat" /> when calculating bonus damage
    /// </summary>
    public decimal? DamageStatMultiplier { get; init; }

    /// <summary>
    ///     The percentage of the Hp sacrificed that's converted into bonus damage
    /// </summary>
    public int DamagePerHpSacrificedPct { get; init; } = 150;

    /// <summary>
    ///     The filter used to determine whether the selected target is valid
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The percentage of the caster's CURRENT Hp sacrificed to use this skill
    /// </summary>
    public int HpSacrificePct { get; init; } = 25;

    /// <summary>
    ///     The maximum distance, in tiles, a target can be selected from
    /// </summary>
    public int Range { get; init; } = 5;

    /// <summary>
    ///     Sound played on hit
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
