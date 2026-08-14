#region
using Chaos.DarkAges.Definitions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     A swift backward step. Tries the tile directly behind the caster first; if that's blocked specifically by a
///     creature, it skips to the tile 2 tiles behind instead. If the near tile is blocked by a wall (or a
///     blocking reactor), it stops there rather than trying the far tile - IsWalkable only checks the destination
///     tile itself, not the path to it, so falling back to "2 behind" whenever "1 behind" merely failed would let
///     a 1-tile-thick wall get hopped straight through/over (confirmed real bug, not an admin/god-mode artifact -
///     IsWalkable's ignoreWalls only defaults true for WalkThrough creatures). If both attempts are blocked, the
///     caster stays put but still plays the dash animation.
/// </summary>
public class EvadeScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public EvadeScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        var behindDirection = source.Direction.Reverse();
        var oneBehind = source.DirectionalOffset(behindDirection);
        var twoBehind = source.DirectionalOffset(behindDirection, 2);

        if (map.IsWalkable(oneBehind, source, false))
            source.WarpTo(oneBehind);
        else if (!map.IsWall(oneBehind)
                 && !map.IsBlockingReactor(oneBehind)
                 && map.IsWalkable(twoBehind, source, false))
            source.WarpTo(twoBehind);

        source.AnimateBody(BodyAnimation);

        if (Animation != null)
            source.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, source);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the caster
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     Sound played on use
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
