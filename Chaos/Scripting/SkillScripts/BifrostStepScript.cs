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
using Chaos.Scripting.SkillScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Dashes forward, leaving a bridge behind for allies at higher tiers. Mirrors
///     <see cref="Chaos.Scripting.SpellScripts.ArcaneGateScript" />'s exact dash + reactor-tile-portal shape.
/// </summary>
/// <remarks>
///     One of Valkyrie's 6 evolving abilities - evolves through BEHAVIOR, not just numbers, per the locked design:
///     Tier I only you dash (no portal left behind at all); Tier II allies can use the bridge too (portal
///     appears, ally-filtered - see <see cref="BifrostStepPortalScript" />); Tier III the bridge lasts longer;
///     Tier IV allies gain a buff after crossing (see <see cref="Chaos.Scripting.EffectScripts.BifrostBlessingEffect" />'s doc comment for why
///     that's a damage buff, not a movement-speed buff - no such mechanic exists in this engine). Tier scales with
///     the skill's own level, re-derived for Bifrost Step's own floor arc (Floor 2 intro through Floor 5 max) -
///     see <see cref="GetTierValues" />.
/// </remarks>
public class BifrostStepScript : ConfigurableSkillScriptBase
{
    private readonly IReactorTileFactory ReactorTileFactory;

    /// <inheritdoc />
    public BifrostStepScript(Skill subject, IReactorTileFactory reactorTileFactory)
        : base(subject)
        => ReactorTileFactory = reactorTileFactory;

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var originPoint = Point.From(source);
        var tier = GetTierValues();

        source.AnimateBody(BodyAnimation);

        var endPoint = source.DirectionalOffset(source.Direction, DashDistance);
        var points = source.GetDirectPath(endPoint).Skip(1);

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

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, originPoint);

        //Tier I: only you dash - no bridge left behind at all
        if (!tier.LeavesBridge)
            return;

        var scriptKey = ScriptBase.GetScriptKey(typeof(BifrostStepPortalScript));

        var portal = ReactorTileFactory.Create(
            map,
            originPoint,
            false,
            [scriptKey],
            new Dictionary<string, IScriptVars>(),
            source,
            this);

        if (portal.Script.As<BifrostStepPortalScript>() is { } portalScript)
        {
            portalScript.PulseAnimation = Animation;
            portalScript.GrantsCrossingBlessing = tier.GrantsCrossingBlessing;
        }

        map.SimpleAdd(portal);
        PendingRemovals.Add(new PendingRemoval(TimeSpan.FromMilliseconds(tier.BridgeDurationMs), portal));
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

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor2(Level&lt;=4)=I(intro,solo), Floor3(&lt;=6)=II
    ///     (bridge for allies), Floor4(&lt;=8)=III(longer bridge), Floor5+(&gt;8)=IV(max, +crossing blessing).
    /// </summary>
    private (bool LeavesBridge, int BridgeDurationMs, bool GrantsCrossingBlessing) GetTierValues() =>
        Subject.Level switch
        {
            <= 4 => (false, 0, false),
            <= 6 => (true, 4000, false),
            <= 8 => (true, 7000, false),
            _    => (true, 10000, true)
        };

    private readonly List<PendingRemoval> PendingRemovals = [];

    private sealed class PendingRemoval(TimeSpan remaining, ReactorTile tile)
    {
        public TimeSpan Remaining { get; set; } = remaining;
        public ReactorTile Tile { get; } = tile;
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played at both the origin and destination points, and re-played as the bridge's beacon
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The number of tiles the caster dashes forward
    /// </summary>
    public int DashDistance { get; init; } = 4;

    /// <summary>
    ///     Sound played at the origin point on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
