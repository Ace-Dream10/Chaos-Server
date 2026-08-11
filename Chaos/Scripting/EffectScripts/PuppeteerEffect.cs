#region
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Applied by Trickster's Puppeteer. While active, the target's own targeting is inverted -
///     <see cref="Chaos.Scripting.MonsterScripts.AggroTargetingScript" /> checks for <see cref="PuppeteeredTag" />
///     and, if present, seeks out other monsters instead of Aislings. Clears the monster's existing target/aggro on
///     both apply and terminate, so it immediately turns on nearby monsters and just as immediately drops that
///     grudge and re-aggros normally once the effect ends.
/// </summary>
public sealed class PuppeteerEffect : EffectBase
{
    public const string PuppeteeredTag = "puppeteered";

    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 46,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(8000);

    /// <inheritdoc />
    public override byte Icon => 75;

    /// <inheritdoc />
    public override string Name => "Puppeteer";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[PuppeteeredTag] = bool.TrueString;
        ResetTargeting();
        Subject.Animate(ApplyAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated()
    {
        Subject.Trackers.Tags.TryRemove(PuppeteeredTag, out _);
        ResetTargeting();
        TricksterAfflictions.TryChainReact(Subject, Source, Name, SourceScript);
    }

    private void ResetTargeting()
    {
        if (Subject is not Monster monster)
            return;

        monster.Target = null;
        monster.AggroList.Clear();
    }
}
