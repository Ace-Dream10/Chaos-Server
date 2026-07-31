#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     Teleports the caster forward through empty space (same pattern as Jaunt/Arrowstep), then strikes whatever
///     hostile creature occupies the landing tile.
/// </summary>
public class VoidSlashScript : ConfigurableSpellScriptBase
{
    /// <inheritdoc />
    public VoidSlashScript(Spell subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        var endPoint = source.DirectionalOffset(source.Direction, TeleportDistance);

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

        var target = map.GetEntitiesAtPoints<Creature>(lastWalkablePoint)
                        .Where(creature => !creature.Equals(source))
                        .TopOrDefault();

        if ((target != null) && Filter.IsValidTarget(source, target))
        {
            var damage = (BaseDamage ?? 0)
                         + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.INT) * (DamageStatMultiplier ?? 1));

            if (damage > 0)
                ApplyDamageScript.ApplyDamage(source, target, this, damage, Element);

            if (Animation != null)
                target.Animate(Animation, source.Id);
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, lastWalkablePoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the arrival tile's occupant, if hit
    /// </summary>
    public Animation? Animation { get; init; }

    public IApplyDamageScript ApplyDamageScript { get; init; }

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
    ///     The multiplier applied to <see cref="DamageStat" /> when calculating bonus damage
    /// </summary>
    public decimal? DamageStatMultiplier { get; init; }

    /// <summary>
    ///     The element of the damage dealt
    /// </summary>
    public Element? Element { get; init; }

    /// <summary>
    ///     The filter used to determine whether the arrival tile's occupant is a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     Sound played at the landing point
    /// </summary>
    public byte? Sound { get; init; }

    /// <summary>
    ///     The maximum number of tiles the caster teleports forward
    /// </summary>
    public int TeleportDistance { get; init; } = 3;
    #endregion
}
