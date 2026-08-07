#region
using Chaos.Collections;
using Chaos.Extensions.Common;
using Chaos.Extensions.Geometry;
using Chaos.Geometry.Abstractions;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.ReactorTileScripts.Abstractions;
using Chaos.Services.Storage.Options;
using Chaos.Storage.Abstractions;
using Chaos.Utilities;
using Microsoft.Extensions.Options;
#endregion

namespace Chaos.Scripting.ReactorTileScripts;

/// <summary>
///     A door into a specific player's house. Placed once per house (e.g. in a housing-district hub map) and
///     configured with <see cref="OwnerName" /> - the door itself knows whose house it leads to, unlike
///     <see cref="Chaos.Scripting.ItemScripts.HouseKeyScript" />, which has to resolve that dynamically since a
///     single key item isn't tied to one physical placement. Modeled directly on <see cref="WarpScript" />'s
///     structure (a gate check, then <see cref="Creature.TraverseMap" />), with the level check swapped for
///     <see cref="HouseAccessHelper.TryAccess" />.
/// </summary>
public class HouseDoorScript : ConfigurableReactorTileScriptBase
{
    private readonly IStore<HouseOwnership> HouseStore;
    private readonly HouseOwnershipStoreOptions Options;
    private readonly ISimpleCache SimpleCache;

    /// <inheritdoc />
    public HouseDoorScript(
        ReactorTile subject,
        IStore<HouseOwnership> houseStore,
        IOptions<HouseOwnershipStoreOptions> options,
        ISimpleCache simpleCache)
        : base(subject)
    {
        HouseStore = houseStore;
        Options = options.Value;
        SimpleCache = simpleCache;
    }

    /// <inheritdoc />
    public override void OnWalkedOn(Creature source)
    {
        if (source is not Aisling aisling)
            return;

        if (!HouseStore.Exists(OwnerName))
        {
            aisling.SendOrangeBarMessage("This house is unclaimed.");
            Bounce(aisling);

            return;
        }

        var ownership = HouseStore.Load(OwnerName);
        var rentInterval = TimeSpan.FromDays(Options.RentIntervalDays);

        var result = HouseAccessHelper.TryAccess(aisling, ownership, HouseStore, rentInterval, out var entryLocation);

        switch (result)
        {
            case HouseAccessHelper.HouseAccessResult.Granted:
            {
                var targetMap = SimpleCache.Get<MapInstance>(entryLocation!.Map);
                aisling.TraverseMap(targetMap, entryLocation);

                break;
            }
            case HouseAccessHelper.HouseAccessResult.NotAuthorized:
            {
                aisling.SendOrangeBarMessage("This isn't your house.");
                Bounce(aisling);

                break;
            }
            case HouseAccessHelper.HouseAccessResult.RentOverdue:
            {
                aisling.SendOrangeBarMessage(
                    aisling.Name.EqualsI(OwnerName)
                        ? "The rent wasn't paid in time - this house has been repossessed."
                        : "This house's rent is overdue and access is suspended.");
                Bounce(aisling);

                break;
            }
            case HouseAccessHelper.HouseAccessResult.NoHouseAssigned:
            {
                aisling.SendOrangeBarMessage("This house hasn't been assigned yet.");
                Bounce(aisling);

                break;
            }
        }
    }

    /// <summary>
    ///     Steps the creature back off the door tile so a failed check doesn't leave them standing on (and
    ///     re-triggering) the reactor - same bounce-back used by <see cref="WarpScript" />'s level gate.
    /// </summary>
    private void Bounce(Creature source)
    {
        var point = source.DirectionalOffset(source.Direction.Reverse());
        source.WarpTo(source.Trackers.LastPosition as IPoint ?? point);
    }

    #region ScriptVars
    /// <summary>
    ///     The player name whose house this door leads to. Configured per-placement, since a door is physically one
    ///     specific house's entrance.
    /// </summary>
    public string OwnerName { get; init; } = null!;
    #endregion
}
