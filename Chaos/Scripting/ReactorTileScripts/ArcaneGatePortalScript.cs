#region
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.ReactorTileScripts.Abstractions;
#endregion

namespace Chaos.Scripting.ReactorTileScripts;

/// <summary>
///     Teleports whoever walks onto this tile to the current position of the tile's owner (the caster of Arcane
///     Gate), as long as the owner is still alive and on the same map. Also re-plays a beacon animation on itself at
///     a fixed interval for its lifetime, so allies can see where it is. <see cref="PulseAnimation" /> and
///     <see cref="PulseIntervalMs" /> are configured by the summoning script right after the tile is spawned.
/// </summary>
public class ArcaneGatePortalScript : ReactorTileScriptBase
{
    private TimeSpan SinceLastPulse = TimeSpan.Zero;

    /// <inheritdoc />
    public ArcaneGatePortalScript(ReactorTile subject)
        : base(subject) { }

    /// <summary>
    ///     The interval, in milliseconds, at which the beacon animation is re-played
    /// </summary>
    public int PulseIntervalMs { get; set; } = 500;

    /// <summary>
    ///     The animation re-played on this tile's point at every pulse interval
    /// </summary>
    public Animation? PulseAnimation { get; set; }

    /// <inheritdoc />
    public override void OnWalkedOn(Creature source)
    {
        var owner = Subject.Owner;

        if ((owner is not { IsAlive: true }) || (source.Id == owner.Id) || !ReferenceEquals(source.MapInstance, owner.MapInstance))
            return;

        source.WarpTo(Point.From(owner));
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (PulseAnimation == null)
            return;

        SinceLastPulse += delta;

        if (SinceLastPulse < TimeSpan.FromMilliseconds(PulseIntervalMs))
            return;

        SinceLastPulse = TimeSpan.Zero;
        Map.ShowAnimation(PulseAnimation.GetPointAnimation(Point, Subject.Owner?.Id));
    }
}
