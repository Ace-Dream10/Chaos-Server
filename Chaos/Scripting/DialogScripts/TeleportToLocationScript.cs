#region
using Chaos.Collections;
using Chaos.Geometry;
using Chaos.Models.Menu;
using Chaos.Models.World;
using Chaos.Scripting.DialogScripts.Abstractions;
using Chaos.Storage.Abstractions;
#endregion

namespace Chaos.Scripting.DialogScripts;

/// <summary>
///     Teleports the Aisling to a fixed, pre-configured map instance/point. Unlike <see cref="TeleportScript" />,
///     the destination is set via scriptVars rather than typed in by the player - meant for a menu of named
///     locations (e.g. a GM teleport item) rather than free-text entry.
/// </summary>
public class TeleportToLocationScript : ConfigurableDialogScriptBase
{
    private readonly ISimpleCache SimpleCache;

    /// <inheritdoc />
    public TeleportToLocationScript(Dialog subject, ISimpleCache simpleCache)
        : base(subject)
        => SimpleCache = simpleCache;

    /// <inheritdoc />
    public override void OnDisplaying(Aisling source)
    {
        MapInstance mapInstance;

        try
        {
            mapInstance = SimpleCache.Get<MapInstance>(MapInstanceId);
        } catch
        {
            source.SendOrangeBarMessage($"No map instance with the id of {MapInstanceId} was found");

            return;
        }

        var point = (X.HasValue && Y.HasValue)
            ? new Point(X.Value, Y.Value)
            : new Point(mapInstance.Template.Width / 2, mapInstance.Template.Height / 2);

        source.TraverseMap(mapInstance, point, true);
    }

    #region ScriptVars
    /// <summary>
    ///     The instance id of the map to teleport to
    /// </summary>
    public string MapInstanceId { get; init; } = string.Empty;

    /// <summary>
    ///     The X coordinate to teleport to. If null (along with Y), teleports to the center of the map
    /// </summary>
    public int? X { get; init; }

    /// <summary>
    ///     The Y coordinate to teleport to. If null (along with X), teleports to the center of the map
    /// </summary>
    public int? Y { get; init; }
    #endregion
}
