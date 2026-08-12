#region
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     One of Tempest's 7 specialization actives - "instantly reposition, leaving behind an afterimage" per the
///     locked design. Cloned from the shared BlinkScript (a Spell script - every other Martial Artist ability is a
///     Skill, so this is a Skill-typed clone of the identical mechanic, same reasoning as Bastion's own
///     IronReprisalEffect being cloned from Counter Strike rather than shared directly) rather than reused as-is.
///     The "afterimage" flavor is realized entirely through <see cref="DepartureAnimation" /> - no separate decoy
///     entity, a deliberate scoping decision rather than standing up a full decoy-monster system for one
///     ability's flavor text.
/// </summary>
public class FlickeringStepScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public FlickeringStepScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
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

            var creature = map.GetEntitiesAtPoints<Creature>(point)
                              .TopOrDefault();

            //can't step through someone
            if (creature != null)
                break;

            lastWalkablePoint = point;
        }

        if (lastWalkablePoint.Equals(Point.From(source)))
        {
            context.SourceAisling?.SendOrangeBarMessage("There's nowhere to step to.");

            return;
        }

        //the afterimage - played at the point being left behind
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
    ///     The animation played at the point the caster stepped from - the "afterimage" the locked design calls for
    /// </summary>
    public Animation? DepartureAnimation { get; init; }

    /// <summary>
    ///     The maximum number of tiles the caster can step forward
    /// </summary>
    public int Range { get; init; } = 4;

    /// <summary>
    ///     Sound played at the arrival point
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
