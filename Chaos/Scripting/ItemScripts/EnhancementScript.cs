#region
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.ItemScripts.Abstractions;
#endregion

namespace Chaos.Scripting.ItemScripts;

/// <summary>
///     Tracks and applies an item's enhancement tier (+1 to +9). This is a foundation for a larger
///     enhancement system - material costs, failure chance, and per-slot rules are not implemented yet.
/// </summary>
public class EnhancementScript : ItemScriptBase
{
    /// <summary>
    ///     The highest enhancement tier an item can reach
    /// </summary>
    public const int MaxEnhancementLevel = 9;

    /// <summary>
    ///     The percentage, per enhancement tier, that each base modifier is boosted by
    /// </summary>
    private const decimal BonusPerTier = 0.1m;

    /// <inheritdoc />
    public EnhancementScript(Item subject)
        : base(subject) { }

    /// <summary>
    ///     Whether this item is eligible to be enhanced further
    /// </summary>
    public bool CanEnhance => Subject.Template.IsModifiable && (Subject.EnhancementLevel < MaxEnhancementLevel);

    /// <summary>
    ///     The item's current enhancement tier
    /// </summary>
    public int EnhancementLevel => Subject.EnhancementLevel;

    /// <summary>
    ///     Increments the item's enhancement tier and recalculates its modifiers from the template's base
    ///     modifiers. Returns false if the item can't be enhanced any further (not modifiable, or already
    ///     at the cap).
    /// </summary>
    public bool TryEnhance()
    {
        if (!CanEnhance)
            return false;

        Subject.EnhancementLevel++;
        RecalculateModifiers();

        return true;
    }

    /// <summary>
    ///     Recomputes <see cref="Item.Modifiers" /> from the template's base modifiers and the item's current
    ///     <see cref="Item.EnhancementLevel" />. Public so callers that restore <see cref="Item.EnhancementLevel" />
    ///     directly (e.g. loading a saved item, which persists the level but not the resulting modifiers) can bring
    ///     modifiers back into sync without going through <see cref="TryEnhance" />.
    /// </summary>
    public void RecalculateModifiers()
    {
        var baseModifiers = Subject.Template.Modifiers;

        if (baseModifiers is null)
        {
            Subject.Modifiers = new Attributes();

            return;
        }

        var level = Subject.EnhancementLevel;

        Subject.Modifiers = new Attributes
        {
            Str = ApplyBonus(baseModifiers.Str, level),
            Dex = ApplyBonus(baseModifiers.Dex, level),
            Int = ApplyBonus(baseModifiers.Int, level),
            Wis = ApplyBonus(baseModifiers.Wis, level),
            Con = ApplyBonus(baseModifiers.Con, level),
            Ac = ApplyBonus(baseModifiers.Ac, level),
            Dmg = ApplyBonus(baseModifiers.Dmg, level),
            Hit = ApplyBonus(baseModifiers.Hit, level),
            MagicResistance = ApplyBonus(baseModifiers.MagicResistance, level),
            MaximumHp = ApplyBonus(baseModifiers.MaximumHp, level),
            MaximumMp = ApplyBonus(baseModifiers.MaximumMp, level),
            AtkSpeedPct = baseModifiers.AtkSpeedPct,
            SkillDamagePct = baseModifiers.SkillDamagePct,
            SpellDamagePct = baseModifiers.SpellDamagePct,
            FlatSkillDamage = baseModifiers.FlatSkillDamage,
            FlatSpellDamage = baseModifiers.FlatSpellDamage
        };
    }

    /// <summary>
    ///     baseValue + roughly 10% of baseValue per enhancement tier (e.g. a +5 stat becomes +5.5 -> +6 at
    ///     tier 1, +6 at tier 2, ... +9.5 -> +10 at tier 9)
    /// </summary>
    private static int ApplyBonus(int baseValue, int enhancementLevel)
        => baseValue + (int)Math.Round(baseValue * enhancementLevel * BonusPerTier, MidpointRounding.AwayFromZero);
}
