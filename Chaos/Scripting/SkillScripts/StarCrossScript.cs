#region
using Chaos.Collections;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     One of Tempest's 7 specialization actives - "fire multiple chi stars at nearby enemies" per the locked
///     design. A direct build: strikes up to <see cref="MaxTargets" /> random hostiles within range, each for the
///     same damage - no travel/positioning mechanic, unlike Beast's Alpha Strike. Not one of Tempest's 5 evolving
///     abilities - flat (the locked design's own evolving-list table marks this ⭐ but the explicit final 5-item
///     evolving list doesn't include it, same "table vs. final list" discrepancy already resolved the same way for
///     Ironscale/Tempest's own earlier drafts - treating the final list as authoritative).
/// </summary>
public class StarCrossScript : ConfigurableSkillScriptBase
{
    private readonly IApplyDamageScript ApplyDamageScript;

    /// <inheritdoc />
    public StarCrossScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        var candidates = map.GetEntitiesWithinRange<Monster>(source, Range)
                           .Where(monster => Filter.IsValidTarget(source, monster))
                           .Distinct()
                           .ToArray();

        Random.Shared.Shuffle(candidates);

        var selected = candidates.Take(MaxTargets);

        source.AnimateBody(BodyAnimation);

        foreach (var target in selected)
        {
            var damage = (BaseDamage ?? 0) + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.WIS) * (DamageStatMultiplier ?? 1));

            if (damage > 0)
                ApplyDamageScript.ApplyDamage(source, target, this, damage);

            if (Animation != null)
                target.Animate(Animation, source.Id);
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each struck target
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt per hit
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
    ///     The multiplier applied to <see cref="DamageStat" /> when calculating bonus damage per hit
    /// </summary>
    public decimal? DamageStatMultiplier { get; init; }

    /// <summary>
    ///     The filter used to determine which nearby monsters are valid targets
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The maximum number of targets struck per cast
    /// </summary>
    public int MaxTargets { get; init; } = 3;

    /// <summary>
    ///     The radius around the caster within which targets are chosen
    /// </summary>
    public int Range { get; init; } = 6;

    /// <summary>
    ///     Sound played on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
