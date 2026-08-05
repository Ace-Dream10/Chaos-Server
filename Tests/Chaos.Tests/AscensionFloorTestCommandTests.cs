#region
using Chaos.Collections;
using Chaos.Collections.Common;
using Chaos.Messaging.Admin;
using Chaos.Networking.Abstractions;
using Chaos.Networking.Entities.Server;
using Chaos.Storage.Abstractions;
using Chaos.Testing.Infrastructure.Mocks;
using FluentAssertions;
using Moq;
#endregion

namespace Chaos.Tests;

public sealed class AscensionFloorTestCommandTests
{
    [Test]
    public async Task ExecuteAsync_ShouldSetCurrentFloorCounter_AndSendPacket()
    {
        var floorStoreMock = new Mock<IStore<AscensionFloorState>>();
        floorStoreMock.Setup(s => s.Exists("5"))
                      .Returns(false);

        var command = new AscensionFloorTestCommand(floorStoreMock.Object);
        var aisling = MockAisling.Create();
        var args = new ArgumentCollection("5");

        await command.ExecuteAsync(aisling, args);

        aisling.Trackers.Counters.TryGetValue("currentFloor", out var currentFloor)
               .Should()
               .BeTrue();

        currentFloor.Should()
                    .Be(5);

        Mock.Get(aisling.Client)
            .Verify(
                c => c.SendAscensionFloorUpdate(It.Is<AscensionFloorUpdateArgs>(a => a.CurrentFloor == 5)),
                Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_ShouldSendExistingFloorState_WhenFloorAlreadyCleared()
    {
        var existingState = new AscensionFloorState
        {
            FloorNumber = 2,
            BossAlive = false,
            BossName = "Floor Two Boss",
            FirstClearers = ["Ace"]
        };

        var floorStoreMock = new Mock<IStore<AscensionFloorState>>();
        floorStoreMock.Setup(s => s.Exists("2"))
                      .Returns(true);

        floorStoreMock.Setup(s => s.Load("2"))
                      .Returns(existingState);

        var command = new AscensionFloorTestCommand(floorStoreMock.Object);
        var aisling = MockAisling.Create();
        var args = new ArgumentCollection("2");

        await command.ExecuteAsync(aisling, args);

        Mock.Get(aisling.Client)
            .Verify(
                c => c.SendAscensionFloorUpdate(
                    It.Is<AscensionFloorUpdateArgs>(
                        a => (a.CurrentFloor == 2) && !a.BossAlive && (a.BossName == "Floor Two Boss") && a.FirstClearers.Contains("Ace"))),
                Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_ShouldRejectOutOfRangeFloor()
    {
        var floorStoreMock = new Mock<IStore<AscensionFloorState>>();
        var command = new AscensionFloorTestCommand(floorStoreMock.Object);
        var aisling = MockAisling.Create();
        var args = new ArgumentCollection("11");

        await command.ExecuteAsync(aisling, args);

        aisling.Trackers.Counters.TryGetValue("currentFloor", out _)
               .Should()
               .BeFalse();

        Mock.Get(aisling.Client)
            .Verify(c => c.SendAscensionFloorUpdate(It.IsAny<AscensionFloorUpdateArgs>()), Times.Never);
    }
}
