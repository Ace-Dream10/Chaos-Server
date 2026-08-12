#region
using Chaos.Collections;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     One of Tempest's 7 specialization actives - "piercing chi projectile that detonates after passing through
///     the target" per the locked design. A direct build: the primary target takes the piercing hit, then a
///     detonation burst hits everyone (including the primary target again, since the burst is centered on where
///     they were standing) within <see cref="DetonationRadius" /> of that point - the "after passing through"
///     wording realized as a second, delayed-in-name-only AoE component rather than a literal travel simulation.
///     Not one of Tempest's 5 evolving abilities - flat.
/// </summary>
public class ChiBulletScript : ConfigurableSkillScriptBase
{
    private readonly IApplyDamageScript ApplyDamageScript;

    /// <inheritdoc />
    public ChiBulletScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override bool CanUse(ActivationContext context)
    {
        if (!context.Source.IsAlive)
            return false;

        if ((context.TargetCreature is not { IsAlive: true } target) || !Filter.IsValidTarget(context.Source, target))
        {
            context.SourceAisling?.SendOrangeBarMessage("You must select a valid target.");

            return false;
        }

        if (context.SourcePoint.ManhattanDistanceFrom(context.TargetPoint) > Range)
        {
            context.SourceAisling?.SendOrangeBarMessage("Your target is too far away.");

            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var target = context.TargetCreature!;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        var pierceDamage = (BaseDamage ?? 0) + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.WIS) * (DamageStatMultiplier ?? 1));

        if (pierceDamage > 0)
            ApplyDamageScript.ApplyDamage(source, target, this, pierceDamage);

        if (target.IsAlive && (DetonationRadius > 0))
        {
            var detonationDamage = Convert.ToInt32(pierceDamage * DetonationDamagePct / 100m);

            foreach (var nearby in map.GetEntitiesWithinRange<Creature>(target, DetonationRadius))
            {
                if (!nearby.IsAlive || !Filter.IsValidTarget(source, nearby))
                    continue;

                if (detonationDamage > 0)
                    ApplyDamageScript.ApplyDamage(source, nearby, this, detonationDamage);

                if (DetonationAnimation != null)
                    nearby.Animate(DetonationAnimation, source.Id);
            }
        }

        if (Animation != null)
            target.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.TargetPoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the primary target on the piercing hit
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The flat portion of the piercing damage dealt to the primary target
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
    ///     The multiplier applied to <see cref="DamageStat" /> when calculating the piercing hit's bonus damage
    /// </summary>
    public decimal? DamageStatMultiplier { get; init; }

    /// <summary>
    ///     The animation played on each creature caught in the detonation
    /// </summary>
    public Animation? DetonationAnimation { get; init; }

    /// <summary>
    ///     The percentage of the piercing hit's damage dealt again to everyone caught in the detonation
    /// </summary>
    public int DetonationDamagePct { get; init; } = 60;

    /// <summary>
    ///     The radius, centered on the primary target, damaged by the detonation
    /// </summary>
    public int DetonationRadius { get; init; } = 2;

    /// <summary>
    ///     The filter used to determine whether a given creature is a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The maximum distance, in tiles, a target can be selected from
    /// </summary>
    public int Range { get; init; } = 8;

    /// <summary>
    ///     Sound played on hit
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
