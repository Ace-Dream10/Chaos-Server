using Chaos.Collections;
using Chaos.Geometry.Abstractions;
using Chaos.Models.World.Abstractions;

namespace Chaos.Models.World;

/// <summary>
///     Sprite is reused from the gold pile sprites as a placeholder - swap for a distinct visual later
/// </summary>
public sealed class GamePointPile(int amount, MapInstance mapInstance, IPoint point) : GroundEntity(
    "Stacia's Tear",
    Money.GetSprite(amount),
    mapInstance,
    point)
{
    public int Amount { get; } = amount;

    public override void OnClicked(Aisling source)
    {
        //nothing
        //there's a different packet for picking things up
    }
}
