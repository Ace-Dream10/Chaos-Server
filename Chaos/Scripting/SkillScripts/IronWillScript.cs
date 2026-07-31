#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     An instant self-heal scaled off the Lancer's own CON - doubled while Lancer's Shield is currently up.
/// </summary>
public class IronWillScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public IronWillScript(Skill subject)
        : base(subject)
        => ApplyHealScript = FunctionalScripts.ApplyHealing.ApplyHealScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        var healAmount = (BaseHeal ?? 0)
                         + Convert.ToInt32(source.StatSheet.GetEffectiveStat(HealStat ?? Stat.CON) * (HealStatMultiplier ?? 1));

        if (source.Trackers.Tags.ContainsKey(LancerShieldEffect.ShieldActiveTag))
            healAmount = Convert.ToInt32(healAmount * ShieldBonusMultiplier);

        if (healAmount > 0)
            ApplyHealScript.ApplyHeal(source, source, this, healAmount);

        if (Animation != null)
            source.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, source);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the caster
    /// </summary>
    public Animation? Animation { get; init; }

    public IApplyHealScript ApplyHealScript { get; init; }

    /// <summary>
    ///     The flat portion of the heal
    /// </summary>
    public int? BaseHeal { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The stat used to scale the heal
    /// </summary>
    public Stat? HealStat { get; init; }

    /// <summary>
    ///     The multiplier applied to <see cref="HealStat" /> when calculating the heal
    /// </summary>
    public decimal? HealStatMultiplier { get; init; }

    /// <summary>
    ///     The multiplier applied to the total heal while Lancer's Shield is active
    /// </summary>
    public decimal ShieldBonusMultiplier { get; init; } = 2;

    /// <summary>
    ///     Sound played on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
