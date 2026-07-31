#region
using Chaos.Collections;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     Pulls all hostiles within range toward the caster - same warp-adjacent pattern as Lancer's Leash, but no
///     root and a shorter default range.
/// </summary>
public class WhirlpoolScript : ConfigurableSpellScriptBase
{
    /// <inheritdoc />
    public WhirlpoolScript(Spell subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        var targets = map.GetEntitiesWithinRange<Monster>(context.SourcePoint, Range)
                         .Where(monster => Filter.IsValidTarget(source, monster))
                         .ToArray();

        var usedPoints = new HashSet<Point>
        {
            Point.From(source)
        };

        foreach (var monster in targets)
        {
            if (!TryFindLandingPoint(source, map, usedPoints, out var landingPoint))
                continue;

            usedPoints.Add(landingPoint);
            monster.WarpTo(landingPoint);

            if (Animation != null)
                monster.Animate(Animation, source.Id);
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    /// <summary>
    ///     Finds the closest walkable point to the caster, spiraling outward, that hasn't already been claimed by a
    ///     previously-pulled monster this cast
    /// </summary>
    private static bool TryFindLandingPoint(Creature source, MapInstance map, ISet<Point> usedPoints, out Point landingPoint)
    {
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
    ///     The filter used to determine which creatures within range are valid pull targets
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The radius around the caster within which hostiles are pulled
    /// </summary>
    public int Range { get; init; } = 3;

    /// <summary>
    ///     The sound played at the caster's position on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
