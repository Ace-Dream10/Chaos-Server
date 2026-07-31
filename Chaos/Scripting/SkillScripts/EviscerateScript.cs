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

/// <summary>
///     A brutal finishing slash, devastating against a target already tagged "bleeding" (from Garrote or Venom
///     Blade).
/// </summary>
public class EviscerateScript : ConfigurableSkillScriptBase
{
    private const string BleedingTag = "bleeding";

    /// <inheritdoc />
    public EviscerateScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        var targetPoint = source.DirectionalOffset(source.Direction, Range);
        var target = map.GetEntitiesAtPoints<Creature>(targetPoint).TopOrDefault();

        if ((target == null) || !Filter.IsValidTarget(source, target))
            return;

        var baseDamage = (BaseDamage ?? 0)
                         + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.DEX) * (DamageStatMultiplier ?? 1));

        var isBleeding = target.Trackers.Tags.ContainsKey(BleedingTag);
        var damage = isBleeding ? Convert.ToInt32(baseDamage * EviscerateMult) : baseDamage;

        if (!isBleeding)
            context.SourceAisling?.SendOrangeBarMessage("More effective on bleeding targets.");

        if (damage > 0)
            ApplyDamageScript.ApplyDamage(source, target, this, damage);

        if (Animation != null)
            target.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, targetPoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the target on hit
    /// </summary>
    public Animation? Animation { get; init; }

    public IApplyDamageScript ApplyDamageScript { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt
    /// </summary>
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster when the skill is used
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
    ///     The multiplier applied to the total damage when the target is bleeding
    /// </summary>
    public decimal EviscerateMult { get; init; } = 2.5m;

    /// <summary>
    ///     The filter used to determine whether the tile directly in front of the caster holds a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The range, in tiles, at which the target is checked - should stay 1 for a melee strike
    /// </summary>
    public int Range { get; init; } = 1;

    /// <summary>
    ///     Sound played on hit
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
