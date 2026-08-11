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
///     Slashes the target and leaves a lingering <see cref="ShadowWoundEffect" /> DoT - a direct build, no
///     existing skill matched this shape closely enough to reuse/rename (Venom Blade was the closest candidate but
///     is a self-buff-then-next-hit ability, not a direct strike; retired as an orphan rather than stretched to
///     fit).
/// </summary>
public class PhantomBladeScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public PhantomBladeScript(Skill subject)
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

        var damage = (BaseDamage ?? 0) + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.DEX) * (DamageStatMultiplier ?? 1));

        if (damage > 0)
            ApplyDamageScript.ApplyDamage(source, target, this, damage);

        var wound = new ShadowWoundEffect { TickDamage = WoundTickDamage };
        target.Effects.Apply(source, wound, this);

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
    ///     The flat portion of the initial slash's damage
    /// </summary>
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster when the skill is used
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The stat used to scale the initial slash's bonus damage
    /// </summary>
    public Stat? DamageStat { get; init; }

    /// <summary>
    ///     The multiplier applied to <see cref="DamageStat" /> when calculating the initial slash's bonus damage
    /// </summary>
    public decimal? DamageStatMultiplier { get; init; }

    /// <summary>
    ///     The filter used to determine whether the tile directly in front of the caster holds a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The range, in tiles, at which the target is checked - should stay 1 for a melee strike
    /// </summary>
    public int Range { get; init; } = 1;

    /// <summary>
    ///     Sound played on hit
    /// </summary>
    public byte? Sound { get; init; }

    /// <summary>
    ///     The damage dealt per tick of the lingering shadow wound
    /// </summary>
    public int WoundTickDamage { get; init; } = 25;
    #endregion
}
