#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Puts a monster to sleep - tags it "asleep", which AttackingScript, CastingScript, MoveToTargetScript, and
///     WanderingScript all check to stay completely idle and silent. Taking any damage wakes the monster
///     immediately (see the Monster case in ApplyAttackDamageScript, which terminates this effect on hit).
/// </summary>
public sealed class LullabyEffect : EffectBase
{
    public const string AsleepTag = "asleep";

    private static readonly Animation SleepAnimation = new()
    {
        TargetAnimation = 14,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(5000);

    /// <inheritdoc />
    public override byte Icon => 60;

    /// <inheritdoc />
    public override string Name => "Lullaby";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[AsleepTag] = bool.TrueString;
        Subject.Animate(SleepAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(AsleepTag, out _);
}
