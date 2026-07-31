#region
using Chaos.Collections;
using Chaos.Common.Abstractions;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SpellScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

public class GlacialBarrierScript : ConfigurableSpellScriptBase
{
    private const int ColdDamageOnContact = 50;
    private readonly IApplyDamageScript ApplyDamageScript;
    private readonly IReactorTileFactory ReactorTileFactory;
    private readonly List<PendingWall> PendingWalls = [];

    /// <inheritdoc />
    public GlacialBarrierScript(Spell subject, IReactorTileFactory reactorTileFactory)
        : base(subject)
    {
        ReactorTileFactory = reactorTileFactory;
        ApplyDamageScript = ApplyAttackDamageScript.Create();
    }

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var level = Subject.Level;
        var origin = Point.From(source);

        var wallTileCount = level switch
        {
            <= 3  => 3,
            <= 6  => 5,
            <= 9  => 7,
            _     => 9
        };

        var dealsColdDamage = level >= 10;

        source.AnimateBody(BodyAnimation);

        var (forward, lateral) = GetAxes(source.Direction);
        var apex = new Point(source.X + (forward.X * 2), source.Y + (forward.Y * 2));

        var armLength = (wallTileCount - 1) / 2;
        var points = new List<Point> { apex };

        for (var i = 1; i <= armLength; i++)
        {
            points.Add(new Point(apex.X - (forward.X * i) + (lateral.X * i), apex.Y - (forward.Y * i) + (lateral.Y * i)));
            points.Add(new Point(apex.X - (forward.X * i) - (lateral.X * i), apex.Y - (forward.Y * i) - (lateral.Y * i)));
        }

        var wall = new PendingWall(source, map, origin);

        foreach (var point in points)
        {
            if (map.IsWall(point))
                continue;

            if (dealsColdDamage)
                foreach (var creature in map.GetEntitiesAtPoints<Creature>(point))
                    if (creature.IsAlive && source.IsHostileTo(creature))
                        ApplyDamageScript.ApplyDamage(source, creature, this, ColdDamageOnContact, Element.Water);

            var tile = ReactorTileFactory.Create(map, point, true, [], new Dictionary<string, IScriptVars>(), source, this);
            map.SimpleAdd(tile);
            wall.Tiles.Add((point, tile));

            if (Animation != null)
                map.ShowAnimation(Animation.GetPointAnimation(point, source.Id));
        }

        if (wall.Tiles.Count > 0)
            PendingWalls.Add(wall);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (PendingWalls.Count == 0)
            return;

        var tickInterval = TimeSpan.FromMilliseconds(TickIntervalMs);
        var totalDuration = TimeSpan.FromMilliseconds(WallDurationMs);

        for (var i = PendingWalls.Count - 1; i >= 0; i--)
        {
            var wall = PendingWalls[i];
            wall.TotalElapsed += delta;
            wall.SinceLastTick += delta;

            if (wall.TotalElapsed >= totalDuration)
            {
                PendingWalls.RemoveAt(i);

                foreach (var (_, tile) in wall.Tiles)
                    tile.MapInstance.RemoveEntity(tile);

                continue;
            }

            if (wall.SinceLastTick < tickInterval)
                continue;

            wall.SinceLastTick -= tickInterval;

            foreach (var (point, tile) in wall.Tiles)
            {
                if (Animation != null)
                    wall.Map.ShowAnimation(Animation.GetPointAnimation(point, wall.Source.Id));

                var monster = wall.Map
                                  .GetEntitiesAtPoints<Monster>(point)
                                  .FirstOrDefault(m => m.IsAlive && wall.Source.IsHostileTo(m));

                if (monster == null)
                    continue;

                var pushDirection = point.DirectionalRelationTo(wall.Origin);
                var landingPoint = point.DirectionalOffset(pushDirection);

                if (wall.Map.IsWalkable(landingPoint, monster, false))
                    monster.WarpTo(landingPoint);
            }
        }
    }

    private static (Point Forward, Point Lateral) GetAxes(Direction direction)
        => direction switch
        {
            Direction.Up    => (new Point(0, -1), new Point(1, 0)),
            Direction.Down  => (new Point(0, 1), new Point(1, 0)),
            Direction.Left  => (new Point(-1, 0), new Point(0, 1)),
            Direction.Right => (new Point(1, 0), new Point(0, 1)),
            _               => (new Point(0, -1), new Point(1, 0))
        };

    private sealed class PendingWall(Creature source, MapInstance map, Point origin)
    {
        public MapInstance Map { get; } = map;
        public Point Origin { get; } = origin;
        public TimeSpan SinceLastTick { get; set; } = TimeSpan.Zero;
        public Creature Source { get; } = source;
        public List<(Point Point, ReactorTile Tile)> Tiles { get; } = [];
        public TimeSpan TotalElapsed { get; set; } = TimeSpan.Zero;
    }

    #region ScriptVars
    /// <summary>
    ///     The animation re-played on each wall tile every tick
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The sound played at the caster's position on cast
    /// </summary>
    public byte? Sound { get; init; }

    /// <summary>
    ///     How often, in milliseconds, wall tiles check for and push back standing monsters, and re-play their animation
    /// </summary>
    public int TickIntervalMs { get; init; } = 1000;

    /// <summary>
    ///     How long, in milliseconds, the wall's reactor tiles remain before being removed
    /// </summary>
    public int WallDurationMs { get; init; } = 3000;
    #endregion
}
