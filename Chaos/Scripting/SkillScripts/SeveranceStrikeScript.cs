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

public class SeveranceStrikeScript : ConfigurableSkillScriptBase
{
    private const string SeveranceTargetTag = "severanceTarget";
    private const string SeveredTag = "severed";
    private const string StacksTag = "severance_stacks";
    private const int MpPerStack = 20;

    private readonly IApplyDamageScript ApplyDamageScript;

    /// <inheritdoc />
    public SeveranceStrikeScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        var targetPoint = source.DirectionalOffset(source.Direction, Range);

        var target = map.GetEntitiesAtPoints<Creature>(targetPoint)
                        .TopOrDefault();

        source.AnimateBody(BodyAnimation);

        if ((target == null) || !Filter.IsValidTarget(source, target))
            return;

        var damage = (BaseDamage ?? 0) + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.STR) * (DamageStatMultiplier ?? 1));

        if (damage > 0)
            ApplyDamageScript.ApplyDamage(source, target, this, damage);

        SeveranceTargetSync.SwitchTargetIfNeeded(context.SourceAisling, map, target, SeveranceTargetTag, StacksTag, SeveredTag);

        target.Effects.Apply(
            source,
            new SeveranceEffect
            {
                StacksToApply = StacksToApply
            },
            this);

        SeveranceTargetSync.SyncMpToStacks(context.SourceAisling, target, StacksTag, MpPerStack);

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
    ///     The number of Severance stacks this strike applies
    /// </summary>
    public int StacksToApply { get; init; } = 1;
    #endregion
}
