#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     One of Fletcher's 5 evolving abilities. <see cref="DmgBonus" />/<see cref="HitBonus" /> are tier-scaled and
///     set by <see cref="Chaos.Scripting.SkillScripts.FletcherFocusScript" /> before applying, the same
///     "properties set by the skill script, not scriptVars" convention <see cref="BloodlustEffect" /> established.
///     Placeholder magnitudes, not balance-tested.
/// </summary>
public sealed class FocusEffect : EffectBase
{
    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 14,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(8000);

    /// <summary>
    ///     The bonus applied to Dmg while active
    /// </summary>
    public int DmgBonus { get; init; } = 25;

    /// <summary>
    ///     The bonus applied to Hit while active
    /// </summary>
    public int HitBonus { get; init; } = 25;

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
