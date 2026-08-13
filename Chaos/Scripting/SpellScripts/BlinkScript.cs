#region
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

public class BlinkScript : ConfigurableSpellScriptBase
{
    /// <inheritdoc />
    public BlinkScript(Spell subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        var endPoint = source.DirectionalOffset(source.Direction, Range);

        var points = source.GetDirectPath(endPoint)
                            .Skip(1);

        var lastWalkablePoint = Point.From(source);

        foreach (var point in points)
        {
            if (map.IsWall(point) || map.IsBlockingReactor(point))
                break;

            //a creature blocks LANDING on its tile, but not travel past it - previously this stopped the blink
            //dead at the first occupied tile (including one immediately adjacent), refusing to fire at all with
            //"nowhere to blink to" even when open space existed just beyond. Now it skips over occupied tiles
            //and keeps advancing toward the farthest open tile in range, the same way a real "blink past a
            //threat" ability should behave.
            var creature = map.GetEntitiesAtPoints<Creature>(point)
                              .TopOrDefault();

            if (creature != null)
                continue;

            lastWalkablePoint = point;
        }

        if (lastWalkablePoint.Equals(Point.From(source)))
        {
            context.SourceAisling?.SendOrangeBarMessage("There's nowhere to blink to.");

            return;
        }

        //departure effect at the point being left behind
        if (DepartureAnimation != null)
            map.ShowAnimation(DepartureAnimation.GetPointAnimation(Point.From(source), source.Id));

        source.WarpTo(lastWalkablePoint);

        if (ArrivalAnimation != null)
            source.Animate(ArrivalAnimation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, lastWalkablePoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the caster upon arrival
    /// </summary>
    public Animation? ArrivalAnimation { get; init; }

    /// <summary>
    ///     The animation played at the point the caster blinked from
    /// </summary>
    public Animation? DepartureAnimation { get; init; }

    /// <summary>
    ///     The maximum number of tiles the caster can blink forward
    /// </summary>
    public int Range { get; init; } = 4;

    /// <summary>
    ///     Sound played at the arrival point
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
