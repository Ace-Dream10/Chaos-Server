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
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     A direct build - nothing existing matched "pull nearby enemies toward the impact point" (no pull/knockback
///     primitive exists anywhere in this codebase yet). Implemented by warping each caught enemy one step along
///     its own direct path toward the impact point, stopping short if that step isn't walkable - a simplification
///     flagged here rather than a true physics-style pull. Flat, non-evolving - not one of Fletcher's 5 evolving
///     abilities.
/// </summary>
public class GravityArrowScript : ConfigurableSpellScriptBase
{
    private readonly IApplyDamageScript ApplyDamageScript;

    /// <inheritdoc />
    public GravityArrowScript(Spell subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var impactPoint = context.TargetPoint;

        if (!source.StatSheet.TrySubtractMp(ManaCost))
        {
            context.SourceAisling?.SendOrangeBarMessage("Not enough focus.");

            return;
        }

        context.SourceAisling?.Client.SendAttributes(StatUpdateType.Vitality);

        source.AnimateBody(BodyAnimation);

        var targets = map.GetEntitiesWithinRange<Monster>(impactPoint, Range)
                         .Where(monster => Filter.IsValidTarget(source, monster))
                         .ToList();

        foreach (var target in targets)
        {
            Pull(map, target, impactPoint);

            if (BaseDamage is > 0)
                ApplyDamageScript.ApplyDamage(source, target, this, BaseDamage.Value);

            if (Animation != null)
                target.Animate(Animation, source.Id);
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, impactPoint);
    }

    /// <summary>
    ///     Warps the target <see cref="PullDistance" /> steps along its own direct path toward the impact point,
    ///     stopping early at the first unwalkable tile
    /// </summary>
    private static void Pull(MapInstance map, Creature target, Point impactPoint)
    {
        var targetPoint = Point.From(target);

        if (targetPoint == impactPoint)
            return;

        var path = target.GetDirectPath(impactPoint)
                          .Take(PullDistance)
                          .ToList();

        var lastWalkable = targetPoint;

        foreach (var point in path)
        {
            if (!map.IsWalkable(point, target, false))
                break;

            lastWalkable = point;
        }

        if (lastWalkable != targetPoint)
            target.WarpTo(lastWalkable);
    }

    private const int PullDistance = 3;

    #region ScriptVars
    /// <summary>
    ///     The animation played on each pulled target
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     Flat damage dealt to each pulled target
    /// </summary>
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine which nearby monsters are pulled
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The MP cost to use this spell
    /// </summary>
    public int ManaCost { get; init; }

    /// <summary>
    ///     The radius, around the impact point, within which enemies are pulled
    /// </summary>
    public int Range { get; init; } = 3;

    /// <summary>
    ///     Sound played at the impact point
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
