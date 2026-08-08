#region
using Chaos.Extensions;
using Chaos.Geometry;
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
///     While active, whenever the caster takes damage, the attacker (and anything hostile near the attacker)
///     catches a fire explosion. Same periodic HP-snapshot-scan detection as
///     <see cref="MoltenArmorEffect" />, but the payload is an AoE burst around the attacker instead of a reflect
///     straight back at them.
/// </summary>
public sealed class BackdraftEffect : IntervalEffectBase
{
    private const int ExplosionDamage = 60;
    private const int ExplosionRange = 1;

    private static readonly Animation ExplosionAnimation = new()
    {
        TargetAnimation = 50,
        AnimationSpeed = 100
    };

    private readonly IApplyDamageScript ApplyDamageScript = ApplyAttackDamageScript.Create();
    private int LastKnownHp;

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(10000);

    /// <inheritdoc />
    protected override IIntervalTimer Interval { get; } = new IntervalTimer(TimeSpan.FromMilliseconds(100), false);

    /// <inheritdoc />
    public override byte Icon => 65;

    /// <inheritdoc />
    public override string Name => "Backdraft";

    /// <inheritdoc />
    public override void OnApplied() => LastKnownHp = Subject.StatSheet.CurrentHp;

    /// <inheritdoc />
    protected override void OnIntervalElapsed()
    {
        if (!Subject.IsAlive)
            return;

        var currentHp = Subject.StatSheet.CurrentHp;
        var damageTaken = LastKnownHp - currentHp;
        LastKnownHp = currentHp;

        if (damageTaken <= 0)
            return;

        var attacker = Subject.Trackers.LastDamagedBy;

        if ((attacker == null) || !attacker.IsAlive || (attacker.Id == Subject.Id))
            return;

        var map = attacker.MapInstance;
        var point = Point.From(attacker);

        map.ShowAnimation(ExplosionAnimation.GetPointAnimation(point, Subject.Id));

        foreach (var nearby in map.GetEntitiesWithinRange<Creature>(point, ExplosionRange))
        {
            if (!nearby.IsAlive || !Subject.IsHostileTo(nearby))
                continue;

            ApplyDamageScript.ApplyDamage(Subject, nearby, this, ExplosionDamage);
        }
    }
}
