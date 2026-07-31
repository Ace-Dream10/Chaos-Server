#region
using Chaos.DarkAges.Definitions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     A swift backward step. Tries the tile directly behind the caster first; if that's blocked by a wall or a
///     creature, it skips to the tile 2 tiles behind instead. If both are blocked, the caster stays put but still
///     plays the dash animation.
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
        else if (map.IsWalkable(twoBehind, source, false))
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
