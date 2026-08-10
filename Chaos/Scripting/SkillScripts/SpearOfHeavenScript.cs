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
///     Ambush ability - dives forward and slams down at the landing point.
/// </summary>
/// <remarks>
///     One of Valkyrie's 6 evolving abilities. Per the locked design's evolution note ("Leap → Bigger impact →
///     Holy explosion → Center stun"): Tier I is a plain leap-and-strike on the landing tile; Tier II increases
///     impact damage/radius; Tier III widens to a small AoE explosion around the landing point; Tier IV adds a
///     brief stun to whatever's at the exact center. Tier scales with the skill's own level, re-derived for Spear
///     of Heaven's own floor arc (Floor 3 intro, Floor 4, Floor 5, Floor 6 max) - see
///     <see cref="GetTierValues" /> for the exact breakdown.
/// </remarks>
public class SpearOfHeavenScript : ConfigurableSkillScriptBase
{
    private readonly IApplyDamageScript ApplyDamageScript;

    /// <inheritdoc />
    public SpearOfHeavenScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var tier = GetTierValues();

        source.AnimateBody(BodyAnimation);

        var endPoint = source.DirectionalOffset(source.Direction, LeapDistance);
        var points = source.GetDirectPath(endPoint).Skip(1).ToList();

        var landingPoint = points.Count > 0 ? points[^1] : Point.From(source);

        foreach (var point in points)
        {
            if (map.IsWall(point) || map.IsBlockingReactor(point))
            {
                landingPoint = point;

                break;
            }

            landingPoint = point;
        }

        source.WarpTo(landingPoint);

        if (Animation != null)
            map.ShowAnimation(Animation.GetPointAnimation(landingPoint, source.Id));

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, landingPoint);

        var centerTarget = map.GetEntitiesAtPoints<Creature>(landingPoint).TopOrDefault();

        if (tier.ImpactRange == 0)
        {
            //Tier I/II: center tile only
            StrikeIfPresent(source, centerTarget, tier.BaseDamage);
        } else
        {
            //Tier III/IV: small AoE explosion around the landing point
            foreach (var nearby in map.GetEntitiesWithinRange<Creature>(landingPoint, tier.ImpactRange))
                StrikeIfPresent(source, nearby, tier.BaseDamage);

            if (tier.CenterStunMs > 0 && (centerTarget != null) && Filter.IsValidTarget(source, centerTarget))
            {
                var stunEffect = new StasisEffect();
                stunEffect.SetDuration(TimeSpan.FromMilliseconds(tier.CenterStunMs));
                centerTarget.Effects.Apply(source, stunEffect, this);
            }
        }

        void StrikeIfPresent(Creature attacker, Creature? target, int damage)
        {
            if ((target == null) || !Filter.IsValidTarget(attacker, target))
                return;

            if (damage > 0)
                ApplyDamageScript.ApplyDamage(attacker, target, this, damage);

            if (Animation != null)
                target.Animate(Animation, attacker.Id);
        }
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor3(Level&lt;=6)=I(intro,single-target), Floor4
    ///     (&lt;=8)=II(bigger single-target impact), Floor5(&lt;=10)=III(small AoE), Floor6+(&gt;10)=IV(max, AoE +
    ///     center stun).
    /// </summary>
    private (int BaseDamage, int ImpactRange, int CenterStunMs) GetTierValues() =>
        Subject.Level switch
        {
            <= 6  => (80, 0, 0),
            <= 8  => (120, 0, 0),
            <= 10 => (140, 1, 0),
            _     => (170, 2, 1500)
        };

    #region ScriptVars
    /// <summary>
    ///     The animation played at the landing point
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster at the start of the dive
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine which creatures at/near the landing point are valid targets
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The maximum number of tiles the caster dives forward
    /// </summary>
    public int LeapDistance { get; init; } = 4;

    /// <summary>
    ///     Sound played at the landing point
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
