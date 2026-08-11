#region
using Chaos.Collections;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
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
///     A direct build - nothing existing matched "crack your enchanted whip in a wide arc, damaging enemies in
///     front of you and briefly staggering them." Uses the same <see cref="AoeShape.FrontalCone" /> pattern
///     Heavenly Strike/Shadow Reap established. The "briefly staggering" half reuses
///     <see cref="BlackoutEffect" /> directly (prevents attacking/casting) rather than inventing a parallel
///     "stagger" tag that would also require touching <c>AttackingScript</c>/<c>CastingScript</c> to recognize it -
///     a deliberate reuse decision, flagged here rather than silently assumed to be a distinct mechanic.
/// </summary>
/// <remarks>
///     One of Trickster's 5 evolving abilities. Tiers per the locked design ("wider arc and longer stagger
///     duration") mapped to Trickster's own floor arc (Floor1-2 intro/stall, Floor3, Floor4, Floor5 max) via the
///     same "Level ≈ 2×Floor" ratio used throughout tonight - see <see cref="GetTierValues" />. All placeholder
///     values, not balance-tested.
/// </remarks>
public class CrackTheWhipScript : ConfigurableSkillScriptBase
{
    private readonly IApplyDamageScript ApplyDamageScript;

    /// <inheritdoc />
    public CrackTheWhipScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var tier = GetTierValues();

        source.AnimateBody(BodyAnimation);

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

            if ((target == null) || !Filter.IsValidTarget(source, target))
                continue;

            if (tier.BaseDamage > 0)
                ApplyDamageScript.ApplyDamage(source, target, this, tier.BaseDamage);

            var staggerEffect = new BlackoutEffect();
            staggerEffect.SetDuration(TimeSpan.FromMilliseconds(tier.StaggerMs));
            target.Effects.Apply(source, staggerEffect, this);

            if (Animation != null)
                target.Animate(Animation, source.Id);
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor1-2(Level&lt;=4)=I/stall, Floor3(&lt;=6)=II(first
    ///     evolution), Floor4(&lt;=8)=III, Floor5+(&gt;8)=IV(max, widest arc, longest stagger).
    /// </summary>
    private (int BaseDamage, int Range, int StaggerMs) GetTierValues() =>
        Subject.Level switch
        {
            <= 4 => (40, 2, 1000),
            <= 6 => (55, 3, 1500),
            <= 8 => (70, 4, 2000),
            _    => (90, 5, 2500)
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
    ///     The filter used to determine whether a given tile holds a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     Sound played once, on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
