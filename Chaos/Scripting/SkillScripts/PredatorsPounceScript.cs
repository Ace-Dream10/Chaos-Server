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
///     One of Beast's 7 specialization actives (formerly "One Inch Punch", now renamed/reused as Predator's
///     Pounce/Wolf Fang) - "a heavy strike that stuns the target" per the locked design. Reworked per playtest
///     feedback: this dealt damage on top of the shove+stun, reading too much like Bastion's Shield Thrust
///     (damage + knockback) rather than its own identity. Now a PURE stun - no damage at all, just a hard shove
///     straight away from the caster (same pushback logic as Heaven's Recoil) and a brief root on landing. Not one
///     of Beast's 3 evolving abilities - flat.
/// </summary>
public class PredatorsPounceScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public PredatorsPounceScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var sourcePoint = Point.From(source);

        source.AnimateBody(BodyAnimation);

        var targetPoint = source.DirectionalOffset(source.Direction);
        var target = map.GetEntitiesAtPoints<Creature>(targetPoint)
                        .TopOrDefault();

        if ((target == null) || !Filter.IsValidTarget(source, target))
            return;

        if (target.IsAlive)
        {
            var targetCurrentPoint = Point.From(target);
            var pushDirection = targetCurrentPoint.DirectionalRelationTo(sourcePoint);
            var landingPoint = targetCurrentPoint.DirectionalOffset(pushDirection, PushbackTiles);

            if (map.IsWalkable(landingPoint, target, false))
                target.WarpTo(landingPoint);

            var rootEffect = new RootEffect();
            rootEffect.SetDuration(TimeSpan.FromMilliseconds(StunDurationMs));
            target.Effects.Apply(source, rootEffect, this);
        }

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
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine whether the tile directly in front of the caster holds a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     How many tiles the target is shoved back, directly away from the caster
    /// </summary>
    public int PushbackTiles { get; init; } = 2;

    /// <summary>
    ///     Sound played on hit
    /// </summary>
    public byte? Sound { get; init; }

    /// <summary>
    ///     How long, in milliseconds, the target is stunned for on landing
    /// </summary>
    public int StunDurationMs { get; init; } = 1000;
    #endregion
}
