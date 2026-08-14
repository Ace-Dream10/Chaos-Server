#region
using Chaos.Collections;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     A rapid series of forward strikes on the tile directly in front of the caster. Unlike Cyclone, facing never
///     changes and the caster isn't locked in place - it's a fast fencing combo, not a channel.
/// </summary>
/// <remarks>
///     One of Slayer's 5 evolving abilities - tier scales with the skill's own level, using the same level-bracket
///     convention <see cref="BastionsChargeScript" /> established (1-2/3-4/5-6/7+ &#8594; tier I-IV). Per the
///     locked design's evolution note ("more slashes each evolution, faster execution, better Execution
///     generation"): hit count and speed scale per tier, and - fixing a real gap found during Slayer's verification
///     pass, not carried over from before - each hit now actually applies a Severance stack (it previously dealt
///     pure damage with no stack generation at all, which didn't match "better Execution generation"). Stacks
///     applied per hit also scale with tier.
/// </remarks>
public class FlourishScript : ConfigurableSkillScriptBase
{
    private const string SeveranceTargetTag = "severanceTarget";
    private const string SeveredTag = "severed";
    private const int MpPerStack = 20;

    private readonly List<PendingHit> PendingHits = [];

    /// <inheritdoc />
    public FlourishScript(Skill subject)
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

        ExecuteHit(source, map, tier.StacksPerHit);

        for (var i = 1; i < tier.HitCount; i++)
            PendingHits.Add(new PendingHit(source, map, tier.StacksPerHit, TimeSpan.FromMilliseconds(tier.HitIntervalMs * i)));
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested.
    /// </summary>
    private (int HitCount, int HitIntervalMs, int StacksPerHit) GetTierValues() =>
        Subject.Level switch
        {
            <= 2 => (5, 150, 1),
            <= 4 => (6, 135, 1),
            <= 6 => (7, 120, 2),
            _    => (8, 100, 2)
        };

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (PendingHits.Count == 0)
            return;

        foreach (var pending in PendingHits)
            pending.Remaining -= delta;

        while ((PendingHits.Count > 0) && (PendingHits[0].Remaining <= TimeSpan.Zero))
        {
            var pending = PendingHits[0];
            PendingHits.RemoveAt(0);

            if (pending.Source.IsAlive)
                ExecuteHit(pending.Source, pending.Map, pending.StacksPerHit);
        }
    }

    private void ExecuteHit(Creature source, MapInstance map, int stacksPerHit)
    {
        var targetPoint = source.DirectionalOffset(source.Direction);
        var target = map.GetEntitiesAtPoints<Creature>(targetPoint).TopOrDefault();

        if ((target == null) || !Filter.IsValidTarget(source, target))
            return;

        var damage = (BaseDamage ?? 0)
                     + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.STR) * (DamageStatMultiplier ?? 1));

        if (damage > 0)
            ApplyDamageScript.ApplyDamage(source, target, this, damage);

        //Better Execution generation per tier - each Flourish hit now applies a Severance stack, scaling with tier.
        //Also keeps the caster's MP-bar-as-stack-visual in sync every hit (see SeveranceTargetSync) - previously
        //Flourish applied real stacks without ever touching that display, so a player relying on Flourish to
        //build stacks saw no visible feedback at all and reasonably assumed stacking was broken.
        var flourishAisling = source as Aisling;
        SeveranceTargetSync.SwitchTargetIfNeeded(flourishAisling, map, target, SeveranceTargetTag, SeveranceEffect.StacksTag, SeveredTag);

        target.Effects.Apply(source, new SeveranceEffect { StacksToApply = stacksPerHit }, this);

        SeveranceTargetSync.SyncMpToStacks(flourishAisling, target, SeveranceEffect.StacksTag, MpPerStack);

        if (Animation != null)
            target.Animate(Animation, source.Id);
    }

    private sealed class PendingHit(Creature source, MapInstance map, int stacksPerHit, TimeSpan remaining)
    {
        public MapInstance Map { get; } = map;
        public TimeSpan Remaining { get; set; } = remaining;
        public Creature Source { get; } = source;
        public int StacksPerHit { get; } = stacksPerHit;
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the target on each hit
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
    ///     The filter used to determine whether the tile directly in front of the caster holds a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     Sound played once, on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
