#region
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SkillScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Berserker's "go berserk" stance. Applies a flat damage bonus paired with a defense penalty (positive AC is
///     worse defense in this engine - AC is inverted from what the name suggests, clamped low = strong defense) for
///     a duration. One of Berserker's 5 evolving abilities: tier scales with the skill's own level, using the same
///     level-bracket convention <see cref="CycloneScript" /> already established (1-2/3-4/5-6/7+ &#8594; tier
///     I/II/III/IV), per the design's "higher damage bonus, lower penalty... longer duration" evolution note.
/// </summary>
/// <remarks>
///     Design note: the locked design also mentions "Rage interactions" as part of this ability's evolution.
///     Confirmed resolved - the flat damage bonus/AC self-debuff tradeoff already implemented here IS the intended
///     "Rage interaction" (going berserk trades safety for damage); no separate Rage cost or generation was ever
///     meant to be added on top. Titanic Fury remains the ability that actually spends Rage as a resource, per its
///     own "more Rage consumption" evolution note - that's a distinct, unrelated mechanic from this one.
///     <br />
///     All numbers below are placeholders, not balance-tested - see <see cref="GetTierValues" />.
/// </remarks>
public class BerserkerGateScript : ConfigurableSkillScriptBase
{
    private readonly IEffectFactory EffectFactory;

    /// <inheritdoc />
    public BerserkerGateScript(Skill subject, IEffectFactory effectFactory)
        : base(subject)
        => EffectFactory = effectFactory;

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var (damageBonus, acPenalty, duration) = GetTierValues();

        var effect = (BerserkerGateEffect)EffectFactory.Create("BerserkerGate");
        effect.DamageBonus = damageBonus;
        effect.AcPenalty = acPenalty;
        effect.SetDuration(duration);

        source.Effects.Apply(source, effect, this);

        if (Sound.HasValue)
            context.TargetMap.PlaySound(Sound.Value, source);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Damage bonus grows and the AC penalty shrinks each tier,
    ///     per the design's "higher damage bonus, lower penalty... longer duration" note.
    /// </summary>
    private (int DamageBonus, int AcPenalty, TimeSpan Duration) GetTierValues() =>
        Subject.Level switch
        {
            <= 2 => (20, 10, TimeSpan.FromSeconds(8)),
            <= 4 => (30, 8, TimeSpan.FromSeconds(10)),
            <= 6 => (40, 6, TimeSpan.FromSeconds(12)),
            _    => (55, 4, TimeSpan.FromSeconds(15))
        };

    #region ScriptVars
    /// <summary>
    ///     Sound played on activation
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
