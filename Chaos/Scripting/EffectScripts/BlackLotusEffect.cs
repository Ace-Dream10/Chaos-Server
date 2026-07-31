#region
using Chaos.Collections;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Time;
using Chaos.Time.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     A strong poison DoT. Its own ticks can never land the killing blow (same convention as Poison/Poison Bomb),
///     but if the target dies from any OTHER source while tagged, <see cref="TriggerChainExplosion" /> - called
///     directly from <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" /> at the
///     moment of death, before the corpse is removed from the map - detonates for damage scaled off the ticks that
///     would have remained, and chains a fresh Black Lotus onto every hostile monster within range of the death
///     point.
/// </summary>
public sealed class BlackLotusEffect : IntervalEffectBase
{
    public const string BlackLotusTag = "black_lotus";
    private const int ExplosionRange = 2;
    private const int TickDamage = 40;
    private const int TickIntervalMs = 1000;

    private static readonly Animation ExplosionAnimation = new()
    {
        TargetAnimation = 374,
        AnimationSpeed = 100
    };

    private static readonly Animation TickAnimation = new()
    {
        TargetAnimation = 46,
        AnimationSpeed = 100
    };

    private readonly IApplyDamageScript ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(8000);

    /// <inheritdoc />
    protected override IIntervalTimer Interval { get; } = new IntervalTimer(TimeSpan.FromMilliseconds(TickIntervalMs));

    /// <inheritdoc />
    public override byte Icon => 59;

    /// <inheritdoc />
    public override string Name => "Black Lotus";

    /// <inheritdoc />
    public override void OnApplied() => Subject.Trackers.Tags[BlackLotusTag] = bool.TrueString;

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(BlackLotusTag, out _);

    /// <inheritdoc />
    protected override void OnIntervalElapsed()
    {
        if (!Subject.IsAlive)
            return;

        //never let the DoT tick itself land the killing blow - the chain only fires when something else finishes
        //the target off while it's still infected
        if (Subject.StatSheet.CurrentHp <= TickDamage)
            return;

        if (Subject.StatSheet.TrySubtractHp(TickDamage))
            Subject.ShowHealth();

        Subject.Animate(TickAnimation, Source.Id);
    }

    /// <summary>
    ///     Called from the damage pipeline the instant a black-lotus-tagged monster dies. Looks up the live effect
    ///     instance attached to it (so the explosion damage can scale off its own remaining ticks) and detonates.
    /// </summary>
    public static void TriggerChainExplosion(Monster monster)
    {
        if (!monster.Effects.TryGetEffect("Black Lotus", out var rawEffect) || (rawEffect is not BlackLotusEffect effect))
            return;

        effect.Explode(monster);
    }

    private void Explode(Monster monster)
    {
        var map = monster.MapInstance;
        var deathPoint = Point.From(monster);

        var remainingTicks = Math.Max(1, (int)Math.Ceiling(Remaining.TotalMilliseconds / TickIntervalMs));
        var explosionDamage = remainingTicks * TickDamage;

        map.ShowAnimation(ExplosionAnimation.GetPointAnimation(deathPoint, Source.Id));

        foreach (var nearby in map.GetEntitiesWithinRange<Monster>(deathPoint, ExplosionRange))
        {
            if (nearby.Equals(monster) || !nearby.IsAlive)
                continue;

            ApplyDamageScript.ApplyDamage(Source, nearby, this, explosionDamage);

            if (nearby.IsAlive)
            {
                var chained = new BlackLotusEffect();
                nearby.Effects.Apply(Source, chained, SourceScript);
            }
        }
    }
}
