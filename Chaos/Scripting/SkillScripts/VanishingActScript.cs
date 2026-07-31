#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Teleports the caster forward through empty space (same pattern as Jaunt/Arrowstep/Void Slash), then vanishes
///     from enemy sight via <see cref="VanishEffect" />.
/// </summary>
public class VanishingActScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public VanishingActScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        var endPoint = source.DirectionalOffset(source.Direction, TeleportDistance);

        var points = source.GetDirectPath(endPoint)
                           .Skip(1);

        var lastWalkablePoint = Point.From(source);

        foreach (var point in points)
        {
            if (map.IsWall(point) || map.IsBlockingReactor(point))
                break;

            lastWalkablePoint = point;
        }

        source.WarpTo(lastWalkablePoint);

        var vanishEffect = new VanishEffect();
        vanishEffect.SetDuration(TimeSpan.FromMilliseconds(VanishDurationMs));
        source.Effects.Apply(source, vanishEffect, this);

        if (Animation != null)
            source.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, lastWalkablePoint);
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
    ///     Sound played at the landing point
    /// </summary>
    public byte? Sound { get; init; }

    /// <summary>
    ///     The maximum number of tiles the caster teleports forward
    /// </summary>
    public int TeleportDistance { get; init; } = 3;

    /// <summary>
    ///     How long, in milliseconds, the caster stays vanished
    /// </summary>
    public int VanishDurationMs { get; init; } = 3000;
    #endregion
}
