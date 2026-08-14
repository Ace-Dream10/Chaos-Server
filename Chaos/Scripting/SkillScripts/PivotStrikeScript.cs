#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
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
///     Reworked per playtest feedback: the original "reposition directly behind an enemy" version gave Bastion a
///     third gap-closer alongside Bastion's Charge and Lancer's Leash, which read as too mobile/agile for a tank
///     archetype. Both the reposition/teleport-behind-target mechanic AND a considered taunt/mark alternative
///     were confirmed dropped - this is now a purely stationary control tool: a straight-ahead strike (same
///     forward line-scan as Shield Thrust, no movement at all) that deals a modest hit and applies a brief stun.
///     No aggro/taunt component - Bastion still has no single-target taunt tool, which is a known, deliberately
///     deferred gap, not something this fix attempts to solve.
/// </summary>
public class PivotStrikeScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public PivotStrikeScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        var endPoint = source.DirectionalOffset(source.Direction, RangeTiles);

        var points = source.GetDirectPath(endPoint)
                            .Skip(1);

        foreach (var point in points)
        {
            if (map.IsWall(point) || map.IsBlockingReactor(point))
                return;

            var target = map.GetEntitiesAtPoints<Creature>(point)
                            .TopOrDefault();

            if (target == null)
                continue;

            if (!Filter.IsValidTarget(source, target))
                return;

            var damage = (BaseDamage ?? 0)
                         + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.STR) * (DamageStatMultiplier ?? 1));

            if (damage > 0)
                ApplyDamageScript.ApplyDamage(source, target, this, damage);

            if (target.IsAlive && (StunDurationMs > 0))
            {
                var rootEffect = new RootEffect();
                rootEffect.SetDuration(TimeSpan.FromMilliseconds(StunDurationMs));
                target.Effects.Apply(source, rootEffect, this);
            }

            if (Animation != null)
                target.Animate(Animation, source.Id);

            if (Sound.HasValue)
                map.PlaySound(Sound.Value, target);

            return;
        }
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the target when struck
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

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.DamageStat" />
    public Stat? DamageStat { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.DamageStatMultiplier" />
    public decimal? DamageStatMultiplier { get; init; }

    /// <summary>
    ///     The filter used to determine whether the first creature encountered along the path is a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The maximum number of tiles scanned in front of the caster for a target
    /// </summary>
    public int RangeTiles { get; init; } = 4;

    /// <summary>
    ///     Sound played on hit
    /// </summary>
    public byte? Sound { get; init; }

    /// <summary>
    ///     How long, in milliseconds, the target is stunned for
    /// </summary>
    public int StunDurationMs { get; init; } = 1000;
    #endregion
}
