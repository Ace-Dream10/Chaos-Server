#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     One of Bard's 5 evolving abilities, and a consolidation of 3 previously-separate spells (Stacia's Armor,
///     the original flat Stacia's Blessing, Stacia's Veil) into one - see
///     <see cref="Chaos.Scripting.EffectScripts.StaciasBlessingEffect" />'s doc comment for why. Tiers per the
///     locked design's evolution track ("Defense -&gt; +HP -&gt; +Stats -&gt; +Damage Reduction -&gt; +CC
///     Resistance") mapped onto the floor schedule's 4 actual checkpoints (obtain, Floor4, Floor7, Floor10 max/
///     solo finale) - 5 named additions across 4 tiers, so the last two (Damage Reduction + CC Resistance) land
///     together at the max tier, same squeeze <see cref="Chaos.Scripting.SpellScripts.BattleHymnScript" /> uses.
///     Applied to every friendly Aisling on the map (same target-scan pattern the 3 source spells all shared).
///     All placeholder values, not balance-tested.
/// </summary>
public class StaciasBlessingScript : ConfigurableSpellScriptBase
{
    /// <inheritdoc />
    public StaciasBlessingScript(Spell subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var tier = GetTierValues();

        if (!source.StatSheet.TrySubtractMp(ManaCost))
        {
            context.SourceAisling?.SendOrangeBarMessage("Not enough mana.");

            return;
        }

        context.SourceAisling?.Client.SendAttributes(StatUpdateType.Vitality);

        source.AnimateBody(BodyAnimation);

        foreach (var aisling in map.GetEntities<Aisling>())
        {
            if (!Filter.IsValidTarget(source, aisling))
                continue;

            var blessingEffect = new StaciasBlessingEffect
            {
                AcBonus = tier.AcBonus,
                HpBonus = tier.HpBonus,
                MpBonus = tier.MpBonus,
                StrBonus = tier.StatBonus,
                DexBonus = tier.StatBonus,
                IntBonus = tier.StatBonus,
                WisBonus = tier.StatBonus,
                ConBonus = tier.StatBonus,
                DamageReductionPct = tier.DamageReductionPct,
                CcResistPct = tier.CcResistPct
            };
            aisling.Effects.Apply(source, blessingEffect, this);
        }

        if (Animation != null)
            map.ShowAnimation(Animation.GetPointAnimation(context.SourcePoint, source.Id));

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor1(Level&lt;=2)=I(obtain,Defense only),
    ///     Floor4(&lt;=8)=II(+HP), Floor7(&lt;=14)=III(+Stats), Floor10+(&gt;14)=IV(max/solo finale,+Damage
    ///     Reduction+CC Resistance).
    /// </summary>
    private (int AcBonus, int HpBonus, int MpBonus, int StatBonus, int DamageReductionPct, int CcResistPct) GetTierValues() =>
        Subject.Level switch
        {
            <= 2  => (-10, 0, 0, 0, 0, 0),
            <= 8  => (-15, 300, 0, 0, 0, 0),
            <= 14 => (-15, 300, 200, 5, 0, 0),
            _     => (-20, 500, 300, 5, 15, 25)
        };

    #region ScriptVars
    /// <summary>
    ///     The animation played at the caster's position
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine which Aislings on the map are valid buff targets
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The MP cost to use this spell
    /// </summary>
    public int ManaCost { get; init; }

    /// <summary>
    ///     Sound played at the caster's position on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
