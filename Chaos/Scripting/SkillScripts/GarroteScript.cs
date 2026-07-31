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
///     Silences and bleeds a target, but only from behind - the target must be facing away from the caster.
/// </summary>
public class GarroteScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public GarroteScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        var targetPoint = source.DirectionalOffset(source.Direction, Range);
        var target = map.GetEntitiesAtPoints<Creature>(targetPoint).TopOrDefault();

        if ((target == null) || !Filter.IsValidTarget(source, target))
            return;

        //the direction the target would need to face to be looking at the caster
        var directionToFaceCaster = context.SourcePoint.DirectionalRelationTo(Point.From(target));
        var isFacingAway = target.Direction == directionToFaceCaster.Reverse();

        if (!isFacingAway)
        {
            context.SourceAisling?.SendOrangeBarMessage("Your target must have their back turned.");

            return;
        }

        source.AnimateBody(BodyAnimation);

        var blackoutEffect = new BlackoutEffect();
        blackoutEffect.SetDuration(TimeSpan.FromMilliseconds(SilenceDurationMs));
        target.Effects.Apply(source, blackoutEffect, this);

        var bleedEffect = new BleedEffect { BleedDamage = BleedDamagePerTick };
        bleedEffect.SetDuration(TimeSpan.FromMilliseconds(BleedDurationMs));
        target.Effects.Apply(source, bleedEffect, this);

        if (Animation != null)
            target.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, targetPoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the target on hit
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     How long, in milliseconds, the bleed lasts
    /// </summary>
    public int BleedDurationMs { get; init; } = 5000;

    /// <summary>
    ///     The amount of bleed damage dealt per tick
    /// </summary>
    public int BleedDamagePerTick { get; init; } = 30;

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine whether the tile directly in front of the caster holds a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The range, in tiles, at which the target is checked
    /// </summary>
    public int Range { get; init; } = 1;

    /// <summary>
    ///     How long, in milliseconds, the target is silenced for
    /// </summary>
    public int SilenceDurationMs { get; init; } = 3000;

    /// <summary>
    ///     Sound played on hit
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
