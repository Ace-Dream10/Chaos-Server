using Chaos.Geometry;

namespace Chaos.Collections;

/// <summary>
///     Ownership record for a single player house, keyed by owner name (see <see cref="Services.Storage.HouseOwnershipStore" />).
///     Persisted directly (no separate schema type - this is a flat POCO with no domain logic, same convention as
///     <see cref="AscensionFloorState" />).
/// </summary>
/// <remarks>
///     Rent is checked on-demand (at the door/key entry point) rather than via a background sweep - see
///     <see cref="Utilities.HouseAccessHelper" />. There is deliberately no background job that reaches every house;
///     an owner who never attempts to enter again while overdue leaves their house locked and un-repossessed
///     indefinitely. This is a known, accepted consequence of the on-demand model, not an oversight - see the
///     housing design discussion for the tradeoff.
/// </remarks>
public sealed class HouseOwnership
{
    /// <summary>
    ///     The player name that owns this house. This is also the store key.
    /// </summary>
    public required string Owner { get; init; }

    /// <summary>
    ///     Names of players authorized to enter as guests, in addition to the owner.
    /// </summary>
    public List<string> Guests { get; set; } = [];

    /// <summary>
    ///     Where entering the house places the player. Null until a real house map/instance is assigned to this
    ///     owner (the purchase flow that assigns this is not implemented yet - see housing design notes).
    /// </summary>
    public Location? EntryLocation { get; set; }

    /// <summary>
    ///     The last time rent was paid. Defaults to creation time so a freshly-purchased house starts in a paid
    ///     state. Nothing currently updates this after creation - the "pay rent" action itself is not implemented
    ///     yet, only the check against it.
    /// </summary>
    public DateTime LastPaidUtc { get; set; } = DateTime.UtcNow;
}
