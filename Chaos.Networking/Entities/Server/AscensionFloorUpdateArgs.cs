#region
using Chaos.Packets.Abstractions;
#endregion

namespace Chaos.Networking.Entities.Server;

/// <summary>
///     Represents the serialization of the <see cref="Chaos.Networking.Abstractions.Definitions.ServerOpCode.AscensionFloorUpdate" />
///     packet - the Ascension Chamber floor-tracker HUD state for a single floor.
/// </summary>
public sealed record AscensionFloorUpdateArgs : IPacketSerializable
{
    /// <summary>
    ///     The floor number this update concerns (1-10). 0 means the recipient is not currently on an Ascension
    ///     Chamber floor.
    /// </summary>
    public required byte CurrentFloor { get; set; }

    /// <summary>
    ///     Whether this floor's boss is currently alive
    /// </summary>
    public required bool BossAlive { get; set; }

    /// <summary>
    ///     The name of this floor's boss, if known
    /// </summary>
    public string? BossName { get; set; }

    /// <summary>
    ///     Names of every participant credited with this floor's first clear. Empty if the floor has not been
    ///     cleared yet.
    /// </summary>
    public required List<string> FirstClearers { get; set; }

    /// <summary>
    ///     The highest floor number the recipient has ever cleared (0-10). 0 means none cleared yet.
    /// </summary>
    public required byte HighestFloorCleared { get; set; }
}
