#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

public class GlaiveLeapScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public GlaiveLeapScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        var endPoint = source.DirectionalOffset(source.Direction, RushDistance);

        var points = source.GetDirectPath(endPoint)
                            .Skip(1);

        var lastWalkablePoint = Point.From(source);

        foreach (var point in points)
        {
            if (map.IsWall(point) || map.IsBlockingReactor(point))
                break;

            var creature = map.GetEntitiesAtPoints<Creature>(point)
                              .TopOrDefault();

            //stop just before a valid hostile target - no damage
            if ((creature != null) && Filter.IsValidTarget(source, creature))
                break;

            if (Animation != null)
                map.ShowAnimation(Animation.GetPointAnimation(point, source.Id));

            lastWalkablePoint = point;
        }

        source.WarpTo(lastWalkablePoint);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, lastWalkablePoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each tile traversed
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster at the start of the leap
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine which creatures in the path block the leap
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The maximum number of tiles the caster will leap forward
    /// </summary>
    public int RushDistance { get; init; } = 2;

    /// <summary>
    ///     Sound played at the landing point
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
