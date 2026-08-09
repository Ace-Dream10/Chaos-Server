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
///     A cruel thrust that opens a bleeding wound and partially ignores armor - "better synergy with Scythe" per
///     the locked design is realized by the initial hit also applying a Severance stack, same as Flourish/Slayer's
///     Rush.
/// </summary>
/// <remarks>
///     One of Slayer's 5 evolving abilities - tier scales with the skill's own level, using the same
///     level-bracket convention <see cref="BastionsChargeScript" /> established. Per the locked design's evolution
///     note ("stronger bleed, armor penetration, better synergy with Scythe"), bleed damage, defense-ignore
///     percent, and Severance stacks applied all scale per tier. Reuses the existing <see cref="BleedEffect" />
///     rather than a new bleed implementation.
/// </remarks>
/// <remarks>
///     Also drives <see cref="SeveranceTargetSync" /> - the MP-bar-as-stack-visual feedback that used to live in
///     the now-retired Severance Strike. Picked over Flourish/Mark of the Bane as the wiring point: it's a single
///     hit per use (one clean sync per activation, unlike Flourish's up-to-8-hits-per-use which would make the MP
///     bar flicker), and Mark of the Bane doesn't apply Severance stacks at all - it's a separate "marked" debuff.
/// </remarks>
public class CruelThrustScript : ConfigurableSkillScriptBase
{
    private const string SeveranceTargetTag = "severanceTarget";
    private const string SeveredTag = "severed";
    private const int MpPerStack = 20;

    private readonly IApplyDamageScript ApplyDamageScript;

    /// <inheritdoc />
    public CruelThrustScript(Skill subject)
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

        var rawDamage = (BaseDamage ?? 0) + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.STR) * (DamageStatMultiplier ?? 1));
        var damage = DefenseIgnoreHelper.ApplyIgnoreDefense(target, rawDamage, tier.IgnoreDefensePct);

        if (damage > 0)
            ApplyDamageScript.ApplyDamage(source, target, this, damage);

        var bleedEffect = new BleedEffect { BleedDamage = tier.BleedDamage };
        bleedEffect.SetDuration(TimeSpan.FromMilliseconds(BleedDurationMs));
        target.Effects.Apply(source, bleedEffect, this);

        SeveranceTargetSync.SwitchTargetIfNeeded(context.SourceAisling, map, target, SeveranceTargetTag, SeveranceEffect.StacksTag, SeveredTag);

        target.Effects.Apply(source, new SeveranceEffect { StacksToApply = tier.SeveranceStacksToApply }, this);

        SeveranceTargetSync.SyncMpToStacks(context.SourceAisling, target, SeveranceEffect.StacksTag, MpPerStack);

        if (Animation != null)
            target.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, targetPoint);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested.
    /// </summary>
    private (int BleedDamage, decimal IgnoreDefensePct, int SeveranceStacksToApply) GetTierValues() =>
        Subject.Level switch
        {
            <= 2 => (15, 0.15m, 1),
            <= 4 => (20, 0.20m, 1),
            <= 6 => (25, 0.25m, 2),
            _    => (30, 0.30m, 2)
        };

    #region ScriptVars
    /// <summary>
    ///     The animation played on the target on hit
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The flat portion of the initial strike's damage
    /// </summary>
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     How long, in milliseconds, the bleed lasts
    /// </summary>
    public int BleedDurationMs { get; init; } = 5000;

    /// <summary>
    ///     The body animation played by the caster when the skill is used
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The stat used to scale the initial strike's bonus damage
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
