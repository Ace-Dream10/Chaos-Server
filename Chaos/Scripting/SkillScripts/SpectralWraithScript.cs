#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     One of Assassin's 5 evolving abilities, and a direct build - nothing existing matched "become a living
///     shadow, striking multiple enemies before materializing at the final target". Implemented as: the closest
///     <see cref="GetMaxTargets" /> hostile monsters within <see cref="Range" /> are struck in order of proximity,
///     then the caster warps to behind whichever one was struck last (the same "behind the target" placement
///     <see cref="DeathsStrikeScript" /> uses). Evolves purely in target count per the locked design ("2 targets
///     -&gt; 4 targets -&gt; 8 targets -&gt; entire screen"), mapped to Assassin's own floor arc (Floor7 intro,
///     Floor8, Floor9, Floor10 max/finale) via the same "Level ≈ 2×Floor" ratio used throughout tonight - "entire
///     screen" at Tier IV is approximated as int.MaxValue targets combined with a much larger scan range rather
///     than a true full-map/viewport query, a simplification flagged here rather than silently assumed. All
///     placeholder values, not balance-tested.
/// </summary>
public class SpectralWraithScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public SpectralWraithScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var tier = GetTierValues();

        source.AnimateBody(BodyAnimation);

        var perHitDamage = (BaseDamage ?? 0) + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.DEX) * (DamageStatMultiplier ?? 1));

        var targets = map.GetEntitiesWithinRange<Monster>(context.SourcePoint, tier.ScanRange)
                         .Where(monster => Filter.IsValidTarget(source, monster))
                         .OrderBy(monster => context.SourcePoint.ManhattanDistanceFrom(monster))
                         .Take(tier.MaxTargets)
                         .ToList();

        Creature? lastStruck = null;

        foreach (var target in targets)
        {
            if (perHitDamage > 0)
                ApplyDamageScript.ApplyDamage(source, target, this, perHitDamage);

            if (Animation != null)
                target.Animate(Animation, source.Id);

            lastStruck = target;
        }

        if (lastStruck == null)
        {
            context.SourceAisling?.SendOrangeBarMessage("No targets found.");

            return;
        }

        //materialize behind the final target struck
        var behindDirection = lastStruck.DirectionalRelationTo(context.SourcePoint);

        foreach (var direction in behindDirection.AsEnumerable())
        {
            var destinationPoint = lastStruck.DirectionalOffset(direction);

            if (!map.IsWalkable(destinationPoint, source, false))
                continue;

            source.WarpTo(destinationPoint);
            source.Turn(lastStruck.DirectionalRelationTo(source));

            break;
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor7(Level&lt;=14)=I(intro,2 targets),
    ///     Floor8(&lt;=16)=II(4), Floor9(&lt;=18)=III(8), Floor10+(&gt;18)=IV(max/finale, entire screen).
    /// </summary>
    private (int MaxTargets, int ScanRange) GetTierValues() =>
        Subject.Level switch
        {
            <= 14 => (2, Range),
            <= 16 => (4, Range),
            <= 18 => (8, Range),
            _     => (int.MaxValue, ScreenRange)
        };

    #region ScriptVars
    /// <summary>
    ///     The animation played on each struck target
    /// </summary>
    public Animation? Animation { get; init; }

    public IApplyDamageScript ApplyDamageScript { get; init; }

    /// <summary>
    ///     The flat portion of each individual hit's damage
    /// </summary>
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster when the skill is used
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The stat used to scale each hit's bonus damage
    /// </summary>
    public Stat? DamageStat { get; init; }

    /// <summary>
    ///     The multiplier applied to <see cref="DamageStat" /> when calculating each hit's bonus damage
    /// </summary>
    public decimal? DamageStatMultiplier { get; init; }

    /// <summary>
    ///     The filter used to determine which nearby monsters are valid targets
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The scan range, in tiles, used at Tiers I-III
    /// </summary>
    public int Range { get; init; } = 5;

    /// <summary>
    ///     The much larger scan range used at Tier IV to approximate "entire screen"
    /// </summary>
    public int ScreenRange { get; init; } = 12;

    /// <summary>
    ///     Sound played on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
