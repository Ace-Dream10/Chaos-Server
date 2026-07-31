using Chaos.DarkAges.Definitions;

namespace Chaos.Formulae.Abstractions;

/// <summary>
///     Implemented by damage formulae that model elemental strengths/weaknesses, allowing callers to ask which
///     offense element performs best against a given defense element (used by adaptive-element gear).
/// </summary>
public interface IElementalDamageFormula
{
    /// <summary>
    ///     Returns the non-None offense element with the highest damage multiplier against the given defense element
    /// </summary>
    Element GetBestOffenseElement(Element defenseElement);
}
