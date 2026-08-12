#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Models.World;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Utilities;

/// <summary>
///     Pure data-access and resolution logic for a Sorcerer's element-choice progression. Kept separate from the
///     granting orchestration (which lives in <see cref="Chaos.Scripting.DialogScripts" />/
///     <see cref="Chaos.Scripting.AislingScripts" /> - see their doc comments) so the order-sensitive resolution
///     math is independently testable without needing a full dialog/script harness.
/// </summary>
/// <remarks>
///     Persistence: the two element picks live on <see cref="Chaos.Collections.Trackers.Counters" /> under
///     <see cref="Element1CounterKey" />/<see cref="Element2CounterKey" /> - the established pattern for "simple
///     named int state on a player" per FLOOR_TRACKER_DESIGN.md's own precedent (the Ascension Chamber's
///     "currentFloor"/"highestFloorCleared" counters), confirmed
///     <c>IS PERSISTENT / SERIALIZED TO FILE</c> on <see cref="Chaos.Collections.Trackers.Counters" />'s own doc
///     comment. Deliberately not <see cref="Chaos.Collections.Trackers.Enums" /> (the pattern the old, now-retired
///     ChooseBeastFormScript used for MartialArtist's beast-flavor choice, before it was superseded by
///     <see cref="Chaos.Scripting.DialogScripts.ChooseSpecializationScript" />'s direct
///     <see cref="Chaos.Models.Data.UserStatSheet.AdvClass" /> write) - <c>EnumCollection</c> stores exactly one
///     value per enum TYPE, and this needs two independently-tracked values of the same
///     <see cref="SorcererElement" /> type, which would otherwise require two duplicate marker enum types just to
///     fit that shape.
///     <para>
///         The FINAL derived specialization is not stored separately at all - it's written directly to the
///         already-real, already-persisted <see cref="Chaos.Models.Data.UserStatSheet.AdvClass" /> (via
///         <see cref="Chaos.Models.Data.UserStatSheet.SetAdvClass" />) once both picks are known, the same field
///         MartialArtist's Floor-2 specialization is designed to use. No new "SorcererSpecialization" enum was
///         added - <see cref="AdvClass" /> already has all 11 Sorcerer values (Ignis/Earthshaper/Hydrosage/Gale/
///         Arcanist/Magma/Cinder/Inferno/Torrent/Sirocco/Blizzard).
///     </para>
/// </remarks>
public static class SorcererProgressionHelper
{
    /// <summary>
    ///     The character level at which a Sorcerer may make their first elemental pick (Tier II unlock). Mirrors
    ///     <c>AdvClass</c>'s own doc comment ("chosen progressively at Floor 3/5/7") - Level is the same temporary
    ///     stand-in for real floor-gating that every other class's evolving abilities already use (floor maps
    ///     mostly don't exist yet, per FLOOR_TRACKER_DESIGN.md's own floor-map inventory), not a new convention
    ///     invented for Sorcerer specifically.
    /// </summary>
    public const int FirstChoiceLevel = 3;

    /// <summary>
    ///     The character level at which a Sorcerer may make their second elemental pick (Tier III unlock, and the
    ///     point their final specialization becomes known). See <see cref="FirstChoiceLevel" />.
    /// </summary>
    public const int SecondChoiceLevel = 5;

    /// <summary>
    ///     The character level at which a fully-specialized Sorcerer's Tier IV signature kit is auto-granted. See
    ///     <see cref="FirstChoiceLevel" />.
    /// </summary>
    public const int TierIVLevel = 7;

    private const string Element1CounterKey = "sorcererElement1";
    private const string Element2CounterKey = "sorcererElement2";

    /// <summary>
    ///     Resolves the final <see cref="AdvClass" /> specialization from an ordered pair of element picks.
    /// </summary>
    /// <remarks>
    ///     Order affects WHICH content the player received (see <see cref="GetTierIIElement" />/
    ///     <see cref="GetTierIIIElement" />) but not the final specialization identity - per the locked design's
    ///     Hybrid Construction Rule, Fire-then-Earth and Earth-then-Fire both resolve to Magma. Picking the SAME
    ///     element twice resolves to that element's pure path - this is the intended, and only, way to reach a
    ///     pure path (there is no separate "stay pure" flag; picking your first element again as your second pick
    ///     IS staying pure, mechanically and by design - see the dialog scripts for how the menu surfaces this).
    /// </remarks>
    public static AdvClass ResolveSpecialization(SorcererElement first, SorcererElement second)
    {
        if (first == second)
            return first switch
            {
                SorcererElement.Fire   => AdvClass.Ignis,
                SorcererElement.Earth  => AdvClass.Earthshaper,
                SorcererElement.Water  => AdvClass.Hydrosage,
                SorcererElement.Wind   => AdvClass.Gale,
                SorcererElement.Arcane => AdvClass.Arcanist,
                _                      => throw new ArgumentOutOfRangeException(nameof(first), first, null)
            };

        //Hybrid outcome depends only on the unordered pair - per the Hybrid Construction Rule, both orderings of
        //the same two elements reach the same Tier IV specialization
        return (first, second) switch
        {
            (SorcererElement.Fire, SorcererElement.Earth) or (SorcererElement.Earth, SorcererElement.Fire) => AdvClass.Magma,
            (SorcererElement.Fire, SorcererElement.Water) or (SorcererElement.Water, SorcererElement.Fire) => AdvClass.Cinder,
            (SorcererElement.Fire, SorcererElement.Wind) or (SorcererElement.Wind, SorcererElement.Fire) => AdvClass.Inferno,
            (SorcererElement.Earth, SorcererElement.Water) or (SorcererElement.Water, SorcererElement.Earth) => AdvClass.Torrent,
            (SorcererElement.Earth, SorcererElement.Wind) or (SorcererElement.Wind, SorcererElement.Earth) => AdvClass.Sirocco,
            (SorcererElement.Water, SorcererElement.Wind) or (SorcererElement.Wind, SorcererElement.Water) => AdvClass.Blizzard,
            //Arcane paired with anything else has no locked hybrid per the design doc (Arcanist is Arcane+Arcane
            //only, "the ONLY specialization that evolves a Tier I ability") - not a guessed gap, the design doc's
            //own Hybrid Specialization Summary table has no Arcane row at all.
            _ => throw new ArgumentOutOfRangeException(
                nameof(second),
                second,
                $"No hybrid specialization exists for {first} + {second} - Arcane only combines with itself (Arcanist).")
        };
    }

    /// <summary>
    ///     The element whose Tier II module (4 abilities: 3 active + 1 passive) the player receives - always the
    ///     FIRST pick, per the Hybrid Construction Rule ("a hybrid uses ... Tier II from its FIRST chosen
    ///     element").
    /// </summary>
    public static SorcererElement GetTierIIElement(SorcererElement first, SorcererElement second) => first;

    /// <summary>
    ///     The element whose Tier III module (4 abilities: 3 active + 1 passive) the player receives - always the
    ///     SECOND pick, per the Hybrid Construction Rule ("... + Tier III from its SECOND chosen element").
    /// </summary>
    public static SorcererElement GetTierIIIElement(SorcererElement first, SorcererElement second) => second;

    /// <summary>
    ///     Reads the player's first elemental pick, if any has been made yet.
    /// </summary>
    public static bool TryGetElement1(Aisling source, out SorcererElement element1)
    {
        element1 = default;

        if (!source.Trackers.Counters.TryGetValue(Element1CounterKey, out var raw))
            return false;

        element1 = (SorcererElement)raw;

        return true;
    }

    /// <summary>
    ///     Reads the player's second elemental pick, if any has been made yet.
    /// </summary>
    public static bool TryGetElement2(Aisling source, out SorcererElement element2)
    {
        element2 = default;

        if (!source.Trackers.Counters.TryGetValue(Element2CounterKey, out var raw))
            return false;

        element2 = (SorcererElement)raw;

        return true;
    }

    /// <summary>
    ///     Persists the player's first elemental pick. Does not grant anything or touch AdvClass - see
    ///     <see cref="Chaos.Scripting.DialogScripts" /> for the granting orchestration.
    /// </summary>
    public static void SetElement1(Aisling source, SorcererElement element1)
        => source.Trackers.Counters.Set(Element1CounterKey, (int)element1);

    /// <summary>
    ///     Persists the player's second elemental pick. Does not grant anything or touch AdvClass - see
    ///     <see cref="Chaos.Scripting.DialogScripts" /> for the granting orchestration.
    /// </summary>
    public static void SetElement2(Aisling source, SorcererElement element2)
        => source.Trackers.Counters.Set(Element2CounterKey, (int)element2);

    /// <summary>
    ///     Grants each spell templateKey to the source via the real production path
    ///     (<see cref="Chaos.Utilities.ComplexActionHelper.LearnSpell" />, which already handles routing true
    ///     passives to Page3/the H tab). Mirrors the old, now-deleted SorcererSpecializeScript test scaffolding's
    ///     graceful-skip behavior for a templateKey that doesn't exist yet - expected and harmless while
    ///     <see cref="ISorcererModuleProvider" />'s production implementation is still empty (Phase 1) or partially
    ///     filled in (early Phase 2), not a sign of a bug.
    /// </summary>
    public static void GrantSpells(Aisling source, ISpellFactory spellFactory, IEnumerable<string> templateKeys)
    {
        foreach (var templateKey in templateKeys)
        {
            try
            {
                var spell = spellFactory.Create(templateKey);
                ComplexActionHelper.LearnSpell(source, spell);
            } catch
            {
                //spell template doesn't exist yet - skip gracefully, see doc comment above
            }
        }
    }
}
