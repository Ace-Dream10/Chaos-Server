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
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     A single, devastating punch on the target directly in front - massive damage, a hard shove straight away
///     from the caster (same pushback logic as Heaven's Recoil), and a brief stun on landing.
/// </summary>
public class OneInchPunchScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public OneInchPunchScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

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

        var damage = (BaseDamage ?? 0)
                     + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.STR) * (DamageStatMultiplier ?? 1));

        if (damage > 0)
            ApplyDamageScript.ApplyDamage(source, target, this, damage);

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

    public IApplyDamageScript ApplyDamageScript { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt
    /// </summary>
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The stat used to scale bonus damage
    /// </summary>
    public Stat? DamageStat { get; init; }

    /// <summary>
    ///     The multiplier applied to <see cref="DamageStat" /> when calculating bonus damage
    /// </summary>
    public decimal? DamageStatMultiplier { get; init; }

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
