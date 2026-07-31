#region
using Chaos.Common.Abstractions;
using Chaos.DarkAges.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.Abstractions;
using Chaos.Scripting.ReactorTileScripts;
using Chaos.Scripting.SpellScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

public class ArcaneGateScript : ConfigurableSpellScriptBase
{
    private readonly IReactorTileFactory ReactorTileFactory;
    private readonly List<PendingRemoval> PendingRemovals = [];

    /// <inheritdoc />
    public ArcaneGateScript(Spell subject, IReactorTileFactory reactorTileFactory)
        : base(subject)
        => ReactorTileFactory = reactorTileFactory;

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var originPoint = Point.From(source);

        source.AnimateBody(BodyAnimation);

        var endPoint = source.DirectionalOffset(source.Direction, TeleportDistance);

        var points = source.GetDirectPath(endPoint)
                            .Skip(1);

        var lastWalkablePoint = originPoint;

        foreach (var point in points)
        {
            if (map.IsWall(point) || map.IsBlockingReactor(point))
                break;

            lastWalkablePoint = point;
        }

        source.WarpTo(lastWalkablePoint);

        if (Animation != null)
        {
            map.ShowAnimation(Animation.GetPointAnimation(originPoint, source.Id));
            map.ShowAnimation(Animation.GetPointAnimation(lastWalkablePoint, source.Id));
        }

        var scriptKey = ScriptBase.GetScriptKey(typeof(ArcaneGatePortalScript));

        var portal = ReactorTileFactory.Create(
            map,
            originPoint,
            false,
            [scriptKey],
            new Dictionary<string, IScriptVars>(),
            source,
            this);

        //post-spawn configuration - the pulse animation/interval aren't scriptVars on the tile itself,
        //since this tile is created ad-hoc rather than from a JSON template
        if (portal.Script.As<ArcaneGatePortalScript>() is { } portalScript)
        {
            portalScript.PulseAnimation = PulseAnimation ?? Animation;
            portalScript.PulseIntervalMs = PulseIntervalMs;
        }

        map.SimpleAdd(portal);
        PendingRemovals.Add(new PendingRemoval(TimeSpan.FromMilliseconds(GateDurationMs), portal));

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, originPoint);
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (PendingRemovals.Count == 0)
            return;

        for (var i = PendingRemovals.Count - 1; i >= 0; i--)
        {
            var pending = PendingRemovals[i];
            pending.Remaining -= delta;

            if (pending.Remaining > TimeSpan.Zero)
                continue;

            PendingRemovals.RemoveAt(i);
            pending.Tile.MapInstance.RemoveEntity(pending.Tile);
        }
    }

    private sealed class PendingRemoval(TimeSpan remaining, ReactorTile tile)
    {
        public TimeSpan Remaining { get; set; } = remaining;
        public ReactorTile Tile { get; } = tile;
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played at both the origin and destination points
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     How long, in milliseconds, the portal left at the origin point remains before being removed
    /// </summary>
    public int GateDurationMs { get; init; } = 5000;

    /// <summary>
    ///     The sound played at the origin point on cast
    /// </summary>
    public byte? Sound { get; init; }

    /// <summary>
    ///     The number of tiles the caster teleports forward
    /// </summary>
    public int TeleportDistance { get; init; } = 5;

    /// <summary>
    ///     The animation re-played on the origin portal tile at every pulse interval, for its full lifetime. Falls back
    ///     to <see cref="Animation" /> if not set.
    /// </summary>
    public Animation? PulseAnimation { get; init; }

    /// <summary>
    ///     The interval, in milliseconds, at which the origin portal tile re-plays its beacon animation
    /// </summary>
    public int PulseIntervalMs { get; init; } = 500;
    #endregion
}
