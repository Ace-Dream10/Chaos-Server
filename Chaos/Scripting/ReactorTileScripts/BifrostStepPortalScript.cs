#region
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.ReactorTileScripts.Abstractions;
#endregion

namespace Chaos.Scripting.ReactorTileScripts;

/// <summary>
///     Teleports an ALLY who walks onto this tile to the current position of the tile's owner (the caster of
///     Bifrost Step) - unlike <see cref="ArcaneGatePortalScript" /> (which lets anyone use it), this checks
///     friendliness, since the locked design specifically frames this as "allies can use the bridge too", not
///     "anyone". Hostile creatures walking onto it are ignored, not teleported.
/// </summary>
public class BifrostStepPortalScript : ReactorTileScriptBase
{
    private TimeSpan SinceLastPulse = TimeSpan.Zero;

    /// <inheritdoc />
    public BifrostStepPortalScript(ReactorTile subject)
        : base(subject) { }

    /// <summary>
    ///     Tier IV only - if set, whoever crosses also receives <see cref="BifrostBlessingEffect" />
    /// </summary>
    public bool GrantsCrossingBlessing { get; set; }

    /// <summary>
    ///     The animation re-played on this tile's point at every pulse interval
    /// </summary>
    public Animation? PulseAnimation { get; set; }

    /// <summary>
    ///     The interval, in milliseconds, at which the beacon animation is re-played
    /// </summary>
    public int PulseIntervalMs { get; set; } = 500;

    /// <inheritdoc />
    public override void OnWalkedOn(Creature source)
    {
        var owner = Subject.Owner;

        if ((owner is not { IsAlive: true })
            || (source.Id == owner.Id)
            || !ReferenceEquals(source.MapInstance, owner.MapInstance)
            || !owner.IsFriendlyTo(source))
            return;

        source.WarpTo(Point.From(owner));

        if (GrantsCrossingBlessing)
            source.Effects.Apply(owner, new BifrostBlessingEffect(), this);
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
