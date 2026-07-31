#region
using Chaos.Collections;
using Chaos.Geometry;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.ItemScripts.Abstractions;
using Chaos.Services.Other.Abstractions;
using Chaos.Storage.Abstractions;
#endregion

namespace Chaos.Scripting.ItemScripts;

/// <summary>
///     A consumable that warps the user to a fixed map/point on use, consuming one from the stack. Modeled on
///     <see cref="Chaos.Services.Other.MapTraversalService" />'s usage in
///     <see cref="Chaos.Scripting.AislingScripts.DefaultAislingScript" />'s shrine-revive traversal - the same
///     non-admin <see cref="IMapTraversalService.TraverseMap" /> call, just triggered by item use instead of death.
/// </summary>
public class UseToWarpScript : ConfigurableItemScriptBase
{
    private readonly ISimpleCache Cache;
    private readonly IMapTraversalService MapTraversalService;

    /// <inheritdoc />
    public UseToWarpScript(Item subject, ISimpleCache cache, IMapTraversalService mapTraversalService)
        : base(subject)
    {
        Cache = cache;
        MapTraversalService = mapTraversalService;
    }

    /// <inheritdoc />
    public override void OnUse(Aisling source)
    {
        var destinationMap = Cache.Get<MapInstance>(DestinationMap);
        var destinationPoint = new Point(DestinationX, DestinationY);

        source.Inventory.RemoveQuantityByTemplateKey(Subject.Template.TemplateKey, 1);

        MapTraversalService.TraverseMap(source, destinationMap, destinationPoint);

        if (Sound.HasValue)
            source.MapInstance.PlaySound(Sound.Value, destinationPoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The instance id of the map to warp the user to
    /// </summary>
    public string DestinationMap { get; init; } = null!;

    /// <summary>
    ///     The x coordinate of the point to warp the user to
    /// </summary>
    public int DestinationX { get; init; }

    /// <summary>
    ///     The y coordinate of the point to warp the user to
    /// </summary>
    public int DestinationY { get; init; }

    /// <summary>
    ///     Sound played at the departure point
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
