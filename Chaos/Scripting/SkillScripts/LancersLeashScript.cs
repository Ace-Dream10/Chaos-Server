#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     One of Bastion's 5 evolving abilities - design intent is "starts as a small pull, eventually gathers
///     entire groups, longer root" (see ELYSIUM_CLASS_DESIGN.md). Previously had a single fixed 13x15 scan area
///     and flat 6-target pull at every level, which is a screen-spanning area for a supposed base-tier "small
///     pull" - it never actually scaled with tier despite being documented as an evolving ability. Now uses the
///     same level-bracket convention as <see cref="BastionsChargeScript" />/<see cref="CycloneScript" /> (1-2/3-4/
///     5-6/7+ &#8594; tier I-IV): a genuinely small area/pull count at tier I, growing to the original 13x15/6
///     values by tier IV.
/// </summary>
public class LancersLeashScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public LancersLeashScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var tier = GetTierValues();

        source.AnimateBody(BodyAnimation);

        var scanArea = new Rectangle(context.SourcePoint, tier.ScanWidth, tier.ScanHeight);

        //monsters already adjacent to the caster don't need to be pulled - they're already right there
        var candidates = map.GetEntitiesAtPoints<Monster>(scanArea.GetPoints())
                            .Where(monster => Filter.IsValidTarget(source, monster))
                            .Where(monster => !context.SourcePoint.IsAdjacentTo(Point.From(monster)))
                            .ToArray();

        Random.Shared.Shuffle(candidates);

        var usedPoints = new HashSet<Point>
        {
            Point.From(source)
        };

        foreach (var monster in candidates.Take(tier.MaxTargets))
        {
            if (!TryFindLandingPoint(context, usedPoints, out var landingPoint))
                continue;

            usedPoints.Add(landingPoint);

            monster.WarpTo(landingPoint);
            monster.AggroList.AddAggro(source, 99999);

            var rootEffect = new RootEffect();
            rootEffect.SetDuration(TimeSpan.FromMilliseconds(tier.RootDurationMs));
            monster.Effects.Apply(source, rootEffect, this);

            if (Animation != null)
            {
                monster.Animate(Animation, source.Id);

                //fallback point animation so the effect shows regardless of the monster's armor/sprite
                map.ShowAnimation(Animation.GetPointAnimation(landingPoint, source.Id));
            }
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Tier IV matches the original pre-scaling values exactly.
    /// </summary>
    private (int ScanWidth, int ScanHeight, int MaxTargets, int RootDurationMs) GetTierValues() =>
        Subject.Level switch
        {
            <= 2 => (5, 5, 2, 1500),
            <= 4 => (7, 7, 3, 1750),
            <= 6 => (9, 9, 4, 2000),
            _    => (13, 15, 6, 2500)
        };

    /// <summary>
    ///     Finds the closest walkable point to the caster, spiraling outward, that hasn't already been claimed by a
    ///     previously-pulled monster this cast
    /// </summary>
    private static bool TryFindLandingPoint(ActivationContext context, ISet<Point> usedPoints, out Point landingPoint)
    {
        var source = context.Source;
        var map = context.TargetMap;

        foreach (var point in Point.From(source)
                                   .SpiralSearch())
        {
            if (usedPoints.Contains(point))
                continue;

            if (map.IsWalkable(point, source, false))
            {
                landingPoint = point;

                return true;
            }
        }

        landingPoint = default;

        return false;
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each pulled monster
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine which creatures within the scan area are valid pull targets
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The sound played at the caster's position on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
