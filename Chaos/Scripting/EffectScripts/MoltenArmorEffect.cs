#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Time;
using Chaos.Time.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Reflects a flat percentage of incoming damage back at the attacker, plus a CON boost, for the duration.
///     Uses the same periodic HP-snapshot scan as <see cref="LancersRetributionEffect" /> rather than a true
///     pre-damage intercept, since effects have no hook into their own subject being attacked.
/// </summary>
public sealed class MoltenArmorEffect : IntervalEffectBase
{
    private const int ConBonus = 15;
    private const decimal ReflectPct = 0.3m;

    private static readonly Animation ReflectAnimation = new()
    {
        TargetAnimation = 50,
        AnimationSpeed = 100
    };

    private readonly IApplyDamageScript ApplyDamageScript = ApplyAttackDamageScript.Create();
    private int LastKnownHp;

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(15000);

    /// <inheritdoc />
    protected override IIntervalTimer Interval { get; } = new IntervalTimer(TimeSpan.FromMilliseconds(100), false);

    /// <inheritdoc />
    public override byte Icon => 64;

    /// <inheritdoc />
    public override string Name => "Molten Armor";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.StatSheet.AddBonus(new Attributes { Con = ConBonus });
        LastKnownHp = Subject.StatSheet.CurrentHp;
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.StatSheet.SubtractBonus(new Attributes { Con = ConBonus });

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

        var reflectDamage = Convert.ToInt32(damageTaken * ReflectPct);

        if (reflectDamage <= 0)
            return;

        ApplyDamageScript.ApplyDamage(Subject, attacker, this, reflectDamage);
        attacker.Animate(ReflectAnimation, Subject.Id);
    }
}
