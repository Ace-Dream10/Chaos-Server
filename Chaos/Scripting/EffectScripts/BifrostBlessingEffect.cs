#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Bifrost Step's Tier IV reward for crossing: the locked design calls for "a movement speed buff after
///     crossing", but this engine has no movement-speed stat or mechanic anywhere (confirmed via a real search -
///     movement here is tile-based/turn-based, not a continuous speed value). Substituted with a brief flat damage
///     buff instead ("blessed swiftness" flavor-wise), a deliberate, documented simplification rather than
///     inventing new movement-speed infrastructure for one Tier IV flourish on one ability - flagged for review,
///     not silently guessed.
/// </summary>
public sealed class BifrostBlessingEffect : EffectBase
{
    private const int DamageBonus = 15;

    private static readonly Animation BlessAnimation = new()
    {
        TargetAnimation = 70,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(5);

    /// <inheritdoc />
    public override byte Icon => 63;

    /// <inheritdoc />
    public override string Name => "Bifrost Blessing";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.StatSheet.AddBonus(new Attributes { Dmg = DamageBonus });
        Subject.Animate(BlessAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.StatSheet.SubtractBonus(new Attributes { Dmg = DamageBonus });
}
