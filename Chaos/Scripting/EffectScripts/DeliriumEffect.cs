#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Confuses the subject, causing it to attack random nearby creatures instead of its usual target. Handled by
///     <see cref="Chaos.Scripting.MonsterScripts.AggroTargetingScript" />, which checks for the "delirium" tag this
///     effect sets.
/// </summary>
public sealed class DeliriumEffect : EffectBase
{
    private const string DeliriumTag = "delirium";

    /// <summary>
    ///     How often the visual is re-played while active - same apply-once-fades-too-early fix as Blackout/Mark
    ///     of the Bane/Death Mark/Marked for Valhalla/Shadowmark, per the same playtest report.
    /// </summary>
    private static readonly TimeSpan AnimationRefreshInterval = TimeSpan.FromMilliseconds(500);

    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 46,
        AnimationSpeed = 100
    };

    private TimeSpan SinceLastAnimation;

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(2000);

    /// <inheritdoc />
    public override byte Icon => 54;

    /// <inheritdoc />
    public override string Name => "Delirium";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[DeliriumTag] = bool.TrueString;
        Subject.Animate(ApplyAnimation, Source.Id);
        SinceLastAnimation = TimeSpan.Zero;
    }

    /// <inheritdoc />
    public override void OnTerminated()
    {
        Subject.Trackers.Tags.TryRemove(DeliriumTag, out _);
        TricksterAfflictions.TryChainReact(Subject, Source, Name, SourceScript);
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        base.Update(delta);

        if (!Subject.IsAlive)
            return;

        SinceLastAnimation += delta;

        if (SinceLastAnimation < AnimationRefreshInterval)
            return;

        SinceLastAnimation = TimeSpan.Zero;
        Subject.Animate(ApplyAnimation, Source.Id);
    }
}
