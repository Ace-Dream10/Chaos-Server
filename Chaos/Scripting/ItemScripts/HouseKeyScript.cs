#region
using Chaos.Collections;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.ItemScripts.Abstractions;
using Chaos.Services.Other.Abstractions;
using Chaos.Services.Storage.Options;
using Chaos.Storage.Abstractions;
using Chaos.Utilities;
using Microsoft.Extensions.Options;
#endregion

namespace Chaos.Scripting.ItemScripts;

/// <summary>
///     A reusable house key: teleports the user to their own house (or a house they're a guest of) on use.
///     Modeled on <see cref="UseToWarpScript" />, with two deliberate differences - the destination is resolved
///     dynamically per-user instead of a fixed <c>ScriptVars</c> location, and the item is <b>not</b> consumed on
///     use (a key should be reusable, unlike the one-shot warp scrolls <see cref="UseToWarpScript" /> backs). Not
///     configurable (no <c>ScriptVars</c>), so this extends the plain <see cref="ItemScriptBase" /> rather than
///     <see cref="ConfigurableItemScriptBase" />.
/// </summary>
/// <remarks>
///     No cooldown - confirmed spammable, same as the existing warp items this is modeled on.
///     <br />
///     Guest resolution relies on <c>source.Trackers.Tags["houseGuestOf"]</c> pointing at the owner's name. Nothing
///     currently sets this tag - the guest-invite flow itself isn't built yet - so today this only ever resolves an
///     owner's own house. This is forward-compatible plumbing for that future flow, not a complete feature; door
///     access for guests (<see cref="Chaos.Scripting.ReactorTileScripts.HouseDoorScript" />) doesn't have this
///     limitation, since a door already knows whose house it is without needing to resolve anything.
/// </remarks>
public class HouseKeyScript : ItemScriptBase
{
    private readonly IStore<HouseOwnership> HouseStore;
    private readonly IMapTraversalService MapTraversalService;
    private readonly HouseOwnershipStoreOptions Options;
    private readonly ISimpleCache SimpleCache;

    /// <inheritdoc />
    public HouseKeyScript(
        Item subject,
        IStore<HouseOwnership> houseStore,
        IMapTraversalService mapTraversalService,
        IOptions<HouseOwnershipStoreOptions> options,
        ISimpleCache simpleCache)
        : base(subject)
    {
        HouseStore = houseStore;
        MapTraversalService = mapTraversalService;
        Options = options.Value;
        SimpleCache = simpleCache;
    }

    /// <inheritdoc />
    public override void OnUse(Aisling source)
    {
        if (!TryResolveOwnership(source, out var ownership))
        {
            source.SendOrangeBarMessage("You don't have access to any house.");

            return;
        }

        var rentInterval = TimeSpan.FromDays(Options.RentIntervalDays);
        var result = HouseAccessHelper.TryAccess(source, ownership, HouseStore, rentInterval, out var entryLocation);

        switch (result)
        {
            case HouseAccessHelper.HouseAccessResult.Granted:
            {
                var targetMap = SimpleCache.Get<MapInstance>(entryLocation!.Map);
                MapTraversalService.TraverseMap(source, targetMap, entryLocation);

                break;
            }
            case HouseAccessHelper.HouseAccessResult.NotAuthorized:
            {
                //shouldn't normally happen - TryResolveOwnership only returns records source is actually
                //owner/guest of - but the record could theoretically change between resolution and this check
                source.SendOrangeBarMessage("You don't have access to any house.");

                break;
            }
            case HouseAccessHelper.HouseAccessResult.RentOverdue:
            {
                source.SendOrangeBarMessage(
                    ownership.Owner.Equals(source.Name, StringComparison.OrdinalIgnoreCase)
                        ? "The rent wasn't paid in time - this house has been repossessed."
                        : "This house's rent is overdue and access is suspended.");

                break;
            }
            case HouseAccessHelper.HouseAccessResult.NoHouseAssigned:
            {
                source.SendOrangeBarMessage("This house hasn't been assigned yet.");

                break;
            }
        }
    }

    /// <summary>
    ///     Resolves which house record applies to <paramref name="source" /> - their own, if they own one,
    ///     otherwise whichever house they're tagged as a guest of (see remarks on the guest tag's current status).
    /// </summary>
    private bool TryResolveOwnership(Aisling source, out HouseOwnership ownership)
    {
        ownership = null!;

        if (HouseStore.Exists(source.Name))
        {
            ownership = HouseStore.Load(source.Name);

            return true;
        }

        if (source.Trackers.Tags.TryGetValue("houseGuestOf", out var guestOfOwner) && HouseStore.Exists(guestOfOwner))
        {
            var candidate = HouseStore.Load(guestOfOwner);

            if (candidate.Guests.Contains(source.Name, StringComparer.OrdinalIgnoreCase))
            {
                ownership = candidate;

                return true;
            }
        }

        return false;
    }
}
