#region
using System.Runtime.CompilerServices;
using Chaos.Collections;
using Chaos.Extensions.Geometry;
using Chaos.Geometry.Abstractions;
using Chaos.Geometry.EqualityComparers;
#endregion

namespace Chaos.Extensions;

public static class PointExtensions
{
    [OverloadResolutionPriority(1)]
    public static IEnumerable<Point> FilterByLineOfSight(
        this IEnumerable<Point> points,
        Point origin,
        MapInstance mapInstance,
        bool invertLos = false)
    {
        ArgumentNullException.ThrowIfNull(points);

        ArgumentNullException.ThrowIfNull(mapInstance);

        if (!invertLos)
            return points.Where(point => !origin.RayTraceTo(point)
                                                .Any(mapInstance.IsWall));

        points = points.ToArray();

        var occludedPoints = points.Where(mapInstance.IsWall)
                                   .SelectMany(point => point.RayTraceTo(origin));

        return points.Except(occludedPoints);
    }

    public static IEnumerable<T> FilterByLineOfSight<T>(
        this IEnumerable<T> points,
        IPoint origin,
        MapInstance mapInstance,
        bool invertLos = false) where T: IPoint
    {
        ArgumentNullException.ThrowIfNull(points);

        ArgumentNullException.ThrowIfNull(origin);

        ArgumentNullException.ThrowIfNull(mapInstance);

        var pointSet = points.OfType<IPoint>()
                             .ToHashSet(PointEqualityComparer.Instance);

        foreach (var point in pointSet.Select(Point.From)
                                      .FilterByLineOfSight(Point.From(origin), mapInstance, invertLos))
            if (pointSet.TryGetValue(point, out var setPoint))
                yield return (T)setPoint;
    }

    [OverloadResolutionPriority(1)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool WithinRange(this Point point, Point other, int distance = 15) => point.ManhattanDistanceFrom(other) <= distance;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool WithinRange(this IPoint point, IPoint other, int distance = 15)
    {
        ArgumentNullException.ThrowIfNull(point);

        ArgumentNullException.ThrowIfNull(other);

        return Point.From(point)
                    .WithinRange(Point.From(other), distance);
    }

    /// <summary>
    ///     Determines whether the other point is within the 8 tiles surrounding this point (or the same tile).
    ///     Unlike <see cref="WithinRange(Chaos.Geometry.Point,Chaos.Geometry.Point,int)" />, which is Manhattan
    ///     distance, this is Chebyshev distance - a diagonal neighbor counts as adjacent too
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAdjacentTo(this IPoint point, IPoint other)
    {
        ArgumentNullException.ThrowIfNull(point);

        ArgumentNullException.ThrowIfNull(other);

        return (Math.Abs(point.X - other.X) <= 1) && (Math.Abs(point.Y - other.Y) <= 1);
    }
}