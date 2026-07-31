#region
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
///     Reflects a portion of incoming damage back at the attacker, scaled by the Lancer's AC at the time the effect
///     was applied. Implemented via a periodic HP-snapshot scan (rather than a true pre-damage intercept) since
///     effects have no hook into their own subject being attacked.
/// </summary>
public sealed class LancersRetributionEffect : IntervalEffectBase
{
    private const decimal AcScaling = 0.005m;

    private static readonly Animation ReflectAnimation = new()
    {
        TargetAnimation = 24,
        AnimationSpeed = 100
    };

    private readonly IApplyDamageScript ApplyDamageScript = ApplyAttackDamageScript.Create();
    private int LastKnownHp;
    private int StoredAc;

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(10000);

    /// <inheritdoc />
    protected override IIntervalTimer Interval { get; } = new IntervalTimer(TimeSpan.FromMilliseconds(100), false);

    /// <inheritdoc />
    public override byte Icon => 53;

    /// <inheritdoc />
    public override string Name => "Lancer's Retribution";

    /// <inheritdoc />
    public override void OnApplied()
    {
        StoredAc = Subject.StatSheet.EffectiveAc;
        LastKnownHp = Subject.StatSheet.CurrentHp;
    }

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

        var reflectDamage = Math.Max(0, Convert.ToInt32(damageTaken * (StoredAc * AcScaling)));

        if (reflectDamage <= 0)
            return;

        ApplyDamageScript.ApplyDamage(Subject, attacker, this, reflectDamage);
        attacker.Animate(ReflectAnimation, Subject.Id);
    }
}
