#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     One of Tempest's 7 specialization actives, and one of its 3 evolving abilities - "a ranged palm strike
///     whose impact materializes directly on the target" per the locked design, evolving "from a simple ranged
///     palm strike into an iconic chi technique." The "materializes directly on the target" flavor is realized by
///     playing the impact animation on the target itself rather than a traveling projectile - no projectile-travel
///     mechanic, just an instant ranged hit.
/// </summary>
public class VoidPalmScript : ConfigurableSkillScriptBase
{
    private readonly IApplyDamageScript ApplyDamageScript;

    /// <inheritdoc />
    public VoidPalmScript(Skill subject)
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
        var tier = GetTierValues();

        source.AnimateBody(BodyAnimation);

        var damage = (BaseDamage ?? 0) + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.WIS) * tier);

        if (damage > 0)
            ApplyDamageScript.ApplyDamage(source, target, this, damage);

        //the impact materializes directly on the target - no projectile travel
        if (Animation != null)
            target.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.TargetPoint);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Per the locked Floor Schedule, Void Palm intros and jumps
    ///     straight to II together at Floor 5 (4-named-stages-onto-3-checkpoints squeeze, same as Beast's Perfect
    ///     Form/Ironscale's Pressure Point): Floor5(Level&lt;=10)=intro+II combined, Floor6(&lt;=12)=III,
    ///     Floor7+(&gt;12)=IV(max).
    /// </summary>
    private decimal GetTierValues() =>
        Subject.Level switch
        {
            <= 10 => 2.5m,
            <= 12 => 3.5m,
            _     => 4.5m
        };

    #region ScriptVars
    /// <summary>
    ///     The animation played directly on the target on impact
    /// </summary>
    public Animation? Animation { get; init; }

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
    ///     The filter used to determine whether the selected target is valid
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
