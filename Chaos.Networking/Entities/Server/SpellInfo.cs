using Chaos.DarkAges.Definitions;
using Chaos.Networking.Abstractions.Definitions;

namespace Chaos.Networking.Entities.Server;

/// <summary>
///     Represents the serialization of a spell in the <see cref="ServerOpCode.AddSpellToPane" /> and
///     <see cref="ServerOpCode.DisplayMenu" /> packets
/// </summary>
public sealed record SpellInfo
{
    /// <summary>
    ///     The number of castlines the spell has
    /// </summary>
    public byte CastLines { get; set; }

    /// <summary>
    ///     A brief description of the spell, shown as a second line in the action-bar hover tooltip. Empty string
    ///     when the spell has no description set.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Whether this spell is a display-only passive entry - its actual mechanic (if any) is delivered by an
    ///     always-on script, independent of this flag. Purely informational for the client tooltip.
    /// </summary>
    public bool IsPassive { get; set; }

    /// <summary>
    ///     The name of the spell
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    ///     The text that appears when you hover this spell on the spell panel
    /// </summary>
    public string PanelName { get; set; } = null!;

    /// <summary>
    ///     If the spell has a prompt, this is that prompt
    /// </summary>
    public string Prompt { get; set; } = null!;

    /// <summary>
    ///     The slot the spell is in
    /// </summary>
    public byte Slot { get; set; }

    /// <summary>
    ///     The type of spell
    /// </summary>
    public SpellType SpellType { get; set; }

    /// <summary>
    ///     The sprite of the spell icon
    /// </summary>
    public ushort Sprite { get; set; }
}