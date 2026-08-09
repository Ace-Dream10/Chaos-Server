#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Services.Factories;

/// <summary>
///     Production <see cref="ISorcererModuleProvider" />. Deliberately empty for now - this is Phase 1
///     (infrastructure) of Sorcerer's build, not Phase 2 (content). Every method returns an empty list rather than
///     guessed-at templateKeys, so the granting mechanism (dialogs/<see cref="Chaos.Scripting.AislingScripts" />)
///     can be built and tested against a real, working, empty-content Sorcerer today, and Phase 2 fills these in
///     (starting with Shared Tier I, then Ignis as the full proof case, per the agreed build order) without
///     touching any granting/resolution code - only this file changes.
/// </summary>
public sealed class SorcererModuleProvider : ISorcererModuleProvider
{
    /// <inheritdoc />
    public IReadOnlyList<string> GetTierISpellKeys() => [];

    /// <inheritdoc />
    public IReadOnlyList<string> GetTierIISpellKeys(SorcererElement element) => [];

    /// <inheritdoc />
    public IReadOnlyList<string> GetTierIIISpellKeys(SorcererElement element) => [];

    /// <inheritdoc />
    public IReadOnlyList<string> GetTierIVSpellKeys(AdvClass specialization) => [];
}
