#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Services.Factories;

/// <summary>
///     Production <see cref="ISorcererModuleProvider" />. Phase 2 status: Shared Tier I and the full Fire element
///     (Tier II + Tier III) are real content - Fire was chosen as the proof-case element per the agreed build
///     order (most existing reusable content, and Ignis is Phase 2's target). Every other element/specialization
///     still returns an empty list - not a bug, the remaining 10 paths are later phases, per the same
///     "populate this one file without touching granting/resolution code" design from Phase 1.
/// </summary>
public sealed class SorcererModuleProvider : ISorcererModuleProvider
{
    //3 items, not 4 - Shadow Bolt was removed from the locked design entirely (see ELYSIUM_CLASS_DESIGN.md's
    //Shared Tier I table and Structural rule, both updated alongside this). Every path is now 14 total abilities
    //(11 active + 3 passive) instead of 15, until a deferred future project designs 11 new Tier IV abilities (one
    //per path) to restore the count - that future work is explicitly NOT happening now, just tracked.
    private static readonly IReadOnlyList<string> TierISpellKeys =
    [
        "arcane_bolt",
        "arcane_gate",
        "arcane_precision"
    ];

    private static readonly IReadOnlyList<string> FireTierIISpellKeys =
    [
        "firebolt",
        "solar_flare",
        "fire_wall",
        "kindling"
    ];

    private static readonly IReadOnlyList<string> FireTierIIISpellKeys =
    [
        "flame_lance",
        "fire_shield",
        "combustion",
        "scorch"
    ];

    private static readonly IReadOnlyList<string> IgnisTierIVSpellKeys =
    [
        "meteor",
        "ember_field",
        "phoenix_rise"
    ];

    /// <inheritdoc />
    public IReadOnlyList<string> GetTierISpellKeys() => TierISpellKeys;

    /// <inheritdoc />
    public IReadOnlyList<string> GetTierIISpellKeys(SorcererElement element) =>
        element switch
        {
            SorcererElement.Fire => FireTierIISpellKeys,
            _                    => []
        };

    /// <inheritdoc />
    public IReadOnlyList<string> GetTierIIISpellKeys(SorcererElement element) =>
        element switch
        {
            SorcererElement.Fire => FireTierIIISpellKeys,
            _                    => []
        };

    /// <inheritdoc />
    public IReadOnlyList<string> GetTierIVSpellKeys(AdvClass specialization) =>
        specialization switch
        {
            AdvClass.Ignis => IgnisTierIVSpellKeys,
            _              => []
        };
}
