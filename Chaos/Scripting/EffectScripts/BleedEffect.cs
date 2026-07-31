#region
using Chaos.Scripting.EffectScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Time;
using Chaos.Time.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     A physical damage-over-time debuff applied by Garrote and Venom Blade. Unlike Poison/Poison Bomb, each tick
///     goes through the full <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" />
///     pipeline (per Garrote's design), so bleed damage is capable of finishing off a target. Tags the target
///     "bleeding" for the duration - checked by Eviscerate for its bonus damage.
/// </summary>
public sealed class BleedEffect : IntervalEffectBase
{
    private const string BleedingTag = "bleeding";

    private readonly IApplyDamageScript ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(5000);

    /// <inheritdoc />
    protected override IIntervalTimer Interval { get; } = new IntervalTimer(TimeSpan.FromMilliseconds(1000));

    /// <summary>
    ///     The amount of damage dealt on each tick. Set by the applying script before Apply() is called, since
    ///     EffectFactory.Create does not populate scriptVars onto effects the way Scripts do.
    /// </summary>
    public int BleedDamage { get; set; } = 30;

    /// <inheritdoc />
    public override byte Icon => 57;

    /// <inheritdoc />
    public override string Name => "Bleed";

    /// <inheritdoc />
    public override void OnApplied() => Subject.Trackers.Tags[BleedingTag] = bool.TrueString;

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(BleedingTag, out _);

    /// <inheritdoc />
    protected override void OnIntervalElapsed()
    {
        if (!Subject.IsAlive)
            return;

        ApplyDamageScript.ApplyDamage(Source, Subject, this, BleedDamage);
    }
}
