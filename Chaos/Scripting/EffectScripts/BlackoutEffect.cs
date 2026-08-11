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

    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 133,
        AnimationSpeed = 100
    };

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
    }

    /// <inheritdoc />
    public override void OnTerminated()
    {
        Subject.Trackers.Tags.TryRemove(BlackoutTag, out _);
        TricksterAfflictions.TryChainReact(Subject, Source, Name, SourceScript);
    }
}
