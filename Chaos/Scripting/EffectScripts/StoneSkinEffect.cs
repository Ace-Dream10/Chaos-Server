#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     A self-buff granting a chunk of AC and CON for the duration.
/// </summary>
public sealed class StoneSkinEffect : EffectBase
{
    private const int AcBonus = -15;
    private const int ConBonus = 10;

    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 35,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(30000);

    /// <inheritdoc />
    public override byte Icon => 60;

    /// <inheritdoc />
    public override string Name => "Stone Skin";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.StatSheet.AddBonus(
            new Attributes
            {
                Ac = AcBonus,
                Con = ConBonus
            });

        Subject.Animate(ApplyAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated()
        => Subject.StatSheet.SubtractBonus(
            new Attributes
            {
                Ac = AcBonus,
                Con = ConBonus
            });
}
