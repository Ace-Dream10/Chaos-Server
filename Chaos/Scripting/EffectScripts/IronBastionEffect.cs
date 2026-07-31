#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

public sealed class IronBastionEffect : EffectBase
{
    private const int AcBonus = -15;

    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 24,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(8000);

    /// <inheritdoc />
    public override byte Icon => 51;

    /// <inheritdoc />
    public override string Name => "Iron Bastion";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.StatSheet.AddBonus(new Attributes { Ac = AcBonus });
        Subject.Animate(ApplyAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.StatSheet.SubtractBonus(new Attributes { Ac = AcBonus });
}
