#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Sandblasts the target's armor, worsening their AC for the duration.
/// </summary>
public sealed class SandVeilEffect : EffectBase
{
    private const int AcPenalty = 12;

    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 60,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(5000);

    /// <inheritdoc />
    public override byte Icon => 67;

    /// <inheritdoc />
    public override string Name => "Sand Veil";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.StatSheet.AddBonus(new Attributes { Ac = AcPenalty });
        Subject.Animate(ApplyAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.StatSheet.SubtractBonus(new Attributes { Ac = AcPenalty });
}
