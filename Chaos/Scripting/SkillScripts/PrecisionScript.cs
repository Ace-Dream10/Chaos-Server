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
///     A precise strike against your target. If they're affected by Mark of the Bane, this attack ignores a
///     portion of their defense AND refreshes Mark of the Bane's duration - the intended combo loop: apply Mark
///     of the Bane, hit with Precision (bonus damage + keeps the mark alive), repeat. Not one of Slayer's 5
///     evolving abilities - flat numbers, per the locked design.
/// </summary>
public class PrecisionScript : ConfigurableSkillScriptBase
{
    private readonly IApplyDamageScript ApplyDamageScript;

    /// <inheritdoc />
    public PrecisionScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        var targetPoint = source.DirectionalOffset(source.Direction, Range);
        var target = map.GetEntitiesAtPoints<Creature>(targetPoint).TopOrDefault();

        source.AnimateBody(BodyAnimation);

        if ((target == null) || !Filter.IsValidTarget(source, target))
            return;

        var rawDamage = (BaseDamage ?? 0) + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.STR) * (DamageStatMultiplier ?? 1));

        var isMarked = target.Effects.TryGetEffect("Mark of the Bane", out var markEffect);

        var damage = isMarked
            ? DefenseIgnoreHelper.ApplyIgnoreDefense(target, rawDamage, MarkedIgnoreDefensePct)
            : rawDamage;

        if (damage > 0)
            ApplyDamageScript.ApplyDamage(source, target, this, damage);

        //Refreshes the mark's remaining time directly (not SetDuration - Remaining is time-left regardless of the
        //mark's original tier-based total Duration, which is exactly the "keeps the mark alive" semantic wanted)
        if (isMarked && (markEffect != null))
            markEffect.Remaining = TimeSpan.FromMilliseconds(RefreshedMarkDurationMs);

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
    ///     The flat portion of the damage dealt
    /// </summary>
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster when the skill is used
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
    ///     The filter used to determine whether the target tile holds a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The percentage (0-1) of the target's defense this strike ignores when they're affected by Mark of the
    ///     Bane
    /// </summary>
    public decimal MarkedIgnoreDefensePct { get; init; } = 0.3m;

    /// <summary>
    ///     The range, in tiles, at which the target is checked
    /// </summary>
    public int Range { get; init; } = 1;

    /// <summary>
    ///     The duration, in milliseconds, Mark of the Bane is refreshed to when Precision refreshes it
    /// </summary>
    public int RefreshedMarkDurationMs { get; init; } = 10000;

    /// <summary>
    ///     Sound played on hit
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
