#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     One of Beast's 7 specialization actives, and one of its 3 evolving abilities - see
///     <see cref="Chaos.Scripting.EffectScripts.PerfectFormEffect" />'s doc comment. All placeholder values, not
///     balance-tested.
/// </summary>
public class PerfectFormScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public PerfectFormScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var tier = GetTierValues();

        source.AnimateBody(BodyAnimation);

        var formEffect = new PerfectFormEffect { AtkSpeedBonus = tier.AtkSpeedBonus };
        formEffect.SetDuration(TimeSpan.FromMilliseconds(tier.DurationMs));
        source.Effects.Apply(source, formEffect, this);

        if (Animation != null)
            source.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Per the locked Floor Schedule, Perfect Form doesn't intro
    ///     until Floor 5, where "intro + II together" arrive at the SAME checkpoint (only 3 real floor checkpoints
    ///     for 4 named stages) - squeezed the same way Bard/Bastion's own 5-named-stages-onto-4-checkpoints cases
    ///     were handled tonight, just folding the FIRST two stages together instead of the last two: Floor5
    ///     (Level&lt;=10)=intro+II combined(+35% atk speed,8s), Floor6(&lt;=12)=III(+45%,10s),
    ///     Floor7+(&gt;12)=IV(max,+60%,12s) - caps here, no further change through Floor10.
    /// </summary>
    private (int AtkSpeedBonus, int DurationMs) GetTierValues() =>
        Subject.Level switch
        {
            <= 10 => (35, 8000),
            <= 12 => (45, 10000),
            _     => (60, 12000)
        };

    #region ScriptVars
    /// <summary>
    ///     The animation played on the caster on activation
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     Sound played on activation
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
