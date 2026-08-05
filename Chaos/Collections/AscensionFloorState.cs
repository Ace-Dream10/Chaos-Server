namespace Chaos.Collections;

/// <summary>
///     Global, server-wide state for a single Ascension Chamber floor - boss status and first-clearer record. One of
///     these exists per floor number, independent of how many shards/instances of that floor's map exist. Persisted
///     directly (no separate schema type - this is a flat POCO with no domain logic, so
///     <see cref="Chaos.Storage.Abstractions.IEntityRepository" />'s non-mapping Save/Load overloads are used as-is).
/// </summary>
public sealed class AscensionFloorState
{
    public int FloorNumber { get; init; }
    public bool BossAlive { get; set; } = true;
    public string? BossName { get; set; }

    /// <summary>
    ///     Names of every Aisling that participated in the fight against this floor's boss the first time it died -
    ///     set once, never appended to afterwards. Empty until the floor has been cleared.
    /// </summary>
    public List<string> FirstClearers { get; set; } = [];

    public DateTime? FirstClearedAt { get; set; }
}
