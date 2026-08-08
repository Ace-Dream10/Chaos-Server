#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     An AoE attack-speed slow. Per the locked design, "Weakening Shout" is no longer a standalone ability - it's
///     absorbed into <see cref="Chaos.Scripting.SkillScripts.ChallengingShoutScript" />'s tier IV (max) evolution,
///     applied to every monster pulled by that cast. Reduces <see cref="Chaos.Models.Data.StatSheet.AtkSpeedPctMod" />
///     via a negative <see cref="Attributes.AtkSpeedPct" /> bonus (see <see cref="Chaos.Models.Data.StatSheet.CalculateEffectiveAssailInterval" />
///     for how that translates into a slower attack interval) - this works uniformly for both Aislings and
///     Monsters since it's a StatSheet-level bonus rather than a Monster-only field like <see cref="SlowEffect" />'s
///     movement-speed adjustment.
/// </summary>
public sealed class WeakeningShoutEffect : EffectBase
{
    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 41,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(6);

    /// <inheritdoc />
    public override byte Icon => 41;

    /// <inheritdoc />
    public override string Name => "Weakening Shout";

    /// <summary>
    ///     The amount subtracted from the subject's attack speed percent while weakened. Placeholder, not
    ///     balance-tested. Set by the applying script before Apply() is called, mirroring
    ///     <see cref="SlowEffect.SlowAmount" />'s convention.
    /// </summary>
    public int AtkSpeedPenalty { get; set; } = 30;

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.StatSheet.AddBonus(new Attributes { AtkSpeedPct = -AtkSpeedPenalty });
        Subject.Animate(ApplyAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.StatSheet.SubtractBonus(new Attributes { AtkSpeedPct = -AtkSpeedPenalty });
}
