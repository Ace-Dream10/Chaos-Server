#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     An impossibly fast flurry of slashes on a single target - <see cref="HitCount" /> individually weaker hits
///     in immediate succession, same "loop the strike N times" shape Heavenly Strike's Tier II ("triple") uses. Not
///     one of Assassin's 5 evolving abilities per the locked design - flat, like Shadow Reap/Vanishing Slash.
/// </summary>
public class EclipseScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public EclipseScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        var targetPoint = source.DirectionalOffset(source.Direction, Range);
        var target = map.GetEntitiesAtPoints<Creature>(targetPoint).TopOrDefault();

        if ((target == null) || !Filter.IsValidTarget(source, target))
            return;

        var perHitDamage = (BaseDamage ?? 0) + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.DEX) * (DamageStatMultiplier ?? 1));

        for (var i = 0; i < HitCount; i++)
        {
            if (!target.IsAlive)
                break;

            if (perHitDamage > 0)
                ApplyDamageScript.ApplyDamage(source, target, this, perHitDamage);

            if (Animation != null)
                target.Animate(Animation, source.Id);
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, targetPoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the target on each hit
    /// </summary>
    public Animation? Animation { get; init; }

    public IApplyDamageScript ApplyDamageScript { get; init; }

    /// <summary>
    ///     The flat portion of each individual hit's damage
    /// </summary>
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster when the skill is used
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The stat used to scale each hit's bonus damage
    /// </summary>
    public Stat? DamageStat { get; init; }

    /// <summary>
    ///     The multiplier applied to <see cref="DamageStat" /> when calculating each hit's bonus damage
    /// </summary>
    public decimal? DamageStatMultiplier { get; init; }

    /// <summary>
    ///     The filter used to determine whether the tile directly in front of the caster holds a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The number of individual hits in the flurry
    /// </summary>
    public int HitCount { get; init; } = 5;

    /// <summary>
    ///     The range, in tiles, at which the target is checked - should stay 1 for a melee strike
    /// </summary>
    public int Range { get; init; } = 1;

    /// <summary>
    ///     Sound played on hit
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
