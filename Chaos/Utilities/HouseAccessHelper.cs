#region
using Chaos.Collections;
using Chaos.Extensions.Common;
using Chaos.Geometry;
using Chaos.Models.World;
using Chaos.Storage.Abstractions;
#endregion

namespace Chaos.Utilities;

/// <summary>
///     Shared access-check logic for house doors/keys - both the door reactor tile and the teleport key item need
///     identical owner-or-guest membership and rent handling, so it lives here once instead of twice.
/// </summary>
/// <remarks>
///     Rent is checked on-demand, right here, rather than via any background sweep - see the remarks on
///     <see cref="HouseOwnership" />. Repossession only ever fires while the person whose house it is is the one
///     currently interacting - see <see cref="Repossess" />'s remarks for why that's what makes it safe.
/// </remarks>
public static class HouseAccessHelper
{
    public enum HouseAccessResult
    {
        /// <summary>
        ///     Access granted - the <c>entryLocation</c> out param is populated.
        /// </summary>
        Granted,

        /// <summary>
        ///     The requesting player is neither the owner nor a guest of this house.
        /// </summary>
        NotAuthorized,

        /// <summary>
        ///     Rent is overdue. If the requester is the owner, repossession has already happened as a side effect
        ///     of this call (see <see cref="Repossess" />) - the ownership record no longer exists. If the
        ///     requester is a guest, nothing was changed; the house stays locked until the owner returns.
        /// </summary>
        RentOverdue,

        /// <summary>
        ///     Rent is current and the requester is authorized, but no house instance has been assigned to this
        ///     ownership record yet (the purchase flow that assigns one isn't implemented yet).
        /// </summary>
        NoHouseAssigned
    }

    /// <summary>
    ///     Checks whether <paramref name="source" /> may enter the house described by <paramref name="ownership" />,
    ///     handling on-demand rent enforcement (and repossession, if applicable) as part of the same check.
    /// </summary>
    public static HouseAccessResult TryAccess(
        Aisling source,
        HouseOwnership ownership,
        IStore<HouseOwnership> houseStore,
        TimeSpan rentInterval,
        out Location? entryLocation)
    {
        entryLocation = null;

        var isOwner = ownership.Owner.EqualsI(source.Name);
        var isGuest = !isOwner && ownership.Guests.Contains(source.Name, StringComparer.OrdinalIgnoreCase);

        if (!isOwner && !isGuest)
            return HouseAccessResult.NotAuthorized;

        if (DateTime.UtcNow > ownership.LastPaidUtc + rentInterval)
        {
            //only the owner attempting entry can trigger repossession - a guest finding the house locked doesn't
            //get to dump someone else's stored items into their own bank, so nothing happens to the record here
            if (isOwner)
                Repossess(source, ownership, houseStore);

            return HouseAccessResult.RentOverdue;
        }

        if (ownership.EntryLocation is null)
            return HouseAccessResult.NoHouseAssigned;

        entryLocation = ownership.EntryLocation;

        return HouseAccessResult.Granted;
    }

    /// <summary>
    ///     Transfers everything in <paramref name="owner" />'s <see cref="Aisling.HouseStorage" /> into their own
    ///     <see cref="Aisling.Bank" />, then removes the ownership record entirely (freeing the house/instance for a
    ///     future owner).
    /// </summary>
    /// <remarks>
    ///     Safe to mutate <paramref name="owner" />'s live objects directly - this only ever runs while
    ///     <paramref name="owner" /> is the one actively interacting (attempting to enter their own house), so there
    ///     is no background/offline path that could call this concurrently and race their own session's periodic
    ///     save. This is the entire reason the on-demand model was chosen over a background sweep - a sweep would
    ///     have to solve safely mutating a possibly-offline player's save data, which this deliberately never needs
    ///     to.
    /// </remarks>
    private static void Repossess(Aisling owner, HouseOwnership ownership, IStore<HouseOwnership> houseStore)
    {
        foreach (var item in owner.HouseStorage.ToArray())
            if (owner.HouseStorage.TryRemove(item.DisplayName, item.Count, out var removedItems))
                foreach (var removedItem in removedItems)
                    owner.Bank.Deposit(removedItem);

        var goldToTransfer = owner.HouseStorage.Gold;

        if ((goldToTransfer > 0) && owner.HouseStorage.RemoveGold(goldToTransfer))
            owner.Bank.AddGold(goldToTransfer);

        houseStore.Remove(ownership.Owner);
    }
}
