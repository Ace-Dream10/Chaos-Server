#region
using Chaos.Collections;
using Chaos.Common.Abstractions;
using Chaos.Geometry;
using Chaos.Geometry.Abstractions;
using Chaos.Models.World;
using Chaos.Scripting.ReactorTileScripts;
using Chaos.Services.Other.Abstractions;
using Chaos.Services.Storage.Options;
using Chaos.Storage.Abstractions;
using Chaos.Testing.Infrastructure.Mocks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
#endregion

namespace Chaos.Tests;

public sealed class HouseDoorScriptTests
{
    private static HouseDoorScript CreateScript(
        string ownerName,
        MapInstance map,
        out Mock<IStore<HouseOwnership>> houseStoreMock,
        out Mock<ISimpleCache> simpleCacheMock,
        int rentIntervalDays = 7)
    {
        houseStoreMock = new Mock<IStore<HouseOwnership>>();
        simpleCacheMock = new Mock<ISimpleCache>();
        var options = Microsoft.Extensions.Options.Options.Create(new HouseOwnershipStoreOptions { RentIntervalDays = rentIntervalDays });

        var vars = new TestScriptVars();
        vars.Set("OwnerName", ownerName);

        var reactorTile = new ReactorTile(
            map,
            new Point(3, 3),
            false,
            MockScriptProvider.Instance.Object,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "HouseDoor" },
            new Dictionary<string, IScriptVars>(StringComparer.OrdinalIgnoreCase) { ["HouseDoor"] = vars });

        return new HouseDoorScript(reactorTile, houseStoreMock.Object, options, simpleCacheMock.Object);
    }

    [Test]
    public void OnWalkedOn_ShouldTraverse_WhenOwnerAndRentPaid()
    {
        var map = MockMapInstance.Create();
        var owner = MockAisling.Create(map, "Owner");
        var targetMap = MockMapInstance.Create();
        var entryLocation = new Location("houseMap", 5, 5);

        var script = CreateScript("Owner", map, out var houseStoreMock, out var simpleCacheMock);

        houseStoreMock.Setup(s => s.Exists("Owner"))
                      .Returns(true);

        houseStoreMock.Setup(s => s.Load("Owner"))
                      .Returns(
                          new HouseOwnership
                          {
                              Owner = "Owner",
                              EntryLocation = entryLocation,
                              LastPaidUtc = DateTime.UtcNow
                          });

        simpleCacheMock.Setup(c => c.Get<MapInstance>("houseMap"))
                       .Returns(targetMap);

        var act = () => script.OnWalkedOn(owner);

        act.Should()
           .NotThrow();

        //TraverseMap is fire-and-forget (queues onto a channel processed by a background service), so the actual
        //map move never completes synchronously in a test - verify the call was made with the right destination
        //instead of asserting on owner.MapInstance
        Mock.Get(map.TraversalService)
            .Verify(
                s => s.TraverseMap(owner, targetMap, It.Is<IPoint>(p => (p.X == 5) && (p.Y == 5)), false, false, null),
                Times.Once);
    }

    [Test]
    public void OnWalkedOn_ShouldNotThrow_WhenSourceIsNotAnAisling()
    {
        var map = MockMapInstance.Create();
        var monster = MockMonster.Create(map);

        var script = CreateScript("Owner", map, out _, out _);

        var act = () => script.OnWalkedOn(monster);

        act.Should()
           .NotThrow();
    }

    [Test]
    public void OnWalkedOn_ShouldBounceBack_WhenHouseIsUnclaimed()
    {
        var map = MockMapInstance.Create();
        var stranger = MockAisling.Create(map, "Stranger", position: new Point(3, 4));

        var script = CreateScript("Owner", map, out var houseStoreMock, out _);

        houseStoreMock.Setup(s => s.Exists("Owner"))
                      .Returns(false);

        var act = () => script.OnWalkedOn(stranger);

        act.Should()
           .NotThrow();

        //never left the original map since access was denied
        stranger.MapInstance
                .Should()
                .BeSameAs(map);
    }

    [Test]
    public void OnWalkedOn_ShouldRepossess_WhenOwnerAndRentOverdue()
    {
        var map = MockMapInstance.Create();
        var owner = MockAisling.Create(map, "Owner", position: new Point(3, 4));

        var script = CreateScript("Owner", map, out var houseStoreMock, out _);

        houseStoreMock.Setup(s => s.Exists("Owner"))
                      .Returns(true);

        houseStoreMock.Setup(s => s.Load("Owner"))
                      .Returns(
                          new HouseOwnership
                          {
                              Owner = "Owner",
                              EntryLocation = new Location("houseMap", 5, 5),
                              LastPaidUtc = DateTime.UtcNow - TimeSpan.FromDays(30)
                          });

        script.OnWalkedOn(owner);

        houseStoreMock.Verify(s => s.Remove("Owner"), Times.Once);

        owner.MapInstance
             .Should()
             .BeSameAs(map);
    }

    private sealed class TestScriptVars : IScriptVars
    {
        private readonly Dictionary<string, object> ByKey = new();
        public bool ContainsKey(string key) => ByKey.ContainsKey(key);
        public object? Get(Type type, string name) => ByKey.TryGetValue(name, out var v) ? v : null;
        public T? Get<T>(string name) => ByKey.TryGetValue(name, out var v) ? (T)v : default;
        public T GetRequired<T>(string key) => ByKey.TryGetValue(key, out var v) ? (T)v : throw new KeyNotFoundException(key);
        public void Set<T>(string name, T value) => ByKey[name] = value!;
    }
}
