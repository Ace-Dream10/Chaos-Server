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
///     One of Tempest's 7 specialization actives, and one of its 3 evolving abilities - "launch a storm of razor
///     feathers over an area" per the locked design (USDA-inspired). Damages every hostile within radius of a
///     targeted point, tier-scaling both damage and radius - the shape of the "larger, deadlier feather storm with
///     additional effects" evolution note.
/// </summary>
public class HailOfFeathersScript : ConfigurableSkillScriptBase
{
    private readonly IApplyDamageScript ApplyDamageScript;

    /// <inheritdoc />
    public HailOfFeathersScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override bool CanUse(ActivationContext context)
    {
        if (!context.Source.IsAlive)
            return false;

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
        var map = context.TargetMap;
        var tier = GetTierValues();

        source.AnimateBody(BodyAnimation);

        if (Animation != null)
            map.ShowAnimation(Animation.GetPointAnimation(context.TargetPoint, source.Id));

        foreach (var monster in map.GetEntitiesWithinRange<Monster>(context.TargetPoint, tier.Radius))
        {
            if (!monster.IsAlive || !Filter.IsValidTarget(source, monster))
                continue;

            var damage = (BaseDamage ?? 0) + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.WIS) * (tier.DamageStatMultiplier));

            if (damage > 0)
                ApplyDamageScript.ApplyDamage(source, monster, this, damage);
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.TargetPoint);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Per the locked Floor Schedule, Hail of Feathers doesn't
    ///     intro until Floor 7 (mirrors Alpha Strike/Shockwave's own late-arriving evolving actives):
    ///     Floor7(Level&lt;=14)=I(obtain,radius2), Floor8(&lt;=16)=II(radius2,harder),
    ///     Floor9(&lt;=18)=III(radius3,harder still), Floor10+(&gt;18)=IV(max,radius4,hardest).
    /// </summary>
    private (int Radius, decimal DamageStatMultiplier) GetTierValues() =>
        Subject.Level switch
        {
            <= 14 => (2, 1.5m),
            <= 16 => (2, 2m),
            <= 18 => (3, 2.5m),
            _     => (4, 3m)
        };

    #region ScriptVars
    /// <summary>
    ///     The animation played centered on the target point
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt to each hit target
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
    ///     The filter used to determine which hit creatures are valid targets
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The maximum distance, in tiles, the target point can be selected from
    /// </summary>
    public int Range { get; init; } = 8;

    /// <summary>
    ///     Sound played on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
