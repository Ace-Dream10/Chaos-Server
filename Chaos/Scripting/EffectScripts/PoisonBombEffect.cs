#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
using Chaos.Time;
using Chaos.Time.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

public sealed class PoisonBombEffect : IntervalEffectBase
{
    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(6000);

    /// <inheritdoc />
    protected override IIntervalTimer Interval { get; } = new IntervalTimer(TimeSpan.FromMilliseconds(1000));

    /// <summary>
    ///     The animation played on the target when the effect is applied. Set by the applying script before Apply()
    ///     is called, since EffectFactory.Create does not populate scriptVars onto effects the way Scripts do.
    /// </summary>
    public Animation? ApplyAnimation { get; set; }

    /// <summary>
    ///     The amount of damage dealt on each tick
    /// </summary>
    public int DamagePerTick { get; set; } = 30;

    /// <inheritdoc />
    public override byte Icon => 53;

    /// <inheritdoc />
    public override string Name => "Poison Bomb";

    /// <inheritdoc />
    public override void OnApplied()
    {
        if (ApplyAnimation != null)
            Subject.Animate(ApplyAnimation, Source.Id);
    }

    /// <inheritdoc />
    protected override void OnIntervalElapsed()
    {
        if (!Subject.IsAlive)
            return;

        //same safety behavior as PoisonEffect - the DoT never lands the killing blow
        if (Subject.StatSheet.CurrentHp <= DamagePerTick)
            return;

        if (Subject.StatSheet.TrySubtractHp(DamagePerTick))
            Subject.ShowHealth();
    }
}
