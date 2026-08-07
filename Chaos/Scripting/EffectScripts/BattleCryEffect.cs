#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Applied by Battle Cry. A flat (non-evolving) rallying shout - grants <see cref="DamagePctBonus" /> skill
///     damage for <see cref="Duration" /> to whoever it's applied to. The skill itself (battle_cry.json) uses the
///     existing generic "applyEffect" script with an AoE friendly-target filter, so this class only needs to define
///     the buff itself, not how it's delivered - same reusable pattern as Unbroken/Root.
/// </summary>
/// <remarks>
///     Placeholder numbers, not final: +10% skill damage for 10 seconds. No existing precedent to derive these
///     from (Battle Cry is entirely new content) - picked to be a noticeable but not run-away combat buff, in line
///     with Rage's own scaling (up to +50 flat skill damage at full 100 rage via
///     <see cref="Chaos.Scripting.AislingScripts.BerserkerRageScript" />). Needs real balance-pass numbers.
/// </remarks>
public sealed class BattleCryEffect : EffectBase
{
    /// <summary>
    ///     Percentage skill damage bonus granted while this effect is active. Placeholder value - not balance-tested.
    /// </summary>
    private const int DamagePctBonus = 10;

    private static readonly Animation CryAnimation = new()
    {
        TargetAnimation = 14,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(10);

    /// <inheritdoc />
    public override byte Icon => 46;

    /// <inheritdoc />
    public override string Name => "Battle Cry";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.StatSheet.AddBonus(new Attributes { SkillDamagePct = DamagePctBonus });
        Subject.Animate(CryAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.StatSheet.SubtractBonus(new Attributes { SkillDamagePct = DamagePctBonus });
}
