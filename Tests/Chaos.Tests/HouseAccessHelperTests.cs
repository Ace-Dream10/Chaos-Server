#region
using Chaos.Collections;
using Chaos.Geometry;
using Chaos.Storage.Abstractions;
using Chaos.Testing.Infrastructure.Mocks;
using Chaos.Utilities;
using FluentAssertions;
using Moq;
#endregion

namespace Chaos.Tests;

public sealed class HouseAccessHelperTests
{
    private static readonly TimeSpan RentInterval = TimeSpan.FromDays(7);

    private static HouseOwnership CreateOwnership(
        string owner,
        List<string>? guests = null,
        Location? entryLocation = null,
        DateTime? lastPaidUtc = null)
        => new()
        {
            Owner = owner,
            Guests = guests ?? [],
            EntryLocation = entryLocation ?? new Location("houseMap", 5, 5),
            LastPaidUtc = lastPaidUtc ?? DateTime.UtcNow
        };

    [Test]
    public void TryAccess_ShouldGrantAccess_ForOwner_WhenRentIsPaid()
    {
        var map = MockMapInstance.Create();
        var owner = MockAisling.Create(map, "Owner");
        var ownership = CreateOwnership("Owner");
        var storeMock = new Mock<IStore<HouseOwnership>>();

        var result = HouseAccessHelper.TryAccess(owner, ownership, storeMock.Object, RentInterval, out var entryLocation);

        result.Should()
              .Be(HouseAccessHelper.HouseAccessResult.Granted);

        entryLocation.Should()
                     .Be(ownership.EntryLocation);

        storeMock.Verify(s => s.Remove(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void TryAccess_ShouldGrantAccess_ForGuest_WhenRentIsPaid()
    {
        var map = MockMapInstance.Create();
        var guest = MockAisling.Create(map, "Guest");
        var ownership = CreateOwnership("Owner", guests: ["Guest"]);
        var storeMock = new Mock<IStore<HouseOwnership>>();

        var result = HouseAccessHelper.TryAccess(guest, ownership, storeMock.Object, RentInterval, out var entryLocation);

        result.Should()
              .Be(HouseAccessHelper.HouseAccessResult.Granted);

        entryLocation.Should()
                     .Be(ownership.EntryLocation);
    }

    [Test]
    public void TryAccess_ShouldDeny_ForNonMember()
    {
        var map = MockMapInstance.Create();
        var stranger = MockAisling.Create(map, "Stranger");
        var ownership = CreateOwnership("Owner", guests: ["Guest"]);
        var storeMock = new Mock<IStore<HouseOwnership>>();

        var result = HouseAccessHelper.TryAccess(stranger, ownership, storeMock.Object, RentInterval, out var entryLocation);

        result.Should()
              .Be(HouseAccessHelper.HouseAccessResult.NotAuthorized);

        entryLocation.Should()
                     .BeNull();

        storeMock.Verify(s => s.Remove(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void TryAccess_ShouldReturnNoHouseAssigned_WhenEntryLocationIsNull_ButRentIsPaid()
    {
        var map = MockMapInstance.Create();
        var owner = MockAisling.Create(map, "Owner");

        //constructed directly rather than via CreateOwnership - that helper's own default-fallback (`??`) would
        //silently replace an explicit null with a real location, defeating the point of this test
        var ownership = new HouseOwnership
        {
            Owner = "Owner",
            EntryLocation = null,
            LastPaidUtc = DateTime.UtcNow
        };

        var storeMock = new Mock<IStore<HouseOwnership>>();

        var result = HouseAccessHelper.TryAccess(owner, ownership, storeMock.Object, RentInterval, out var entryLocation);

        result.Should()
              .Be(HouseAccessHelper.HouseAccessResult.NoHouseAssigned);

        entryLocation.Should()
                     .BeNull();
    }

    [Test]
    public void TryAccess_ShouldRepossess_WhenOwnerAttemptsEntry_AndRentIsOverdue()
    {
        var map = MockMapInstance.Create();
        var owner = MockAisling.Create(map, "Owner");
        var storedItem = MockItem.Create("StoredSword");
        owner.HouseStorage.Deposit(storedItem);
        owner.HouseStorage.AddGold(500);

        var ownership = CreateOwnership("Owner", lastPaidUtc: DateTime.UtcNow - RentInterval - TimeSpan.FromDays(1));
        var storeMock = new Mock<IStore<HouseOwnership>>();

        var result = HouseAccessHelper.TryAccess(owner, ownership, storeMock.Object, RentInterval, out var entryLocation);

        result.Should()
              .Be(HouseAccessHelper.HouseAccessResult.RentOverdue);

        entryLocation.Should()
                     .BeNull();

        //items and gold moved out of house storage into the owner's own bank
        owner.HouseStorage
             .Should()
             .BeEmpty();

        owner.HouseStorage.Gold
             .Should()
             .Be(0);

        owner.Bank.Contains("StoredSword")
             .Should()
             .BeTrue();

        owner.Bank.Gold
             .Should()
             .Be(500);

        //ownership record removed entirely, freeing the house for a future owner
        storeMock.Verify(s => s.Remove("Owner"), Times.Once);
    }

    [Test]
    public void TryAccess_ShouldDenyWithoutRepossessing_WhenGuestAttemptsEntry_AndRentIsOverdue()
    {
        var map = MockMapInstance.Create();
        var guest = MockAisling.Create(map, "Guest");
        var ownership = CreateOwnership(
            "Owner",
            guests: ["Guest"],
            lastPaidUtc: DateTime.UtcNow - RentInterval - TimeSpan.FromDays(1));
        var storeMock = new Mock<IStore<HouseOwnership>>();

        var result = HouseAccessHelper.TryAccess(guest, ownership, storeMock.Object, RentInterval, out var entryLocation);

        result.Should()
              .Be(HouseAccessHelper.HouseAccessResult.RentOverdue);

        entryLocation.Should()
                     .BeNull();

        //a guest finding the house overdue must not be able to trigger repossession of someone else's house
        storeMock.Verify(s => s.Remove(It.IsAny<string>()), Times.Never);

        guest.Bank.Should()
             .BeEmpty();
    }
}
