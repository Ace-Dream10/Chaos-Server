#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Boosts attack speed and skill damage while active. Auto-triggered by
///     <see cref="Chaos.Scripting.AislingScripts.AssassinFrenzyScript" /> the instant banked kill energy hits its
///     hard cap, for a flat duration (not proportional to anything) - this class's own <see cref="Duration" />
///     default is unused in practice since the caller always overrides it via <c>SetDuration</c>.
/// </summary>
public sealed class BloodlustEffect : EffectBase
{
    private static readonly TimeSpan AuraPulseInterval = TimeSpan.FromMilliseconds(800);

    private static readonly Animation AuraAnimation = new()
    {
        TargetAnimation = 133,
        AnimationSpeed = 100
    };

    private TimeSpan SinceLastPulse = TimeSpan.Zero;

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(1);

    /// <inheritdoc />
    public override byte Icon => 58;

    /// <inheritdoc />
    public override string Name => "Bloodlust";

    /// <summary>
    ///     The bonus applied to AtkSpeedPct while active
    /// </summary>
    public int AtkSpeedBonus { get; init; } = 50;

    /// <summary>
    ///     The bonus applied to FlatSkillDamage while active
    /// </summary>
    public int FlatDamageBonus { get; init; } = 30;

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.StatSheet.AddBonus(
            new Attributes
            {
                AtkSpeedPct = AtkSpeedBonus,
                FlatSkillDamage = FlatDamageBonus
            });

        Subject.Animate(AuraAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        base.Update(delta);

        SinceLastPulse += delta;

        if (SinceLastPulse < AuraPulseInterval)
            return;

        SinceLastPulse = TimeSpan.Zero;
        Subject.Animate(AuraAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated()
        => Subject.StatSheet.SubtractBonus(
            new Attributes
            {
                AtkSpeedPct = AtkSpeedBonus,
                FlatSkillDamage = FlatDamageBonus
            });
}
