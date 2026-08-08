#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SkillScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     One of Bastion's 5 evolving abilities (tier scales with the skill's own level, using the same
///     level-bracket convention <see cref="CycloneScript" />/<see cref="BerserkerGateScript" /> already
///     established: 1-2/3-4/5-6/7+ &#8594; tier I-IV). The base mass-aggro pull (every hostile on the map, matching
///     the design table's "Mass aggro" role) is unchanged across tiers - see <see cref="GetTierValues" />'s doc
///     comment for why "larger radius" from the design's evolution notes doesn't apply here. Tier II+ adds a
///     self "damage reduction while active" buff; tier III+ adds "brief taunt immunity"; tier IV additionally
///     applies the AoE attack-speed slow absorbed from the retired standalone "Weakening Shout" ability to every
///     monster pulled, per the locked design.
/// </summary>
public class ChallengingShoutScript : ConfigurableSkillScriptBase
{
    /// <summary>
    ///     Animation.DurationMs is never transmitted over the wire (see AnimationConverter) - it's only used
    ///     server-side for animation-priority interpolation. To make the mob animation visually persist, it has
    ///     to be re-triggered on an interval instead, the same technique StasisEffect uses for its freeze visual.
    /// </summary>
    private static readonly TimeSpan MobAnimationRefreshInterval = TimeSpan.FromMilliseconds(500);

    private readonly IEffectFactory EffectFactory;
    private readonly List<PulseTarget> PulseTargets = [];

    /// <inheritdoc />
    public ChallengingShoutScript(Skill subject, IEffectFactory effectFactory)
        : base(subject)
        => EffectFactory = effectFactory;

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var tier = GetTierValues();

        source.AnimateBody(BodyAnimation);

        foreach (var monster in map.GetEntities<Monster>())
            if (Filter.IsValidTarget(source, monster))
            {
                monster.AggroList.AddAggro(source, 99999);

                if (tier.WeakeningSlowAmount > 0)
                {
                    var weakeningEffect = (WeakeningShoutEffect)EffectFactory.Create("WeakeningShout");
                    weakeningEffect.AtkSpeedPenalty = tier.WeakeningSlowAmount;
                    weakeningEffect.SetDuration(tier.WeakeningDuration);
                    monster.Effects.Apply(source, weakeningEffect, this);
                }

                if (MobAnimation != null)
                {
                    monster.Animate(MobAnimation, source.Id);

                    PulseTargets.Add(new PulseTarget(monster, source.Id, TimeSpan.FromMilliseconds(MobAnimationDurationMs)));
                }
            }

        if (tier.ResolveAcBonus != 0)
        {
            var resolveEffect = (ChallengingResolveEffect)EffectFactory.Create("ChallengingResolve");
            resolveEffect.AcBonus = tier.ResolveAcBonus;
            resolveEffect.GrantsTauntImmunity = tier.GrantsTauntImmunity;
            resolveEffect.SetDuration(tier.ResolveDuration);
            source.Effects.Apply(source, resolveEffect, this);
        }

        if (CasterAnimation != null)
            map.ShowAnimation(CasterAnimation.GetPointAnimation(context.SourcePoint, source.Id));

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. The base pull already hits every hostile on the map
    ///     (matching the design table's own "Mass aggro" description), so unlike Cyclone/Berserker Gate there's no
    ///     "larger radius" dial to turn - that evolution note from the design doc's prose doesn't have anything to
    ///     attach to given the ability's existing whole-map scope, so it's treated as already satisfied at tier I
    ///     rather than built out further. "Longer threat duration" is realized as longer self-buff/debuff
    ///     durations at higher tiers instead.
    /// </summary>
    private (int ResolveAcBonus, TimeSpan ResolveDuration, bool GrantsTauntImmunity, int WeakeningSlowAmount, TimeSpan WeakeningDuration) GetTierValues() =>
        Subject.Level switch
        {
            <= 2 => (0, TimeSpan.Zero, false, 0, TimeSpan.Zero),
            <= 4 => (-10, TimeSpan.FromSeconds(5), false, 0, TimeSpan.Zero),
            <= 6 => (-15, TimeSpan.FromSeconds(7), true, 0, TimeSpan.Zero),
            _    => (-20, TimeSpan.FromSeconds(10), true, 30, TimeSpan.FromSeconds(6))
        };

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (PulseTargets.Count == 0)
            return;

        for (var i = PulseTargets.Count - 1; i >= 0; i--)
        {
            var pulse = PulseTargets[i];
            pulse.Remaining -= delta;
            pulse.SinceLastPulse += delta;

            if (!pulse.Monster.IsAlive || (pulse.Remaining <= TimeSpan.Zero))
            {
                PulseTargets.RemoveAt(i);

                continue;
            }

            if ((pulse.SinceLastPulse >= MobAnimationRefreshInterval) && (MobAnimation != null))
            {
                pulse.SinceLastPulse = TimeSpan.Zero;
                pulse.Monster.Animate(MobAnimation, pulse.SourceId);
            }
        }
    }

    private sealed class PulseTarget(Monster monster, uint sourceId, TimeSpan remaining)
    {
        public Monster Monster { get; } = monster;
        public TimeSpan Remaining { get; set; } = remaining;
        public TimeSpan SinceLastPulse { get; set; } = TimeSpan.Zero;
        public uint SourceId { get; } = sourceId;
    }

    #region ScriptVars
    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The pulse animation played centered on the caster's point
    /// </summary>
    public Animation? CasterAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine which creatures on the map are valid aggro targets
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The animation played on each affected monster
    /// </summary>
    public Animation? MobAnimation { get; init; }

    /// <summary>
    ///     How long, in milliseconds, the mob animation is re-triggered for on each affected monster
    /// </summary>
    public int MobAnimationDurationMs { get; init; } = 2500;

    /// <summary>
    ///     The sound played at the caster's position on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
