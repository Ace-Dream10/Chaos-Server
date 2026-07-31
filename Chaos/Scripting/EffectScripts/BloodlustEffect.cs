#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Boosts attack speed and skill damage for a duration proportional to the kill energy spent to activate it.
///     <see cref="AtkSpeedBonus" /> and <see cref="FlatDamageBonus" /> are set by
///     <see cref="Chaos.Scripting.SkillScripts.BloodlustScript" /> before applying, since scriptVars aren't
///     auto-populated onto effect instances the way they are for scripts.
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
