#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     One of Tempest's 7 specialization actives, and one of its 3 evolving abilities - see
///     <see cref="Chaos.Scripting.EffectScripts.TempestFocusEffect" />'s doc comment. All placeholder values, not
///     balance-tested.
/// </summary>
public class TempestFocusScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public TempestFocusScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var tier = GetTierValues();

        source.AnimateBody(BodyAnimation);

        var focusEffect = new TempestFocusEffect { AtkSpeedBonus = tier.AtkSpeedBonus };
        focusEffect.SetDuration(TimeSpan.FromMilliseconds(tier.DurationMs));
        source.Effects.Apply(source, focusEffect, this);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Per the locked Floor Schedule, Tempest Focus doesn't intro
    ///     until Floor 6 (mirrors Beast's Howling Sequence): Floor6(Level&lt;=12)=I(obtain,+25%,6s),
    ///     Floor7(&lt;=14)=II(+35%,8s), Floor8(&lt;=16)=III(+45%,10s), Floor9+(&gt;16)=IV(max,+60%,12s).
    /// </summary>
    private (int AtkSpeedBonus, int DurationMs) GetTierValues() =>
        Subject.Level switch
        {
            <= 12 => (25, 6000),
            <= 14 => (35, 8000),
            <= 16 => (45, 10000),
            _     => (60, 12000)
        };

    #region ScriptVars
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
