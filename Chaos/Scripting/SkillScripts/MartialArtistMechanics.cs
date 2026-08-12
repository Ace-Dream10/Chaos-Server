#region
using Chaos.Collections;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;
using Chaos.Models.World.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Shared helper for Beast's "dash/land behind the target" mechanic, used by both Alpha Strike (ending behind
///     the final target of its multi-blink combo) and Traverse Punch (a single dash-behind-and-strike). Kept as a
///     small local static helper rather than a general engine-wide positioning system, same scoping approach as
///     BardMechanics/HealFormula-adjacent helpers built earlier this session.
/// </summary>
public static class MartialArtistMechanics
{
    /// <summary>
    ///     Prefers the tile directly behind the target's own facing direction, falling back to any adjacent
    ///     walkable tile if that spot is blocked
    /// </summary>
    public static Point FindLandingBehindTarget(MapInstance map, Creature source, Creature target)
    {
        var targetPoint = Point.From(target);
        var behindPoint = targetPoint.DirectionalOffset(target.Direction.Reverse());

        if (map.IsWalkable(behindPoint, source, false))
            return behindPoint;

        foreach (var direction in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
        {
            var candidate = targetPoint.DirectionalOffset(direction);

            if (map.IsWalkable(candidate, source, false))
                return candidate;
        }

        return targetPoint;
    }
}
