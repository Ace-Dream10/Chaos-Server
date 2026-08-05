using Chaos.Collections;
using Chaos.Collections.Common;
using Chaos.Messaging.Abstractions;
using Chaos.Models.World;
using Chaos.Networking.Abstractions;
using Chaos.Networking.Entities.Server;
using Chaos.Storage.Abstractions;

namespace Chaos.Messaging.Admin;

/// <summary>
///     TEMPORARY test-only command for the floor-tracker packet (Phase 2). Simulates "entering floor N" by setting
///     the caller's <c>currentFloor</c> counter directly and sending them the current
///     <see cref="Chaos.Networking.Abstractions.Definitions.ServerOpCode.AscensionFloorUpdate" /> state for that
///     floor. This is NOT the real floor-entry detection mechanism - that's still gated on the floor↔map
///     resolution convention decision (see FLOOR_TRACKER_DESIGN.md's open questions), which hasn't been made yet.
///     Once that's decided, real floor-entry detection (a gate reactor tile + polling AislingScript, per the
///     original design) replaces this command entirely.
/// </summary>
[Command("testfloorupdate", helpText: "<floorNumber> - TEMPORARY: simulates entering the given Ascension Chamber floor for packet testing")]
public class AscensionFloorTestCommand(IStore<AscensionFloorState> floorStore) : ICommand<Aisling>
{
    private readonly IStore<AscensionFloorState> FloorStore = floorStore;

    /// <inheritdoc />
    public ValueTask ExecuteAsync(Aisling source, ArgumentCollection args)
    {
        if (!args.TryGetNext<int>(out var floorNumber) || (floorNumber < 1) || (floorNumber > 10))
        {
            source.SendOrangeBarMessage("Usage: /testfloorupdate <1-10>");

            return default;
        }

        source.Trackers.Counters.Set("currentFloor", floorNumber);

        var key = floorNumber.ToString();
        var state = FloorStore.Exists(key) ? FloorStore.Load(key) : new AscensionFloorState { FloorNumber = floorNumber };

        var highestFloorCleared = source.Trackers.Counters.TryGetValue("highestFloorCleared", out var highest) ? highest : 0;

        source.Client.SendAscensionFloorUpdate(
            new AscensionFloorUpdateArgs
            {
                CurrentFloor = (byte)floorNumber,
                BossAlive = state.BossAlive,
                BossName = state.BossName,
                FirstClearers = state.FirstClearers,
                HighestFloorCleared = (byte)highestFloorCleared
            });

        source.SendOrangeBarMessage($"[TEST] Simulated entering floor {floorNumber} - floor-tracker packet sent.");

        return default;
    }
}
