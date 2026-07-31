#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

public sealed class FocusEffect : EffectBase
{
    private const int DmgBonus = 25;
    private const int HitBonus = 25;

    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 14,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(8000);

    /// <inheritdoc />
    public override byte Icon => 54;

    /// <inheritdoc />
    public override string Name => "Focus";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.StatSheet.AddBonus(
            new Attributes
            {
                Dmg = DmgBonus,
                Hit = HitBonus
            });

        Subject.Animate(ApplyAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated()
        => Subject.StatSheet.SubtractBonus(
            new Attributes
            {
                Dmg = DmgBonus,
                Hit = HitBonus
            });
}
