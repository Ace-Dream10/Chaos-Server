#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Blinds the subject, preventing it from attacking or casting. It can still move. Handled by
///     <see cref="Chaos.Scripting.MonsterScripts.AttackingScript" /> and
///     <see cref="Chaos.Scripting.MonsterScripts.CastingScript" />, which check for the "blackout" tag this effect
///     sets.
/// </summary>
public sealed class BlackoutEffect : EffectBase
{
    private const string BlackoutTag = "blackout";

    /// <summary>
    ///     How often the visual is re-played while active - per playtest feedback ("Blackout needs a visible
    ///     effect on the target, which is currently missing"), the original apply-once Animate() call faded well
    ///     before this 2-second effect actually ended. Same fix shape as Mark of the Bane/Death Mark/Marked for
    ///     Valhalla/Shadowmark.
    /// </summary>
    private static readonly TimeSpan AnimationRefreshInterval = TimeSpan.FromMilliseconds(500);

    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 133,
        AnimationSpeed = 100
    };

    private TimeSpan SinceLastAnimation;

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(2000);

    /// <inheritdoc />
    public override byte Icon => 55;

    /// <inheritdoc />
    public override string Name => "Blackout";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[BlackoutTag] = bool.TrueString;
        Subject.Animate(ApplyAnimation, Source.Id);
        SinceLastAnimation = TimeSpan.Zero;
    }

    /// <inheritdoc />
    public override void OnTerminated()
    {
        Subject.Trackers.Tags.TryRemove(BlackoutTag, out _);
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
