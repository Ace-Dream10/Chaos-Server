#region
using Chaos.Collections;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry.Abstractions;
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
///     Valkyrie's signature attack. Every hit marks its target with
///     <see cref="MarkedForValhallaEffect" /> - resolves the "defeating a MARKED enemy" ambiguity Chooser of the
///     Slain's own doc comment flags.
/// </summary>
/// <remarks>
///     One of Valkyrie's 6 evolving abilities - evolves through SHAPE, not just numbers, per the locked design
///     ("Single → Triple → Cone → Pillar of Light"): Tier I hits one target directly ahead; Tier II hits the same
///     tile 3 times (mirrors <see cref="DreamslashScript" />'s multi-hit shape); Tier III widens to a frontal cone
///     (<see cref="AoeShape.FrontalCone" />); Tier IV is a bigger, harder-hitting cone with a distinct "pillar of
///     light" animation/sound - the shape itself doesn't need a new geometry primitive at Tier IV, a stronger cone
///     sells the escalation. Tier scales with the skill's own level, same level-bracket convention
///     <see cref="BastionsChargeScript" /> established, re-derived for Heavenly Strike's own floor arc (Floor 4
///     intro, Floor 5, Floor 6, Floor 7 max): Floor4-5(Level&lt;=10)=I/II boundary... see <see cref="GetTierValues" />
///     for the exact per-tier breakdown.
/// </remarks>
public class HeavenlyStrikeScript : ConfigurableSkillScriptBase
{
    private readonly IApplyDamageScript ApplyDamageScript;

    /// <inheritdoc />
    public HeavenlyStrikeScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var tier = GetTierValues();

        source.AnimateBody(BodyAnimation);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, source);

        if (tier.Shape == AoeShape.None)
        {
            //Tier I/II: single tile directly ahead, hit tier.HitCount times
            var targetPoint = source.DirectionalOffset(source.Direction);
            var target = map.GetEntitiesAtPoints<Creature>(targetPoint).TopOrDefault();

            for (var i = 0; i < tier.HitCount; i++)
                StrikeIfPresent(source, target);
        } else
        {
            //Tier III/IV: frontal cone
            var options = new AoeShapeOptions
            {
                Source = source,
                Range = tier.Range,
                Direction = source.Direction
            };

            var points = AoeShape.FrontalCone.ResolvePoints(options);

            foreach (var point in points)
            {
                var target = map.GetEntitiesAtPoints<Creature>(point).TopOrDefault();
                StrikeIfPresent(source, target);
            }
        }

        void StrikeIfPresent(Creature attacker, Creature? target)
        {
            if ((target == null) || !Filter.IsValidTarget(attacker, target))
                return;

            var damage = tier.BaseDamage + Convert.ToInt32(attacker.StatSheet.GetEffectiveStat(DamageStat ?? Stat.STR) * tier.DamageStatMultiplier);

            if (damage > 0)
                ApplyDamageScript.ApplyDamage(attacker, target, this, damage);

            //Marks the target for Chooser of the Slain - see MarkedForValhallaEffect's doc comment for why this
            //exists (resolves the "defeating a MARKED enemy" ambiguity flagged when that passive was first built)
            target.Effects.Apply(attacker, new MarkedForValhallaEffect(), this);

            if (Animation != null)
                target.Animate(Animation, attacker.Id);
        }
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor4(Level&lt;=8)=I(intro,single), Floor5(&lt;=10)=II
    ///     (triple), Floor6(&lt;=12)=III(cone), Floor7+(&gt;12)=IV(max, bigger cone).
    /// </summary>
    private (int BaseDamage, decimal DamageStatMultiplier, int HitCount, AoeShape Shape, int Range) GetTierValues() =>
        Subject.Level switch
        {
            <= 8  => (70, 2.5m, 1, AoeShape.None, 0),
            <= 10 => (90, 2.75m, 3, AoeShape.None, 0),
            <= 12 => (130, 3m, 1, AoeShape.FrontalCone, 3),
            _     => (170, 3.5m, 1, AoeShape.FrontalCone, 4)
        };

    #region ScriptVars
    /// <summary>
    ///     The animation played on each struck target
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster when the skill is used
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The stat used to scale bonus damage
    /// </summary>
    public Stat? DamageStat { get; init; }

    /// <summary>
    ///     The filter used to determine whether a given tile holds a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     Sound played once, on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
