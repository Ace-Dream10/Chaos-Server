#region
using Chaos.Collections;
using Chaos.Geometry;
using Chaos.Geometry.Abstractions;
using Chaos.Models.World;
using Chaos.Scripting.ItemScripts;
using Chaos.Services.Other.Abstractions;
using Chaos.Services.Storage.Options;
using Chaos.Storage.Abstractions;
using Chaos.Testing.Infrastructure.Mocks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
#endregion

namespace Chaos.Tests;

public sealed class HouseKeyScriptTests
{
    private static HouseKeyScript CreateScript(
        out Mock<IStore<HouseOwnership>> houseStoreMock,
        out Mock<IMapTraversalService> mapTraversalServiceMock,
        out Mock<ISimpleCache> simpleCacheMock,
        int rentIntervalDays = 7)
    {
        var subject = MockItem.Create("HouseKey");
        houseStoreMock = new Mock<IStore<HouseOwnership>>();
        mapTraversalServiceMock = new Mock<IMapTraversalService>();
        simpleCacheMock = new Mock<ISimpleCache>();
        var options = Microsoft.Extensions.Options.Options.Create(new HouseOwnershipStoreOptions { RentIntervalDays = rentIntervalDays });

        return new HouseKeyScript(subject, houseStoreMock.Object, mapTraversalServiceMock.Object, options, simpleCacheMock.Object);
    }

    [Test]
    public void OnUse_ShouldTraverseToOwnHouse_WhenOwnerAndRentPaid()
    {
        var script = CreateScript(out var houseStoreMock, out var mapTraversalServiceMock, out var simpleCacheMock);
        var map = MockMapInstance.Create();
        var owner = MockAisling.Create(map, "Owner");
        var targetMap = MockMapInstance.Create();
        var entryLocation = new Location("houseMap", 5, 5);

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

        script.OnUse(owner);

        mapTraversalServiceMock.Verify(
            s => s.TraverseMap(owner, targetMap, It.Is<IPoint>(p => (p.X == 5) && (p.Y == 5)), false, false, null),
            Times.Once);
    }

    [Test]
    public void OnUse_ShouldNotTraverse_WhenSourceHasNoHouseAccess()
    {
        var script = CreateScript(out var houseStoreMock, out var mapTraversalServiceMock, out _);
        var map = MockMapInstance.Create();
        var source = MockAisling.Create(map, "Nobody");

        houseStoreMock.Setup(s => s.Exists("Nobody"))
                      .Returns(false);

        var act = () => script.OnUse(source);

        act.Should()
           .NotThrow();

        mapTraversalServiceMock.Verify(
            s => s.TraverseMap(It.IsAny<Aisling>(), It.IsAny<MapInstance>(), It.IsAny<IPoint>(), It.IsAny<bool>(), It.IsAny<bool>(), null),
            Times.Never);
    }

    [Test]
    public void OnUse_ShouldRepossessAndNotTraverse_WhenOwnerAndRentOverdue()
    {
        var script = CreateScript(out var houseStoreMock, out var mapTraversalServiceMock, out _);
        var map = MockMapInstance.Create();
        var owner = MockAisling.Create(map, "Owner");

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

        script.OnUse(owner);

        houseStoreMock.Verify(s => s.Remove("Owner"), Times.Once);

        mapTraversalServiceMock.Verify(
            s => s.TraverseMap(It.IsAny<Aisling>(), It.IsAny<MapInstance>(), It.IsAny<IPoint>(), It.IsAny<bool>(), It.IsAny<bool>(), null),
            Times.Never);
    }

    [Test]
    public void OnUse_ShouldTraverseToOwnersHouse_WhenSourceIsTaggedGuest()
    {
        var script = CreateScript(out var houseStoreMock, out var mapTraversalServiceMock, out var simpleCacheMock);
        var map = MockMapInstance.Create();
        var guest = MockAisling.Create(map, "Guest");
        guest.Trackers.Tags["houseGuestOf"] = "Owner";

        var targetMap = MockMapInstance.Create();
        var entryLocation = new Location("houseMap", 5, 5);

        houseStoreMock.Setup(s => s.Exists("Guest"))
                      .Returns(false);

        houseStoreMock.Setup(s => s.Exists("Owner"))
                      .Returns(true);

        houseStoreMock.Setup(s => s.Load("Owner"))
                      .Returns(
                          new HouseOwnership
                          {
                              Owner = "Owner",
                              Guests = ["Guest"],
                              EntryLocation = entryLocation,
                              LastPaidUtc = DateTime.UtcNow
                          });

        simpleCacheMock.Setup(c => c.Get<MapInstance>("houseMap"))
                       .Returns(targetMap);

        script.OnUse(guest);

        mapTraversalServiceMock.Verify(
            s => s.TraverseMap(guest, targetMap, It.Is<IPoint>(p => (p.X == 5) && (p.Y == 5)), false, false, null),
            Times.Once);
    }
}
