#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     One of Bard's 5 evolving abilities, and a consolidation - the locked design's "Stacia's Blessing ⭐ Evolving
///     defensive blessing (Armor + Blessing + Veil)" explicitly merges 3 previously-separate spells (Stacia's
///     Armor's AC bonus, the original Stacia's Blessing's stat/HP/MP bonus, Stacia's Veil's damage reduction) into
///     ONE evolving ability's tiers, plus a new Tier IV addition (CC Resistance) with no legacy precedent. Tiers
///     set by <see cref="Chaos.Scripting.SpellScripts.StaciasBlessingScript" /> before applying - see that script's
///     doc comment for the exact tier breakdown. <see cref="DamageReductionPct" /> is read directly by
///     <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" /> (same hook Veil used);
///     <see cref="CcResistPct" /> is read by <see cref="RootEffect" />/<see cref="SlowEffect" />'s ShouldApply -
///     scoped to just those two crowd-control effects rather than every debuff in the game, flagged as a
///     simplification. All placeholder values, not balance-tested.
/// </summary>
public sealed class StaciasBlessingEffect : EffectBase
{
    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(1800000);

    public int AcBonus { get; set; }
    public int CcResistPct { get; set; }
    public int ConBonus { get; set; }
    public int DamageReductionPct { get; set; }
    public int DexBonus { get; set; }
    public int HpBonus { get; set; }
    public int IntBonus { get; set; }
    public int MpBonus { get; set; }
    public int StrBonus { get; set; }
    public int WisBonus { get; set; }

    /// <inheritdoc />
    public override byte Icon => 56;

    /// <inheritdoc />
    public override string Name => "Stacia's Blessing";

    /// <inheritdoc />
    public override void OnApplied()
        => Subject.StatSheet.AddBonus(
            new Attributes
            {
                Ac = AcBonus,
                Str = StrBonus,
                Dex = DexBonus,
                Int = IntBonus,
                Wis = WisBonus,
                Con = ConBonus,
                MaximumHp = HpBonus,
                MaximumMp = MpBonus
            });

    /// <inheritdoc />
    public override void OnTerminated()
        => Subject.StatSheet.SubtractBonus(
            new Attributes
            {
                Ac = AcBonus,
                Str = StrBonus,
                Dex = DexBonus,
                Int = IntBonus,
                Wis = WisBonus,
                Con = ConBonus,
                MaximumHp = HpBonus,
                MaximumMp = MpBonus
            });
}
