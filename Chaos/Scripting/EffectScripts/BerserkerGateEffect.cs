#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Applied by Berserker Gate - "the iconic go berserk button" (design doc's own phrasing). A flat damage
///     bonus paired with a defense penalty (higher AC = worse defense - see AC note in
///     <see cref="Chaos.Scripting.SkillScripts.BerserkerGateScript" />'s doc comment) for a duration. Magnitude and
///     duration are NOT hardcoded here (unlike <see cref="UnbrokenEffect" />/<see cref="RootEffect" />) - they're
///     settable properties, populated by <see cref="Chaos.Scripting.SkillScripts.BerserkerGateScript" /> before
///     <c>Apply</c> is called, since this is one of Berserker's 5 evolving abilities (tier scales with the skill's
///     own level, same mechanism <see cref="CycloneScript" /> already uses).
/// </summary>
public sealed class BerserkerGateEffect : EffectBase
{
    private static readonly Animation GateAnimation = new()
    {
        TargetAnimation = 402,
        AnimationSpeed = 100
    };

    /// <summary>
    ///     Defense penalty applied while active - a positive AC value, since lower AC is better defense in this
    ///     engine's inverted AC convention.
    /// </summary>
    public int AcPenalty { get; set; }

    /// <summary>
    ///     Flat weapon damage bonus applied while active.
    /// </summary>
    public int DamageBonus { get; set; }

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(8);

    /// <inheritdoc />
    public override byte Icon => 46;

    /// <inheritdoc />
    public override string Name => "Berserker Gate";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.StatSheet.AddBonus(
            new Attributes
            {
                Dmg = DamageBonus,
                Ac = AcPenalty
            });

        Subject.Animate(GateAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated()
        => Subject.StatSheet.SubtractBonus(
            new Attributes
            {
                Dmg = DamageBonus,
                Ac = AcPenalty
            });
}
