#region
using Chaos.Collections;
using Chaos.Extensions.Common;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Networking.Abstractions;
using Chaos.Networking.Entities.Server;
using Chaos.Scripting.MonsterScripts.Abstractions;
using Chaos.Storage.Abstractions;
using Microsoft.Extensions.Logging;
#endregion

namespace Chaos.Scripting.MonsterScripts;

/// <summary>
///     Marks a monster as an Ascension Chamber floor boss and, on death, resolves first-clearer credit for that
///     floor. Purely additive alongside the standard <see cref="DeathScript" /> - attach both via
///     <c>scriptKeys: ["Death", "AscensionBossDeath"]</c>.
/// </summary>
/// <remarks>
///     Pet/summon damage is not credited to an owner in this pass - see FLOOR_TRACKER_DESIGN.md §6's "Known
///     follow-up" note. Only <see cref="Aisling" /> sources/targets are captured.
/// </remarks>
public class AscensionBossDeathScript : ConfigurableMonsterScriptBase
{
    private readonly IClientRegistry<IChaosWorldClient> ClientRegistry;
    private readonly IStore<AscensionFloorState> FloorStore;
    private readonly ILogger<AscensionBossDeathScript> Logger;

    /// <inheritdoc />
    public AscensionBossDeathScript(
        Monster subject,
        IStore<AscensionFloorState> floorStore,
        IClientRegistry<IChaosWorldClient> clientRegistry,
        ILogger<AscensionBossDeathScript> logger)
        : base(subject)
    {
        FloorStore = floorStore;
        ClientRegistry = clientRegistry;
        Logger = logger;

        //tag the monster as a tracked boss for this floor - AscensionFloorTrackerScript checks this tag to know
        //whether an aisling was just hit by a tracked boss
        Subject.Trackers.Tags["ascensionFloor"] = FloorNumber.ToString();
    }

    /// <inheritdoc />
    public override void OnAttacked(Creature source, int damage, int? aggroOverride)
    {
        if (source is Aisling aisling)
            Subject.AscensionParticipants.Add(aisling);
    }

    /// <inheritdoc />
    public override void OnDeath()
    {
        var key = FloorNumber.ToString();
        var state = FloorStore.Exists(key) ? FloorStore.Load(key) : new AscensionFloorState { FloorNumber = FloorNumber };

        state.BossAlive = false;
        state.BossName = Subject.Template.Name;

        var participants = Subject.AscensionParticipants.ToArray();

        if (state.FirstClearers.Count == 0)
        {
            state.FirstClearers = participants.ToList();
            state.FirstClearedAt = DateTime.UtcNow;

            foreach (var participantName in participants)
            {
                var participant = ClientRegistry.Select(c => c.Aisling)
                                                 .FirstOrDefault(a => a.Name.EqualsI(participantName));

                if (participant is null)
                    continue;

                var currentHighest = participant.Trackers.Counters.TryGetValue("highestFloorCleared", out var highest) ? highest : 0;

                if (FloorNumber > currentHighest)
                    participant.Trackers.Counters.Set("highestFloorCleared", FloorNumber);
            }

            Logger.LogInformation(
                "AscensionBossDeath: floor {FloorNumber} boss {BossName} died - first clear, {ParticipantCount} participant(s): {Participants}",
                FloorNumber,
                state.BossName,
                participants.Length,
                string.Join(", ", participants));
        } else
            Logger.LogInformation(
                "AscensionBossDeath: floor {FloorNumber} boss {BossName} died - already cleared, {ParticipantCount} participant(s) this kill did not affect FirstClearers",
                FloorNumber,
                state.BossName,
                participants.Length);

        FloorStore.Save(state);

        //broadcast to everyone currently on this floor (global state, so this covers every shard/instance of the
        //floor's map, not just the one this boss died on)
        foreach (var client in ClientRegistry)
        {
            var recipient = client.Aisling;

            if (!recipient.Trackers.Counters.TryGetValue("currentFloor", out var recipientFloor) || (recipientFloor != FloorNumber))
                continue;

            var highestFloorCleared = recipient.Trackers.Counters.TryGetValue("highestFloorCleared", out var highest) ? highest : 0;

            client.SendAscensionFloorUpdate(
                new AscensionFloorUpdateArgs
                {
                    CurrentFloor = (byte)FloorNumber,
                    BossAlive = state.BossAlive,
                    BossName = state.BossName,
                    FirstClearers = state.FirstClearers,
                    HighestFloorCleared = (byte)highestFloorCleared
                });
        }
    }

    #region ScriptVars
    /// <summary>
    ///     The Ascension Chamber floor number this boss belongs to (1-10)
    /// </summary>
    public int FloorNumber { get; init; }
    #endregion
}
