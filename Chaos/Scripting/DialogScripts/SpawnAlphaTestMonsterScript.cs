#region
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Models.Menu;
using Chaos.Models.World;
using Chaos.Scripting.DialogScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.DialogScripts;

/// <summary>
///     Spawns a tester-facing monster (Easy/Mid/Hard) at a random walkable point within a fixed area on the alpha
///     test map, on a per-player cooldown shared across all three difficulties (so picking a different tier doesn't
///     bypass the wait). Not tied to the automatic interval-based <see cref="Chaos.Models.Data.MonsterSpawn" />
///     system at all - reuses the same <see cref="IMonsterFactory" />.Create + MapInstance.AddEntity pattern as the
///     GM-only <c>/spawnMonster</c> admin command (<see cref="Chaos.Messaging.Admin.SpawnMonsterCommand" />), just
///     triggered from a dialog instead of a command, and constrained to a defined rectangle instead of "at my feet".
/// </summary>
public class SpawnAlphaTestMonsterScript : ConfigurableDialogScriptBase
{
    /// <summary>
    ///     The monster spawn area on map20004 - roughly (18,37) to (32,7) as originally specified, expressed as a
    ///     Left/Top/Width/Height rectangle.
    /// </summary>
    private static readonly Rectangle SpawnArea = new(
        left: 18,
        top: 7,
        width: 14,
        height: 30);

    private const string CooldownCounterKey = "alphaTestMonsterSpawnReadyAt";

    private readonly IMonsterFactory MonsterFactory;

    /// <inheritdoc />
    public SpawnAlphaTestMonsterScript(Dialog subject, IMonsterFactory monsterFactory)
        : base(subject) => MonsterFactory = monsterFactory;

    /// <inheritdoc />
    public override void OnDisplaying(Aisling source)
    {
        var nowSeconds = Convert.ToInt32(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        var readyAtSeconds = source.Trackers.Counters.TryGetValue(CooldownCounterKey, out var ready)
            ? ready
            : 0;

        if (nowSeconds < readyAtSeconds)
        {
            var remainingSeconds = readyAtSeconds - nowSeconds;
            source.SendOrangeBarMessage($"You must wait {remainingSeconds} more second(s) before spawning another test target.");

            return;
        }

        if (!source.MapInstance.TryGetRandomWalkablePoint(pt => SpawnArea.ContainsPoint(pt), out var point))
        {
            source.SendOrangeBarMessage("No valid spawn point found - try again.");

            return;
        }

        var monster = MonsterFactory.Create(MonsterTemplateKey, source.MapInstance, point.Value);
        source.MapInstance.AddEntity(monster, point.Value);
        monster.Script.OnSpawn();

        source.Trackers.Counters.Set(CooldownCounterKey, nowSeconds + CooldownSeconds);
        source.SendOrangeBarMessage($"A {DisplayName} test target has been spawned.");
    }

    #region ScriptVars
    /// <summary>
    ///     The number of seconds before this player can trigger another test-monster spawn (any difficulty - shared
    ///     cooldown, not per-difficulty)
    /// </summary>
    public int CooldownSeconds { get; init; } = 90;

    /// <summary>
    ///     The display name used in the confirmation message (e.g. "Easy", "Mid", "Hard")
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    ///     The templateKey of the monster to spawn
    /// </summary>
    public string MonsterTemplateKey { get; init; } = string.Empty;
    #endregion
}
