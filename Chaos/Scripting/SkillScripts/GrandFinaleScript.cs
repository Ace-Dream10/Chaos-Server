#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     A direct build - nothing existing matched "consume every mental affliction affecting nearby enemies, ending
///     their effects and dealing devastating damage based on the number and variety of afflictions removed."
///     Mana Burst's "damage scales with a consumed resource" shape is the pattern precedent the locked design
///     calls out (its own doc comment note: "relevant mechanic for Grand Finale's 'damage scales with consumed
///     effects' concept") - here the consumed resource is affliction count/variety instead of MP. Flat,
///     non-evolving per the locked design's updated floor schedule (unlocks Floor 7).
/// </summary>
public class GrandFinaleScript : ConfigurableSkillScriptBase
{
    private readonly IApplyDamageScript ApplyDamageScript;

    /// <inheritdoc />
    public GrandFinaleScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        var targets = map.GetEntitiesWithinRange<Monster>(context.SourcePoint, Range)
                         .Where(monster => Filter.IsValidTarget(source, monster))
                         .ToList();

        foreach (var target in targets)
        {
            var (count, variety) = TricksterAfflictions.CountActive(target);

            if (count == 0)
                continue;

            TricksterAfflictions.ConsumeAll(target);

            var damage = (BaseDamage ?? 0) + (count * DamagePerAffliction) + (variety * VarietyBonus);

            if (damage > 0)
                ApplyDamageScript.ApplyDamage(source, target, this, damage);

            if (Animation != null)
                target.Animate(Animation, source.Id);
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each target whose afflictions were consumed
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt, before affliction-count/variety scaling
    /// </summary>
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     Bonus damage per individual active affliction consumed (count)
    /// </summary>
    public int DamagePerAffliction { get; init; } = 40;

    /// <summary>
    ///     The filter used to determine which nearby creatures are valid targets
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The radius, around the caster, scanned for afflicted enemies
    /// </summary>
    public int Range { get; init; } = 5;

    /// <summary>
    ///     Sound played at the caster's position on cast
    /// </summary>
    public byte? Sound { get; init; }

    /// <summary>
    ///     Additional bonus damage per DISTINCT kind of affliction consumed (variety)
    /// </summary>
    public int VarietyBonus { get; init; } = 60;
    #endregion
}
