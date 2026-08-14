#region
using Chaos.Collections;
using Chaos.Models.World;
using Chaos.Networking.Abstractions;
using Chaos.Networking.Entities.Server;
using Chaos.Scripting.FunctionalScripts;
using Chaos.Scripting.FunctionalScripts.LevelUp;
using Chaos.Scripting.MonsterScripts;
using Chaos.Storage.Abstractions;
using Chaos.Testing.Infrastructure.Harnesses;
using Chaos.Testing.Infrastructure.Mocks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
#endregion

namespace Chaos.Tests;

/// <summary>
///     AscensionBossDeathScript's constructor calls DefaultLevelUpScript.Create(), which resolves through the
///     shared static FunctionalScriptRegistry - this test class previously had no static constructor registering
///     it, silently relying on some unrelated test class happening to register "DefaultLevelUp" first in whatever
///     order the whole suite ran in. Confirmed via git-stash isolation: passed alone at a clean checkout, failed
///     alone with a `KeyNotFoundException: Script with key 'DefaultLevelUp' not found` after simply adding an
///     unrelated new test file elsewhere in the assembly (shifting test discovery/scheduling enough to expose the
///     gap). Registering it here directly makes this file self-sufficient, matching every other test class's
///     convention of registering exactly what it needs.
/// </summary>
public sealed class AscensionBossDeathScriptTests
{
    static AscensionBossDeathScriptTests()
    {
        var registry = new FunctionalScriptRegistry(MockServiceProvider.CreateBuilder()
                                                                        .Build()
                                                                        .Object);

        registry.Register(DefaultLevelUpScript.Key, typeof(DefaultLevelUpScript));
    }

    private static (MonsterScriptHarness<AscensionBossDeathScript> Harness, Mock<IStore<AscensionFloorState>> FloorStore, Mock<
        IClientRegistry<IChaosWorldClient>> ClientRegistry) CreateHarness(int floorNumber = 1, List<IChaosWorldClient>? onlineClients = null)
    {
        var floorStoreMock = new Mock<IStore<AscensionFloorState>>();
        floorStoreMock.Setup(s => s.Exists(It.IsAny<string>()))
                      .Returns(false);

        var clients = onlineClients ?? [];
        var clientRegistryMock = new Mock<IClientRegistry<IChaosWorldClient>>();
        clientRegistryMock.Setup(r => r.GetEnumerator())
                          .Returns(() => clients.GetEnumerator());

        var loggerMock = new Mock<ILogger<AscensionBossDeathScript>>();

        var scriptVars = new MockScriptVars();
        scriptVars.Set(floorNumber, nameof(AscensionBossDeathScript.FloorNumber));

        var harness = new MonsterScriptHarness<AscensionBossDeathScript>(
            scriptFactory: monster => new AscensionBossDeathScript(
                monster,
                floorStoreMock.Object,
                clientRegistryMock.Object,
                loggerMock.Object),
            monsterSetup: monster => monster.Template.ScriptVars["AscensionBossDeath"] = scriptVars);

        return (harness, floorStoreMock, clientRegistryMock);
    }

    private static Mock<IChaosWorldClient> CreateOnlineClient(Aisling aisling)
    {
        var clientMock = new Mock<IChaosWorldClient>();
        clientMock.SetupGet(c => c.Aisling)
                  .Returns(aisling);

        return clientMock;
    }

    [Test]
    public void Constructor_ShouldTagSubjectWithFloorNumber()
    {
        var (harness, _, _) = CreateHarness(floorNumber: 3);

        harness.Subject.Trackers.Tags.Should()
               .ContainKey("ascensionFloor")
               .WhoseValue.Should()
               .Be("3");
    }

    [Test]
    public void OnAttacked_ShouldAddAislingSourceToParticipants()
    {
        var (harness, _, _) = CreateHarness();
        var attacker = MockAisling.Create(name: "Ace");

        harness.Attack(attacker, 10);

        harness.Subject.AscensionParticipants.Should()
               .ContainSingle()
               .Which.Should()
               .Be("Ace");
    }

    [Test]
    public void OnAttacked_ShouldNotAddNonAislingSource()
    {
        var (harness, _, _) = CreateHarness();
        var petMonster = MockMonster.Create(name: "SomePet");

        harness.Attack(petMonster, 10);

        harness.Subject.AscensionParticipants.Should()
               .BeEmpty();
    }

    [Test]
    public void OnDeath_ShouldPersistFirstClearers_WhenFloorNotPreviouslyCleared()
    {
        var (harness, floorStoreMock, _) = CreateHarness(floorNumber: 1);
        var attacker1 = MockAisling.Create(name: "Ace");
        var attacker2 = MockAisling.Create(name: "Dream");

        harness.Attack(attacker1, 10);
        harness.Attack(attacker2, 10);

        AscensionFloorState? saved = null;
        floorStoreMock.Setup(s => s.Save(It.IsAny<AscensionFloorState>()))
                      .Callback<AscensionFloorState>(state => saved = state);

        harness.Script.OnDeath();

        saved.Should()
             .NotBeNull();

        saved!.BossAlive.Should()
              .BeFalse();

        saved.FirstClearers.Should()
             .BeEquivalentTo(["Ace", "Dream"]);

        saved.FirstClearedAt.Should()
             .NotBeNull();
    }

    [Test]
    public void OnDeath_ShouldBumpHighestFloorCleared_ForOnlineParticipants()
    {
        var attacker = MockAisling.Create(name: "Ace");
        var onlineClient = CreateOnlineClient(attacker);

        var (harness, _, _) = CreateHarness(floorNumber: 4, onlineClients: [onlineClient.Object]);

        harness.Attack(attacker, 10);
        harness.Script.OnDeath();

        attacker.Trackers.Counters.TryGetValue("highestFloorCleared", out var highest)
                .Should()
                .BeTrue();

        highest.Should()
               .Be(4);
    }

    [Test]
    public void OnDeath_ShouldNotOverwriteFirstClearers_WhenFloorAlreadyCleared()
    {
        var floorStoreMock = new Mock<IStore<AscensionFloorState>>();
        var existingState = new AscensionFloorState
        {
            FloorNumber = 1,
            BossAlive = true,
            FirstClearers = ["EarlierClearer"],
            FirstClearedAt = DateTime.UtcNow.AddDays(-1)
        };

        floorStoreMock.Setup(s => s.Exists("1"))
                      .Returns(true);

        floorStoreMock.Setup(s => s.Load("1"))
                      .Returns(existingState);

        var clientRegistryMock = new Mock<IClientRegistry<IChaosWorldClient>>();
        clientRegistryMock.Setup(r => r.GetEnumerator())
                          .Returns(() => new List<IChaosWorldClient>().GetEnumerator());

        var loggerMock = new Mock<ILogger<AscensionBossDeathScript>>();
        var scriptVars = new MockScriptVars();
        scriptVars.Set(1, nameof(AscensionBossDeathScript.FloorNumber));

        var harness = new MonsterScriptHarness<AscensionBossDeathScript>(
            scriptFactory: monster => new AscensionBossDeathScript(
                monster,
                floorStoreMock.Object,
                clientRegistryMock.Object,
                loggerMock.Object),
            monsterSetup: monster => monster.Template.ScriptVars["AscensionBossDeath"] = scriptVars);

        var laterAttacker = MockAisling.Create(name: "LaterKiller");
        harness.Attack(laterAttacker, 10);

        AscensionFloorState? saved = null;
        floorStoreMock.Setup(s => s.Save(It.IsAny<AscensionFloorState>()))
                      .Callback<AscensionFloorState>(state => saved = state);

        harness.Script.OnDeath();

        saved!.FirstClearers.Should()
              .BeEquivalentTo(["EarlierClearer"]);
    }

    [Test]
    public void OnDeath_ShouldBroadcastToRecipient_WhenOnMatchingFloor()
    {
        var attacker = MockAisling.Create(name: "Ace");
        attacker.Trackers.Counters.Set("currentFloor", 4);
        var onlineClient = CreateOnlineClient(attacker);

        var (harness, _, _) = CreateHarness(floorNumber: 4, onlineClients: [onlineClient.Object]);

        harness.Attack(attacker, 10);
        harness.Script.OnDeath();

        onlineClient.Verify(
            c => c.SendAscensionFloorUpdate(
                It.Is<AscensionFloorUpdateArgs>(
                    args => (args.CurrentFloor == 4)
                            && !args.BossAlive
                            && args.FirstClearers.Contains("Ace")
                            && (args.HighestFloorCleared == 4))),
            Times.Once);
    }

    [Test]
    public void OnDeath_ShouldNotBroadcastToRecipient_WhenOnDifferentFloor()
    {
        var attacker = MockAisling.Create(name: "Ace");

        var bystander = MockAisling.Create(name: "Bystander");
        bystander.Trackers.Counters.Set("currentFloor", 7);
        var onlineClient = CreateOnlineClient(bystander);

        var (harness, _, _) = CreateHarness(floorNumber: 4, onlineClients: [onlineClient.Object]);

        harness.Attack(attacker, 10);
        harness.Script.OnDeath();

        onlineClient.Verify(c => c.SendAscensionFloorUpdate(It.IsAny<AscensionFloorUpdateArgs>()), Times.Never);
    }

    [Test]
    public void OnDeath_ShouldNotBroadcastToRecipient_WhenNoCurrentFloorSet()
    {
        var attacker = MockAisling.Create(name: "Ace");

        //recipient never entered a floor at all (no "currentFloor" counter set)
        var bystander = MockAisling.Create(name: "NeverEnteredChamber");
        var onlineClient = CreateOnlineClient(bystander);

        var (harness, _, _) = CreateHarness(floorNumber: 4, onlineClients: [onlineClient.Object]);

        harness.Attack(attacker, 10);
        harness.Script.OnDeath();

        onlineClient.Verify(c => c.SendAscensionFloorUpdate(It.IsAny<AscensionFloorUpdateArgs>()), Times.Never);
    }
}
