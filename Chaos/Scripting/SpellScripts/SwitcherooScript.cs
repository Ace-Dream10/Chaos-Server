#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     A direct build - nothing existing matched "instantly swap positions with an ally or enemy." Filter defaults
///     to allowing both friendly and hostile targets (neither hostileOnly nor friendlyOnly) since the locked
///     description explicitly covers both. Flat, non-evolving - not one of Trickster's 5 evolving abilities.
/// </summary>
public class SwitcherooScript : ConfigurableSpellScriptBase
{
    /// <inheritdoc />
    public SwitcherooScript(Spell subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        var endPoint = source.DirectionalOffset(source.Direction, Range);

        var points = source.GetDirectPath(endPoint)
                           .Skip(1);

        Creature? target = null;

        foreach (var point in points)
        {
            if (map.IsWall(point) || map.IsBlockingReactor(point))
                break;

            var entity = map.GetEntitiesAtPoints<Creature>(point)
                            .TopOrDefault();

            if (entity != null)
            {
                if (Filter.IsValidTarget(source, entity))
                    target = entity;

                break;
            }
        }

        if (target == null)
        {
            context.SourceAisling?.SendOrangeBarMessage("No target in range.");

            return;
        }

        source.AnimateBody(BodyAnimation);

        var sourcePoint = Point.From(source);
        var targetPoint = Point.From(target);

        source.WarpTo(targetPoint);
        target.WarpTo(sourcePoint);

        if (Animation != null)
        {
            map.ShowAnimation(Animation.GetPointAnimation(sourcePoint, source.Id));
            map.ShowAnimation(Animation.GetPointAnimation(targetPoint, source.Id));
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, sourcePoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played at both the departure and arrival points
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine whether the first creature encountered in the scan is a valid target -
    ///     defaults to allowing both allies and enemies, per the locked description
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The maximum number of tiles scanned in front of the caster for a target
    /// </summary>
    public int Range { get; init; } = 6;

    /// <summary>
    ///     Sound played at the departure point
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
