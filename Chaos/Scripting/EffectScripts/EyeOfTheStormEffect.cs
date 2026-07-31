#region
using Chaos.Extensions;
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
///     The calm center of destruction - the caster becomes fully invulnerable (same AC-floor trick as
///     <see cref="StasisEffect" />/<see cref="Chaos.Scripting.EffectScripts.AxeBlockEffect" />) while periodically
///     blasting every hostile nearby.
/// </summary>
public sealed class EyeOfTheStormEffect : IntervalEffectBase
{
    private const int DamagePerPulse = 40;
    private const int PulseRange = 2;

    /// <summary>
    ///     A bonus large enough that, once clamped, guarantees effective invulnerability regardless of the subject's
    ///     base AC
    /// </summary>
    private const int InvulnerabilityAcBonus = -10000;

    private static readonly Animation PulseAnimation = new()
    {
        TargetAnimation = 60,
        AnimationSpeed = 100
    };

    private readonly IApplyDamageScript ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(5000);

    /// <inheritdoc />
    protected override IIntervalTimer Interval { get; } = new IntervalTimer(TimeSpan.FromMilliseconds(500));

    /// <inheritdoc />
    public override byte Icon => 68;

    /// <inheritdoc />
    public override string Name => "Eye of the Storm";

    /// <inheritdoc />
    public override void OnApplied() => Subject.StatSheet.AddBonus(new Attributes { Ac = InvulnerabilityAcBonus });

    /// <inheritdoc />
    public override void OnTerminated() => Subject.StatSheet.SubtractBonus(new Attributes { Ac = InvulnerabilityAcBonus });

    /// <inheritdoc />
    protected override void OnIntervalElapsed()
    {
        if (!Subject.IsAlive)
            return;

        var map = Subject.MapInstance;
        var point = Point.From(Subject);

        map.ShowAnimation(PulseAnimation.GetPointAnimation(point, Subject.Id));

        foreach (var nearby in map.GetEntitiesWithinRange<Monster>(point, PulseRange))
        {
            if (!nearby.IsAlive || !Subject.IsHostileTo(nearby))
                continue;

            ApplyDamageScript.ApplyDamage(Subject, nearby, this, DamagePerPulse);
        }
    }
}
