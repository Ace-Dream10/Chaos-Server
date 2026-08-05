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

public class LancersLeashScript : ConfigurableSkillScriptBase
{
    private const int ScanHeight = 15;
    private const int ScanWidth = 13;

    /// <inheritdoc />
    public LancersLeashScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        var scanArea = new Rectangle(context.SourcePoint, ScanWidth, ScanHeight);

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

        foreach (var monster in candidates.Take(MaxTargets))
        {
            if (!TryFindLandingPoint(context, usedPoints, out var landingPoint))
                continue;

            usedPoints.Add(landingPoint);

            monster.WarpTo(landingPoint);
            monster.AggroList.AddAggro(source, 99999);

            var rootEffect = new RootEffect();
            rootEffect.SetDuration(TimeSpan.FromMilliseconds(RootDurationMs));
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
    ///     The maximum number of monsters that can be pulled per cast
    /// </summary>
    public int MaxTargets { get; init; } = 6;

    /// <summary>
    ///     How long, in milliseconds, pulled monsters are rooted in place
    /// </summary>
    public int RootDurationMs { get; init; } = 1500;

    /// <summary>
    ///     The sound played at the caster's position on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
