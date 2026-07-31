#region
using Chaos.Models.Data;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     A stacking debuff applied to monsters by Slayer skills. Each application adds stacks (up to 5) and refreshes
///     the duration. At 5 stacks, the monster is "severed" - vulnerable to Scythe's finishing damage. Reapplication is
///     always allowed (unlike most effects), since that's how stacks build.
/// </summary>
public sealed class SeveranceEffect : EffectBase
{
    private const string StacksTag = "severance_stacks";
    private const string SeveredTag = "severed";
    private const int MaxStacks = 5;

    private static readonly Animation StackAnimation = new()
    {
        TargetAnimation = 90,
        AnimationSpeed = 100
    };

    //reuses the same "vulnerable to finisher" visual language as ExecuteIndicatorScript
    private static readonly Animation SeveredAnimation = new()
    {
        TargetAnimation = 374,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(30);

    /// <inheritdoc />
    public override byte Icon => 56;

    /// <inheritdoc />
    public override string Name => "Severance";

    /// <summary>
    ///     The number of stacks this particular application adds
    /// </summary>
    public int StacksToApply { get; init; } = 1;

    /// <inheritdoc />
    public override void OnApplied()
    {
        var currentStacks = 0;

        if (Subject.Trackers.Tags.TryGetValue(StacksTag, out var stacksStr))
            int.TryParse(stacksStr, out currentStacks);

        currentStacks = Math.Min(MaxStacks, currentStacks + StacksToApply);
        Subject.Trackers.Tags[StacksTag] = currentStacks.ToString();

        if (currentStacks >= MaxStacks)
        {
            Subject.Trackers.Tags[SeveredTag] = bool.TrueString;
            Subject.Animate(SeveredAnimation, Source.Id);
        } else
            Subject.Animate(StackAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated()
    {
        Subject.Trackers.Tags.TryRemove(StacksTag, out _);
        Subject.Trackers.Tags.TryRemove(SeveredTag, out _);
    }

    /// <summary>
    ///     Always allow reapplication - each new stack replaces this effect instance with a fresh one, which is how
    ///     stacking and duration-refresh both happen for free
    /// </summary>
    public override bool ShouldApply(Creature source, Creature target) => true;
}
