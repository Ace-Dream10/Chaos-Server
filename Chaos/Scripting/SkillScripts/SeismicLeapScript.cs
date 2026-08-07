#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Common;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Berserker's leap-slam. Leaps forward (path-walk pattern shared with <see cref="GlaiveLeapScript" />), then
///     deals AoE damage around the landing point (shape/range pattern shared with the generic "damage" script, e.g.
///     <see cref="Thunderstrike" />-style templates). One of Berserker's 5 evolving abilities: tier scales with the
///     skill's own level, using the same level-bracket convention <see cref="CycloneScript" /> already established
///     (1-2/3-4/5-6/7+ &#8594; tier I/II/III/IV), per the design's "larger impact &#8594; bigger radius &#8594;
///     slow &#8594; stun at max" evolution note.
/// </summary>
/// <remarks>
///     Tier III applies the existing, reusable <see cref="SlowEffect" />; tier IV applies the existing, reusable
///     "Root" effect (<see cref="RootEffect" />) - the same effect key <c>flatten.json</c> uses. This is the
///     confirmed reuse: Seismic Leap's max-tier stun calls the same generic Root effect directly, rather than
///     duplicating stun logic or depending on flatten.json's own skill template (which was intentionally left
///     untouched - its fate as a standalone ability is still an open decision, see the housing/berserker report).
///     <br />
///     <see cref="SlowEffect" /> only affects <see cref="Monster" /> targets (its own doc comment confirms this) -
///     it's a no-op against Aisling targets. Not a bug introduced here, just worth knowing.
///     <br />
///     All tier numbers (leap distance, impact range, damage) are placeholders, not balance-tested - see
///     <see cref="GetTierValues" />.
/// </remarks>
public class SeismicLeapScript : ConfigurableSkillScriptBase
{
    private readonly IEffectFactory EffectFactory;

    /// <inheritdoc />
    public SeismicLeapScript(Skill subject, IEffectFactory effectFactory)
        : base(subject)
    {
        EffectFactory = effectFactory;
        ApplyDamageScript = ApplyAttackDamageScript.Create();
    }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var (leapDistance, impactRange, applySlow, applyStun) = GetTierValues();

        source.AnimateBody(BodyAnimation);

        var endPoint = source.DirectionalOffset(source.Direction, leapDistance);
        var points = source.GetDirectPath(endPoint)
                            .Skip(1);

        var lastWalkablePoint = Point.From(source);

        foreach (var point in points)
        {
            if (map.IsWall(point) || map.IsBlockingReactor(point))
                break;

            lastWalkablePoint = point;
        }

        source.WarpTo(lastWalkablePoint);

        var impactPoints = AoeShape.AllAround.ResolvePoints(
                                        new AoeShapeOptions
                                        {
                                            Range = impactRange,
                                            Source = lastWalkablePoint
                                        })
                                    .ToArray();

        var targets = map.GetEntitiesAtPoints<Creature>(impactPoints)
                         .WithFilter(source, Filter)
                         .ToArray();

        foreach (var target in targets)
        {
            var damage = (BaseDamage ?? 0)
                         + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.STR) * (DamageStatMultiplier ?? 1));

            if (damage > 0)
                ApplyDamageScript.ApplyDamage(source, target, this, damage);

            if (Animation != null)
                target.Animate(Animation, source.Id);

            if (applySlow)
            {
                var slow = (SlowEffect)EffectFactory.Create("Slow");
                target.Effects.Apply(source, slow, this);
            }

            if (applyStun)
            {
                var stun = EffectFactory.Create("Root");
                target.Effects.Apply(source, stun, this);
            }
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, lastWalkablePoint);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Leap distance and impact radius grow through tier II, tier
    ///     III adds Slow, tier IV adds the Root stun on top (per the design's own "slow -&gt; stun at max" ordering
    ///     - stun replaces slow rather than both applying at tier IV, since a stunned target doesn't need to also be
    ///     slowed).
    /// </summary>
    private (int LeapDistance, int ImpactRange, bool ApplySlow, bool ApplyStun) GetTierValues() =>
        Subject.Level switch
        {
            <= 2 => (2, 1, false, false),
            <= 4 => (3, 2, false, false),
            <= 6 => (3, 2, true, false),
            _    => (3, 3, false, true)
        };

    #region ScriptVars
    public Animation? Animation { get; init; }
    public IApplyDamageScript ApplyDamageScript { get; init; }
    public int? BaseDamage { get; init; }
    public BodyAnimation BodyAnimation { get; init; }
    public Stat? DamageStat { get; init; }
    public decimal? DamageStatMultiplier { get; init; }
    public TargetFilter Filter { get; init; }
    public byte? Sound { get; init; }
    #endregion
}
