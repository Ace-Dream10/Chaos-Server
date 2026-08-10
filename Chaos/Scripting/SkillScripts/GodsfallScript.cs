#region
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
using Chaos.Scripting.MonsterScripts;
using Chaos.Scripting.SkillScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     The ultimate holy AoE, centered on the caster.
/// </summary>
/// <remarks>
///     One of Valkyrie's 6 evolving abilities. Per the locked design's evolution note ("larger radius, more
///     damage, holy shockwave, lingering consecrated ground"): each tier increases radius and damage; Tier III
///     adds a "shockwave" (knockback on every hit target, mirrors the retired Heaven's Recoil's knockback shape);
///     Tier IV additionally leaves lingering consecrated ground behind (reuses the same hazard-field monster/pulse
///     pattern Sorcerer's Ember Field and Fire Wall use, via <see cref="IMonsterFactory" />, but as a
///     self-contained inline spawn rather than pulling in the full HazardFieldScript - Godsfall's ground is a
///     single fixed-radius patch, not a configurable footprint). Tier scales with the skill's own level,
///     re-derived for Godsfall's own floor arc (Floor 7 intro through Floor 10 max) - see
///     <see cref="GetTierValues" />.
/// </remarks>
public class GodsfallScript : ConfigurableSkillScriptBase
{
    private readonly IApplyDamageScript ApplyDamageScript;
    private readonly IMonsterFactory MonsterFactory;

    /// <inheritdoc />
    public GodsfallScript(Skill subject, IMonsterFactory monsterFactory)
        : base(subject)
    {
        ApplyDamageScript = ApplyAttackDamageScript.Create();
        MonsterFactory = monsterFactory;
    }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var sourcePoint = Point.From(source);
        var tier = GetTierValues();

        source.AnimateBody(BodyAnimation);

        if (Animation != null)
            map.ShowAnimation(Animation.GetPointAnimation(sourcePoint, source.Id));

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, sourcePoint);

        var targets = map.GetEntitiesWithinRange<Monster>(source, tier.Radius)
                        .Where(monster => Filter.IsValidTarget(source, monster))
                        .ToArray();

        foreach (var monster in targets)
        {
            ApplyDamageScript.ApplyDamage(source, monster, this, tier.BaseDamage);

            //Tier III+ "holy shockwave" - push every hit target back, same shape as Heaven's Recoil's knockback
            if (tier.HasShockwave)
            {
                var monsterPoint = Point.From(monster);
                var pushDirection = monsterPoint.DirectionalRelationTo(sourcePoint);
                var landingPoint = monsterPoint.DirectionalOffset(pushDirection);

                if (map.IsWalkable(landingPoint, monster, false))
                    monster.WarpTo(landingPoint);
            }
        }

        //Tier IV "lingering consecrated ground" - a single fixed hazard patch at the caster's own position
        if (tier.HasConsecratedGround)
        {
            var hazard = MonsterFactory.Create(ConsecratedGroundTemplateKey, map, sourcePoint);
            map.AddEntity(hazard, sourcePoint);

            if (hazard.Script.Is<DecoyExpirationScript>(out var expirationScript))
                expirationScript.DurationMs = ConsecratedGroundDurationMs;

            if (hazard.Script.Is<StaciasJudgmentPulseScript>(out var damagePulse))
            {
                damagePulse.Caster = source;
                damagePulse.Filter = Filter;
                damagePulse.BaseDamage = tier.BaseDamage / 4;
                damagePulse.DamageStat = Stat.STR;
                damagePulse.DamageStatMultiplier = 0;
                damagePulse.DamageRange = tier.Radius;
                damagePulse.PulseIntervalMs = 1000;
                damagePulse.DurationMs = ConsecratedGroundDurationMs;
                damagePulse.Animation = Animation;
            }
        }
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor7(Level&lt;=14)=I(intro), Floor8(&lt;=16)=II
    ///     (larger/more damage), Floor9(&lt;=18)=III(+shockwave), Floor10+(&gt;18)=IV(max, +consecrated ground).
    /// </summary>
    private (int BaseDamage, int Radius, bool HasShockwave, bool HasConsecratedGround) GetTierValues() =>
        Subject.Level switch
        {
            <= 14 => (150, 2, false, false),
            <= 16 => (200, 3, false, false),
            <= 18 => (260, 3, true, false),
            _     => (320, 4, true, true)
        };

    #region ScriptVars
    /// <summary>
    ///     The animation played at the caster's position on cast
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     How long, in milliseconds, the Tier IV consecrated ground patch lingers
    /// </summary>
    public int ConsecratedGroundDurationMs { get; init; } = 6000;

    /// <summary>
    ///     The monster template key used for the Tier IV consecrated ground hazard
    /// </summary>
    public string ConsecratedGroundTemplateKey { get; init; } = "elemental_hazard";

    /// <summary>
    ///     The filter used to determine which nearby creatures are hit
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     Sound played at the caster's position on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
