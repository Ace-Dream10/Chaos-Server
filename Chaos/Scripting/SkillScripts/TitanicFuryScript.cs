#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SkillScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Berserker's Rage-spent burst window - design doc's own note: "Berserker's equivalent of Slayer's Scythe."
///     Spends current Rage (stored as MP - see <see cref="Chaos.Scripting.AislingScripts.BerserkerRageScript" />)
///     for a temporary attack speed + damage burst. One of Berserker's 5 evolving abilities: tier scales with the
///     skill's own level, using the same level-bracket convention <see cref="CycloneScript" /> already established
///     (1-2/3-4/5-6/7+ &#8594; tier I/II/III/IV), per the design's "more attack speed, more Rage consumption, more
///     devastating burst" evolution note.
/// </summary>
/// <remarks>
///     All tier numbers (Rage cost, attack speed/damage bonus, duration) are placeholders, not balance-tested - see
///     <see cref="GetTierValues" />.
/// </remarks>
public class TitanicFuryScript : ConfigurableSkillScriptBase
{
    private readonly IEffectFactory EffectFactory;

    /// <inheritdoc />
    public TitanicFuryScript(Skill subject, IEffectFactory effectFactory)
        : base(subject)
        => EffectFactory = effectFactory;

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var (rageCost, atkSpeedBonus, damageBonus, duration) = GetTierValues();

        if (!source.StatSheet.TrySubtractMp(rageCost))
        {
            if (source is Aisling insufficientRageAisling)
                insufficientRageAisling.SendOrangeBarMessage("Not enough rage.");

            return;
        }

        if (source is Aisling attackerAisling)
            attackerAisling.Client.SendAttributes(StatUpdateType.Vitality);

        var effect = (TitanicFuryEffect)EffectFactory.Create("TitanicFury");
        effect.AtkSpeedPctBonus = atkSpeedBonus;
        effect.DamageBonus = damageBonus;
        effect.SetDuration(duration);

        source.Effects.Apply(source, effect, this);

        if (Sound.HasValue)
            context.TargetMap.PlaySound(Sound.Value, source);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Rage cost, attack speed bonus, and damage bonus all grow
    ///     each tier, per the design's "more attack speed, more Rage consumption, more devastating burst" note.
    /// </summary>
    private (int RageCost, int AtkSpeedPctBonus, int DamageBonus, TimeSpan Duration) GetTierValues() =>
        Subject.Level switch
        {
            <= 2 => (30, 15, 15, TimeSpan.FromSeconds(5)),
            <= 4 => (40, 20, 25, TimeSpan.FromSeconds(6)),
            <= 6 => (50, 25, 35, TimeSpan.FromSeconds(7)),
            _    => (60, 30, 50, TimeSpan.FromSeconds(8))
        };

    #region ScriptVars
    /// <summary>
    ///     Sound played on activation
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
