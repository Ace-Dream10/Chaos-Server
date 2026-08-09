#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
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
///     A precise strike that marks the target with Mark of the Bane - the "marks a target" step of Slayer's
///     identity loop. See <see cref="MarkOfTheBaneEffect" /> for the mark's mechanical effect.
/// </summary>
/// <remarks>
///     One of Slayer's 5 evolving abilities - tier scales with the skill's own level, using the same
///     level-bracket convention <see cref="BastionsChargeScript" /> established. Per the locked design's
///     evolution note ("higher damage cap, longer duration, stronger mark effects"), the mark's bonus damage
///     percent and duration both scale per tier.
/// </remarks>
public class MarkOfTheBaneScript : ConfigurableSkillScriptBase
{
    private readonly IApplyDamageScript ApplyDamageScript;

    /// <inheritdoc />
    public MarkOfTheBaneScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var tier = GetTierValues();

        var targetPoint = source.DirectionalOffset(source.Direction, Range);
        var target = map.GetEntitiesAtPoints<Creature>(targetPoint).TopOrDefault();

        source.AnimateBody(BodyAnimation);

        if ((target == null) || !Filter.IsValidTarget(source, target))
            return;

        var damage = (BaseDamage ?? 0) + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.STR) * (DamageStatMultiplier ?? 1));

        if (damage > 0)
            ApplyDamageScript.ApplyDamage(source, target, this, damage);

        var markEffect = new MarkOfTheBaneEffect { BonusDamagePct = tier.BonusDamagePct };
        markEffect.SetDuration(TimeSpan.FromMilliseconds(tier.DurationMs));
        target.Effects.Apply(source, markEffect, this);

        if (Animation != null)
            target.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, targetPoint);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested.
    /// </summary>
    private (int BonusDamagePct, int DurationMs) GetTierValues() =>
        Subject.Level switch
        {
            <= 2 => (15, 10000),
            <= 4 => (20, 12000),
            <= 6 => (25, 14000),
            _    => (30, 16000)
        };

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
    ///     The filter used to determine whether the target tile holds a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The range, in tiles, at which the target is checked
    /// </summary>
    public int Range { get; init; } = 1;

    /// <summary>
    ///     Sound played on hit
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
