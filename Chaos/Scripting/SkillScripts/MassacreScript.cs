#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Common;
using Chaos.Geometry;
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
///     Berserker's evolving cleave. One of the 5 evolving abilities: tier scales with the skill's own level, using
///     the same level-bracket convention <see cref="CycloneScript" /> already established (1-2/3-4/5-6/7+
///     &#8594; tier I/II/III/IV), per the design's "wider cleave &#8594; larger AoE &#8594; additional hits
///     &#8594; huge finishing swing" evolution note. Calls <see cref="AoeShapeExtensions.ResolvePoints" /> directly
///     (the same shape-resolution logic <see cref="Chaos.Scripting.Components.AbilityComponents.GetTargetsAbilityComponent{TEntity}" />
///     uses internally) with a level-computed shape/range instead of a fixed one, since the interface that generic
///     component expects requires <c>init</c>-bound (not computed) properties and can't express tiering - same
///     reason <see cref="CycloneScript" /> also bypasses the component pipeline and drives its own tile logic.
/// </summary>
/// <remarks>
///     All tier numbers are placeholders, not balance-tested - see <see cref="GetTierValues" />. "Additional hits"
///     (tier III+) is interpreted here as a second pass over the same shape rather than a separate mechanic;
///     "huge finishing swing" (tier IV) switches the shape from a cone to a full circle around the caster rather
///     than just growing the cone further, to make the finisher feel distinct.
/// </remarks>
public class MassacreScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public MassacreScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var (shape, range, hitPasses) = GetTierValues();

        var points = shape.ResolvePoints(
                               new AoeShapeOptions
                               {
                                   Direction = source.Direction,
                                   Range = range,
                                   Source = Point.From(source)
                               })
                           .ToArray();

        var targets = map.GetEntitiesAtPoints<Creature>(points)
                         .WithFilter(source, Filter)
                         .ToArray();

        for (var pass = 0; pass < hitPasses; pass++)
            foreach (var target in targets)
            {
                if (!target.IsAlive)
                    continue;

                var damage = (BaseDamage ?? 0)
                             + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.STR) * (DamageStatMultiplier ?? 1));

                if (damage > 0)
                    ApplyDamageScript.ApplyDamage(source, target, this, damage);

                if (Animation != null)
                    target.Animate(Animation, source.Id);
            }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, source);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Tiers I-III widen the frontal cone; tier IV swaps to a full
    ///     circle around the caster ("huge finishing swing") and adds a second damage pass ("additional hits").
    /// </summary>
    private (AoeShape Shape, int Range, int HitPasses) GetTierValues() =>
        Subject.Level switch
        {
            <= 2 => (AoeShape.FrontalCone, 2, 1),
            <= 4 => (AoeShape.FrontalCone, 3, 1),
            <= 6 => (AoeShape.FrontalCone, 3, 2),
            _    => (AoeShape.AllAround, 3, 2)
        };

    #region ScriptVars
    public Animation? Animation { get; init; }
    public IApplyDamageScript ApplyDamageScript { get; init; }
    public int? BaseDamage { get; init; }
    public Stat? DamageStat { get; init; }
    public decimal? DamageStatMultiplier { get; init; }
    public TargetFilter Filter { get; init; }
    public byte? Sound { get; init; }
    #endregion
}
