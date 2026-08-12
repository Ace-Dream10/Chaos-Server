#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     A direct build growing out of the flat Meditate skill (its MeditateEffect extended with settable tier
///     properties rather than hardcoded constants). One of the Martial Artist's 2 shared evolving abilities
///     (alongside Martial Form) - floor-gated I/II/III/IV per ELYSIUM_CLASS_DESIGN.md's combined Meditate+Form
///     table. All placeholder values, not balance-tested.
/// </summary>
public class MeditateScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public MeditateScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var tier = GetTierValues();

        source.AnimateBody(BodyAnimation);

        var meditateEffect = new MeditateEffect
        {
            MpPerTick = tier.MpPerTick,
            FlatSpellDamageBonus = tier.FlatSpellDamageBonus
        };

        source.Effects.Apply(source, meditateEffect, this);

        if (Animation != null)
            source.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Per the locked Floor Schedule: Floor2(Level&lt;=4)=I,
    ///     Floor5(&lt;=10)=II (first evolution), Floor7(&lt;=14)=III, Floor9+(&gt;14)=IV (max - no further change
    ///     at Floor10, already maxed).
    /// </summary>
    private (int MpPerTick, int FlatSpellDamageBonus) GetTierValues() =>
        Subject.Level switch
        {
            <= 4  => (20, 15),
            <= 10 => (30, 20),
            <= 14 => (40, 25),
            _     => (55, 35)
        };

    #region ScriptVars
    /// <summary>
    ///     The animation played on the caster on activation
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster when the skill is used
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     Sound played on activation
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
