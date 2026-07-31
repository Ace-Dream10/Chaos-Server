#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry.Abstractions.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Slashes the 4 tiles diagonal (catty-corner) to the caster - northeast, northwest, southeast, southwest.
///     Complements Broad Swipe, which covers the cardinal tiles instead.
/// </summary>
public class CrossSlashScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public CrossSlashScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        var points = new[]
        {
            source.DirectionalOffset(Direction.Up).DirectionalOffset(Direction.Right), //NE
            source.DirectionalOffset(Direction.Up).DirectionalOffset(Direction.Left), //NW
            source.DirectionalOffset(Direction.Down).DirectionalOffset(Direction.Right), //SE
            source.DirectionalOffset(Direction.Down).DirectionalOffset(Direction.Left) //SW
        };

        foreach (var point in points)
        {
            var target = map.GetEntitiesAtPoints<Creature>(point).TopOrDefault();

            if ((target == null) || !Filter.IsValidTarget(source, target))
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

    #region ScriptVars
    /// <summary>
    ///     The animation played on each creature struck
    /// </summary>
    public Animation? Animation { get; init; }

    public IApplyDamageScript ApplyDamageScript { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt per hit
    /// </summary>
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster when the skill is used
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The stat used to scale bonus damage
    /// </summary>
    public Stat? DamageStat { get; init; }

    /// <summary>
    ///     The multiplier applied to <see cref="DamageStat" /> when calculating bonus damage per hit
    /// </summary>
    public decimal? DamageStatMultiplier { get; init; }

    /// <summary>
    ///     The filter used to determine whether each of the 4 diagonal tiles holds a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     Sound played once, on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
