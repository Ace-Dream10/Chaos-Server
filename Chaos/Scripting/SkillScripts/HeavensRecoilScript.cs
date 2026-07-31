#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

public class HeavensRecoilScript : ConfigurableSkillScriptBase
{
    private const int ScanRange = 1;

    private readonly IApplyDamageScript ApplyDamageScript;

    /// <inheritdoc />
    public HeavensRecoilScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var sourcePoint = Point.From(source);

        source.AnimateBody(BodyAnimation);

        var targets = map.GetEntitiesWithinRange<Monster>(source, ScanRange)
                        .Where(monster => Filter.IsValidTarget(source, monster))
                        .ToArray();

        foreach (var monster in targets)
        {
            var monsterPoint = Point.From(monster);
            var pushDirection = monsterPoint.DirectionalRelationTo(sourcePoint);
            var landingPoint = monsterPoint.DirectionalOffset(pushDirection);

            if (map.IsWalkable(landingPoint, monster, false))
                monster.WarpTo(landingPoint);

            var damage = BaseDamage ?? 0;

            if (DamageStat.HasValue)
                damage += Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat.Value) * (DamageStatMultiplier ?? 1));

            ApplyDamageScript.ApplyDamage(source, monster, this, damage);

            if (Animation != null)
                monster.Animate(Animation, source.Id);
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, sourcePoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each hit monster
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt to each hit monster
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
    ///     The filter used to determine which nearby creatures are hit
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The sound played at the caster's position on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
