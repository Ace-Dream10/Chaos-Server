#region
using Chaos.Models.Data;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Time;
using Chaos.Time.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Fire's core status (per the locked Sorcerer design), stacking up to <see cref="MaxStacks" /> like
///     <see cref="SeveranceEffect" /> - each application adds a stack and refreshes duration, and each tick deals
///     damage scaled by current stack count. Applied by Kindling's chance-on-hit and directly by abilities like
///     Fire Wall; consumed entirely by Combustion for a burst; checked by Scorch for bonus damage.
/// </summary>
public sealed class BurnEffect : IntervalEffectBase
{
    /// <summary>
    ///     Public so Kindling/Scorch/Combustion (stateless hooks in
    ///     <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" />, and
    ///     <see cref="Chaos.Scripting.SpellScripts.CombustionScript" />) can read/consume the current stack count
    ///     directly, the same way <see cref="SeveranceEffect.StacksTag" /> is already read externally.
    /// </summary>
    public const string StacksTag = "burn_stacks";

    private const int MaxStacks = 5;

    private readonly IApplyDamageScript ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(10);

    /// <inheritdoc />
    protected override IIntervalTimer Interval { get; } = new IntervalTimer(TimeSpan.FromMilliseconds(1000));

    /// <inheritdoc />
    public override byte Icon => 60;

    /// <inheritdoc />
    public override string Name => "Burn";

    /// <summary>
    ///     The damage dealt per stack, per tick. Set by the applying script before Apply() is called, the same way
    ///     <see cref="BleedEffect.BleedDamage" /> is set.
    /// </summary>
    public int DamagePerStackPerTick { get; set; } = 5;

    /// <summary>
    ///     The number of stacks this particular application adds
    /// </summary>
    public int StacksToApply { get; init; } = 1;

    /// <inheritdoc />
    public override void OnApplied()
    {
        var currentStacks = 0;

        if (Subject.Trackers.Tags.TryGetValue(StacksTag, out var stacksStr))
            int.TryParse(stacksStr, out currentStacks);

        currentStacks = Math.Min(MaxStacks, currentStacks + StacksToApply);
        Subject.Trackers.Tags[StacksTag] = currentStacks.ToString();
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(StacksTag, out _);

    /// <inheritdoc />
    protected override void OnIntervalElapsed()
    {
        if (!Subject.IsAlive)
            return;

        if (!Subject.Trackers.Tags.TryGetValue(StacksTag, out var stacksStr) || !int.TryParse(stacksStr, out var stacks) || (stacks <= 0))
            return;

        ApplyDamageScript.ApplyDamage(Source, Subject, this, stacks * DamagePerStackPerTick);
    }

    /// <summary>
    ///     Always allow reapplication - each new stack replaces this effect instance with a fresh one, which is how
    ///     stacking and duration-refresh both happen for free. Mirrors <see cref="SeveranceEffect.ShouldApply" />.
    /// </summary>
    public override bool ShouldApply(Creature source, Creature target) => true;
}
