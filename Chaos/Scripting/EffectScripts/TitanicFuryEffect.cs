#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Applied by Titanic Fury - design doc's own note: "Berserker's equivalent of Slayer's Scythe," a Rage-spent
///     burst window. Grants a temporary attack speed + flat damage bonus. Magnitude/duration are settable
///     properties (not hardcoded), populated by <see cref="Chaos.Scripting.SkillScripts.TitanicFuryScript" /> before
///     <c>Apply</c> is called - same extensibility pattern as <see cref="BerserkerGateEffect" />, since this is also
///     one of Berserker's 5 evolving abilities.
/// </summary>
public sealed class TitanicFuryEffect : EffectBase
{
    private static readonly Animation FuryAnimation = new()
    {
        TargetAnimation = 402,
        AnimationSpeed = 100
    };

    /// <summary>
    ///     Percentage attack speed bonus applied while active.
    /// </summary>
    public int AtkSpeedPctBonus { get; set; }

    /// <summary>
    ///     Flat weapon damage bonus applied while active.
    /// </summary>
    public int DamageBonus { get; set; }

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(5);

    /// <inheritdoc />
    public override byte Icon => 46;

    /// <inheritdoc />
    public override string Name => "Titanic Fury";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.StatSheet.AddBonus(
            new Attributes
            {
                Dmg = DamageBonus,
                AtkSpeedPct = AtkSpeedPctBonus
            });

        Subject.Animate(FuryAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated()
        => Subject.StatSheet.SubtractBonus(
            new Attributes
            {
                Dmg = DamageBonus,
                AtkSpeedPct = AtkSpeedPctBonus
            });
}
