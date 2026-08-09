#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
#endregion

namespace Chaos.Services.Factories.Abstractions;

/// <summary>
///     Supplies the spell templateKeys that make up each piece of a Sorcerer's modular kit - the "Tier II and
///     Tier III behave like interchangeable modules" framing from the locked design. Deliberately separate from
///     the granting mechanism (<see cref="Chaos.Scripting.DialogScripts" />/
///     <see cref="Chaos.Scripting.AislingScripts" />) and from the choice/order-resolution logic
///     (<see cref="Chaos.Utilities.SorcererProgressionHelper" />) so real content can be filled in later (Phase 2)
///     without touching the granting or resolution code at all - the granting code just asks "what am I supposed
///     to hand out for element X's Tier II" and doesn't care where the answer comes from.
/// </summary>
public interface ISorcererModuleProvider
{
    /// <summary>
    ///     The Shared Tier I spells every Sorcerer receives (Arcane Bolt, Arcane Gate, Shadow Bolt + Arcane
    ///     Precision passive), regardless of eventual specialization.
    /// </summary>
    IReadOnlyList<string> GetTierISpellKeys();

    /// <summary>
    ///     The 4-ability Tier II module (3 active + 1 passive) for the given element - granted whichever element is
    ///     picked FIRST, per <see cref="Chaos.Utilities.SorcererProgressionHelper.GetTierIIElement" />.
    /// </summary>
    IReadOnlyList<string> GetTierIISpellKeys(SorcererElement element);

    /// <summary>
    ///     The 4-ability Tier III module (3 active + 1 passive) for the given element - granted whichever element
    ///     is picked SECOND, per <see cref="Chaos.Utilities.SorcererProgressionHelper.GetTierIIIElement" />.
    /// </summary>
    IReadOnlyList<string> GetTierIIISpellKeys(SorcererElement element);

    /// <summary>
    ///     The 3-ability Tier IV signature kit for a fully-resolved specialization (no passive at Tier IV, per the
    ///     locked design's structural rule).
    /// </summary>
    IReadOnlyList<string> GetTierIVSpellKeys(AdvClass specialization);
}
