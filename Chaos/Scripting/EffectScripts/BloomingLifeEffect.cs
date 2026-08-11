#region
using Chaos.Models.Data;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyHealing;
using Chaos.Time;
using Chaos.Time.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     One of Mystic's 5 evolving abilities - a tiered heal-over-time, direct build replacing the flat "regrowth"
///     spell/RegenerationEffect it grew out of (renamed to Blooming Life, see BloomingLifeScript's doc comment for
///     the tier interpolation). Ticks once per second for the configured duration, healing for
///     <see cref="HealPerTick" /> each tick. Tracks how many instances of itself a given caster currently has active
///     (across all their targets) via a counter on the caster's own <c>Trackers.Counters</c>, so that Bloomkeeper
///     (the Mystic passive) can read "how many Blooming Lifes are currently out" directly off the caster without a
///     separate global registry.
/// </summary>
public sealed class BloomingLifeEffect : IntervalEffectBase
{
    /// <summary>
    ///     The Trackers.Counters key, on the CASTER, tracking how many Blooming Life effects they currently have
    ///     active across all targets - read by Bloomkeeper in ApplyHealScript
    /// </summary>
    public const string ActiveCountCounterKey = "bloomingLifeActiveCount";

    private readonly IApplyHealScript ApplyHealScript = FunctionalScripts.ApplyHealing.ApplyHealScript.Create();

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(6);

    /// <inheritdoc />
    protected override IIntervalTimer Interval { get; } = new IntervalTimer(TimeSpan.FromSeconds(1));

    /// <summary>
    ///     The creature that cast Blooming Life - used as the healing source (so Bloomkeeper/Spirit Overflow
    ///     attribute correctly) rather than the effect's target healing itself
    /// </summary>
    public Creature? Caster { get; set; }

    /// <summary>
    ///     The amount healed on each tick
    /// </summary>
    public int HealPerTick { get; set; }

    /// <inheritdoc />
    public override byte Icon => 146;

    /// <inheritdoc />
    public override string Name => "Blooming Life";

    /// <inheritdoc />
    public override void OnApplied() => Caster?.Trackers.Counters.AddOrIncrement(ActiveCountCounterKey);

    /// <inheritdoc />
    public override void OnTerminated() => Caster?.Trackers.Counters.TryDecrement(ActiveCountCounterKey, out _);

    /// <inheritdoc />
    protected override void OnIntervalElapsed()
    {
        if ((Caster == null) || (HealPerTick <= 0))
            return;

        ApplyHealScript.ApplyHeal(Caster, Subject, this, HealPerTick);
    }
}
