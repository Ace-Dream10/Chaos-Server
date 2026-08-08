using System.Text.Json.Serialization;
using Chaos.DarkAges.Definitions;
using Chaos.Schemas.Data;
using Chaos.Schemas.Templates.Abstractions;

namespace Chaos.Schemas.Templates;

/// <summary>
///     Represents the serializable schema for a spell template
/// </summary>
public sealed record SpellTemplateSchema : PanelEntityTemplateSchema
{
    /// <summary>
    ///     Defaults to false. If true, only an admin (<c>Aisling.IsAdmin</c>) can cast this spell - checked in
    ///     <c>Creature.CanUse(Spell, ...)</c> alongside the other universal pre-cast checks. Replaces the old
    ///     convention of restricting GM-only spells via a fake <c>BaseClass</c> value (e.g. the removed
    ///     <c>Diacht</c>), which had no real enforcement at cast time and only worked by nobody ever teaching the
    ///     spell to a non-admin.
    /// </summary>
    public bool AdminOnly { get; set; }

    /// <summary>
    ///     The number of chant lines this spell requires by default
    /// </summary>
    public byte CastLines { get; set; }

    /// <summary>
    ///     Defaults to false. Only meaningful when <see cref="SpellType" /> is <see cref="Chaos.DarkAges.Definitions.SpellType.Targeted" />.
    ///     If true, this spell can be cast at an empty map point with no entity under the cursor (the client sends
    ///     entity ID 0 alongside the point in that case) - see <c>Aisling.TryUseSpell</c>. Existing Targeted spells
    ///     default to false and are unaffected; this is an explicit opt-in per spell, not a blanket capability
    ///     unlocked for every Targeted spell.
    /// </summary>
    public bool GroundTargeted { get; set; }

    /// <summary>
    ///     Defaults to false. Marks this spell as a display-only entry for a passive mechanic - mirrors
    ///     SkillTemplateSchema.IsPassive. No spell-based passive exists yet; added here for wire-format parity.
    /// </summary>
    public bool IsPassive { get; set; }

    /// <summary>
    ///     Default null
    ///     <br />
    ///     If set, these are the requirements for the spell to be learned
    /// </summary>
    /// <remarks>
    ///     this is a test
    /// </remarks>
    public LearningRequirementsSchema? LearningRequirements { get; set; }

    /// <summary>
    ///     Whether or not the spell is capable of leveling up. If false, the spell will start at level 100
    /// </summary>
    public bool LevelsUp { get; set; }

    /// <summary>
    ///     Defaults to null
    ///     <br />
    ///     If set, this is the maximum level the spell can be leveled up to
    /// </summary>
    public byte? MaxLevel { get; set; }

    /// <summary>
    ///     Defaults to null
    ///     <br />
    ///     Should be specified with a spell type of "Prompt", this is the prompt the spell will offer when used in game
    /// </summary>
    public string? Prompt { get; set; }

    /// <summary>
    ///     The way the spell is cast by the player
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public SpellType SpellType { get; set; }
}