#region
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

public sealed class RootEffect : EffectBase
{
    private const string RootTag = "rooted";

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(1500);

    /// <inheritdoc />
    public override byte Icon => 43;

    /// <inheritdoc />
    public override string Name => "Root";

    /// <inheritdoc />
    public override void OnApplied() => Subject.Trackers.Tags[RootTag] = bool.TrueString;

    /// <inheritdoc />
    public override void OnTerminated()
    {
        Subject.Trackers.Tags.TryRemove(RootTag, out _);
        TricksterAfflictions.TryChainReact(Subject, Source, Name, SourceScript);
    }
}
