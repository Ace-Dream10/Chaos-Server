using Chaos.Networking.Abstractions.Definitions;

namespace Chaos.Networking.Entities.Server;

/// <summary>
///     Represents the serialization of a skill in the <see cref="ServerOpCode.AddSkillToPane" /> and
///     <see cref="ServerOpCode.DisplayMenu" /> packets
/// </summary>
public sealed record SkillInfo
{
    /// <summary>
    ///     A brief description of the skill, shown as a second line in the action-bar hover tooltip. Empty string
    ///     when the skill has no description set.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Whether this skill is a display-only passive entry - its actual mechanic (if any) is delivered by an
    ///     always-on script, independent of this flag. Purely informational for the client tooltip.
    /// </summary>
    public bool IsPassive { get; set; }

    /// <summary>
    ///     The name of the skill
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    ///     The text that appears when you hover this skill on the skill panel
    /// </summary>
    public string PanelName { get; set; } = null!;

    /// <summary>
    ///     The slot the skill is in
    /// </summary>
    public byte Slot { get; set; }

    /// <summary>
    ///     The sprite of the skill icon
    /// </summary>
    public ushort Sprite { get; set; }
}