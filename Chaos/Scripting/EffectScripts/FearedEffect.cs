#region
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Applied by Intimidating Shout. While active, the subject loses its current target and stops re-acquiring one
///     (gated in <see cref="Chaos.Scripting.MonsterScripts.AggroTargetingScript" />), and actively flees directly
///     away from <see cref="EffectBase.Source" /> instead of wandering normally (gated in
///     <see cref="Chaos.Scripting.MonsterScripts.WanderingScript" />).
/// </summary>
public sealed class FearedEffect : EffectBase
{
    public const string FearedTag = "feared";

    private static readonly Animation FearAnimation = new()
    {
        TargetAnimation = 14,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(2000);

    /// <inheritdoc />
    public override byte Icon => 45;

    /// <inheritdoc />
    public override string Name => "Feared";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[FearedTag] = bool.TrueString;

        if (Subject is Monster monster)
        {
            monster.Target = null;
            monster.AggroList.Clear();
        }

        Subject.Animate(FearAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(FearedTag, out _);
}
