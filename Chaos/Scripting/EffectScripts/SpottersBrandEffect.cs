#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Applied by Fletcher's Spotter's Brand. Marks the target with a bonus critical-strike-chance percentage that
///     benefits every attacker, not just the marking Fletcher - matches the locked design's "increasing the
///     critical strike chance they receive from you and your allies" literally. Consumed by
///     <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" />'s crit-roll hook - see
///     that hook's own comment for why a scoped local crit check was built rather than a full engine-wide crit
///     system (no CritChance stat/roll exists anywhere else in this codebase). Placeholder duration/magnitude, not
///     balance-tested.
/// </summary>
public sealed class SpottersBrandEffect : EffectBase
{
    public const string CritChanceBonusTag = "spottersBrandCritBonus";
    private const int CritChanceBonusPct = 25;

    private static readonly Animation MarkAnimation = new()
    {
        TargetAnimation = 56,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(10);

    /// <inheritdoc />
    public override byte Icon => 56;

    /// <inheritdoc />
    public override string Name => "Spotter's Brand";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[CritChanceBonusTag] = CritChanceBonusPct.ToString();
        Subject.Animate(MarkAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(CritChanceBonusTag, out _);
}
