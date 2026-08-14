#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     One of Bard's 5 evolving abilities, and a consolidation - the locked design's "Battle Hymn ⭐ Evolving
///     offensive party buff (Attack + Accuracy + Speed + Crit)" explicitly merges 2 previously-separate spells
///     (Stacia's Hymn's flat damage bonus, Stacia's March's attack speed bonus) into ONE evolving ability's tiers,
///     plus 2 new Tier-IV additions (Critical chance, Mana Regeneration) with no legacy precedent. Tiers set by
///     <see cref="Chaos.Scripting.SpellScripts.BattleHymnScript" /> before applying.
///     <see cref="CritChanceBonusPct" /> is read the same scoped way Fletcher's Spotter's Brand/Windrunor crit tags
///     are (see ApplyAttackDamageScript's crit-roll hook); <see cref="MpRegenPerTick" /> ticks directly in this
///     effect's own <see cref="Update" /> rather than needing a new shared regen system. All placeholder values,
///     not balance-tested.
/// </summary>
public sealed class BattleHymnEffect : EffectBase
{
    public const string CritChanceBonusTag = "battleHymnCritBonus";
    private static readonly TimeSpan MpTickInterval = TimeSpan.FromSeconds(2);

    private TimeSpan SinceLastMpTick = TimeSpan.Zero;

    /// <summary>
    ///     Bumped from 30000 (30s) to 180000 (3min) per playtest feedback ("Battle Hymn's duration feels too
    ///     short") - 30s read as barely worth casting for a party-wide multi-stat buff. Placeholder, not
    ///     balance-tested; still well short of Stacia's Blessing's 30-minute buff, keeping some differentiation
    ///     between the two.
    /// </summary>
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(180000);

    public int AtkSpeedBonus { get; set; }
    public int CritChanceBonusPct { get; set; }
    public int FlatDamageBonus { get; set; }
    public int FlatSpellBonus { get; set; }
    public int HitBonus { get; set; }
    public int MpRegenPerTick { get; set; }

    /// <inheritdoc />
    public override byte Icon => 61;

    /// <inheritdoc />
    public override string Name => "Battle Hymn";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.StatSheet.AddBonus(
            new Attributes
            {
                FlatSkillDamage = FlatDamageBonus,
                FlatSpellDamage = FlatSpellBonus,
                Hit = HitBonus,
                AtkSpeedPct = AtkSpeedBonus
            });

        if (CritChanceBonusPct > 0)
            Subject.Trackers.Tags[CritChanceBonusTag] = CritChanceBonusPct.ToString();
    }

    /// <inheritdoc />
    public override void OnTerminated()
    {
        Subject.StatSheet.SubtractBonus(
            new Attributes
            {
                FlatSkillDamage = FlatDamageBonus,
                FlatSpellDamage = FlatSpellBonus,
                Hit = HitBonus,
                AtkSpeedPct = AtkSpeedBonus
            });

        Subject.Trackers.Tags.TryRemove(CritChanceBonusTag, out _);
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        base.Update(delta);

        if (MpRegenPerTick <= 0)
            return;

        SinceLastMpTick += delta;

        if (SinceLastMpTick < MpTickInterval)
            return;

        SinceLastMpTick = TimeSpan.Zero;
        Subject.StatSheet.AddMp(MpRegenPerTick);

        if (Subject is Aisling aisling)
            aisling.Client.SendAttributes(StatUpdateType.Vitality);
    }
}
