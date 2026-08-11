#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
using Chaos.Time;
using Chaos.Time.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Phantom Blade's lingering "shadow wound" damage-over-time. A plain interval DoT, same shape as
///     <see cref="PoisonEffect" />/<see cref="BleedEffect" /> - no chain/explosion behavior like Black Lotus had,
///     since nothing in the locked description asks for one. <see cref="TickDamage" />/duration are placeholders,
///     not balance-tested.
/// </summary>
public sealed class ShadowWoundEffect : IntervalEffectBase
{
    private const int TickIntervalMs = 1000;

    private static readonly Animation TickAnimation = new()
    {
        TargetAnimation = 35,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(6000);

    /// <inheritdoc />
    protected override IIntervalTimer Interval { get; } = new IntervalTimer(TimeSpan.FromMilliseconds(TickIntervalMs));

    /// <inheritdoc />
    public override byte Icon => 59;

    /// <inheritdoc />
    public override string Name => "Shadow Wound";

    /// <summary>
    ///     The damage dealt per tick
    /// </summary>
    public int TickDamage { get; init; } = 25;

    /// <inheritdoc />
    protected override void OnIntervalElapsed()
    {
        if (!Subject.IsAlive)
            return;

        //same "DoT ticks can never land the killing blow" convention as Poison/Black Lotus
        if (Subject.StatSheet.CurrentHp <= TickDamage)
            return;

        if (Subject.StatSheet.TrySubtractHp(TickDamage))
            Subject.ShowHealth();

        Subject.Animate(TickAnimation, Source.Id);
    }
}
