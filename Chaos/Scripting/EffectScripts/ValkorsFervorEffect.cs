#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
using Chaos.Time;
using Chaos.Time.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     A Martial Artist self-buff, tied to Valkor's war domain (renamed from the working name "Tiger Stance" - the
///     Floor 1 Battle Focus slot's naming was confirmed and locked as Valkor's Fervor this session, not left open).
///     Boosts STR/DEX/attack speed/flat skill damage, and doubles Chi generation for the duration (checked directly
///     in <see cref="Chaos.Scripting.AislingScripts.MartialArtistChiScript" /> via
///     <see cref="ValkorsFervorTag" />). Periodically re-plays its aura animation since animations don't persist
///     visually on their own.
/// </summary>
public sealed class ValkorsFervorEffect : IntervalEffectBase
{
    public const string ValkorsFervorTag = "valkors_fervor";
    private const int AtkSpeedBonus = 20;
    private const int DexBonus = 10;
    private const int FlatSkillDamageBonus = 20;
    private const int StrBonus = 15;

    private static readonly Animation AuraAnimation = new()
    {
        TargetAnimation = 245,
        AnimationSpeed = 100
    };

    private static readonly Animation RoarAnimation = new()
    {
        TargetAnimation = 14,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(20000);

    /// <inheritdoc />
    protected override IIntervalTimer Interval { get; } = new IntervalTimer(TimeSpan.FromMilliseconds(1500));

    /// <inheritdoc />
    public override byte Icon => 74;

    /// <inheritdoc />
    public override string Name => "Valkor's Fervor";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.StatSheet.AddBonus(
            new Attributes
            {
                Str = StrBonus,
                Dex = DexBonus,
                AtkSpeedPct = AtkSpeedBonus,
                FlatSkillDamage = FlatSkillDamageBonus
            });

        Subject.Trackers.Tags[ValkorsFervorTag] = bool.TrueString;
        Subject.Animate(RoarAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated()
    {
        Subject.StatSheet.SubtractBonus(
            new Attributes
            {
                Str = StrBonus,
                Dex = DexBonus,
                AtkSpeedPct = AtkSpeedBonus,
                FlatSkillDamage = FlatSkillDamageBonus
            });

        Subject.Trackers.Tags.TryRemove(ValkorsFervorTag, out _);
    }

    /// <inheritdoc />
    protected override void OnIntervalElapsed()
    {
        if (Subject.IsAlive)
            Subject.Animate(AuraAnimation, Source.Id);
    }
}
