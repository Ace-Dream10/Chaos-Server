#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Empower yourself with solar fire, temporarily increasing Fire damage and Burn application - a self-buff,
///     per the locked design. Reworked from this templateKey's previous content (an AoE damage+blind nuke, which
///     didn't match the locked design's description at all - found during Phase 2's content-vs-design
///     verification pass, not carried over from before).
/// </summary>
/// <remarks>
///     "Increasing Burn application" is realized as a flat bonus added directly to Kindling's own roll
///     (<see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" /> reads both this
///     tag and Kindling's base chance together) rather than a second independent proc chance.
/// </remarks>
public sealed class SolarFlareEffect : EffectBase
{
    public const string BonusKindlingChanceTag = "solar_flare_bonus_kindling_chance";
    public const string FireDamageBonusPctTag = "solar_flare_fire_damage_bonus_pct";

    private static readonly Animation FlareAnimation = new()
    {
        TargetAnimation = 50,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(12);

    /// <inheritdoc />
    public override byte Icon => 62;

    /// <inheritdoc />
    public override string Name => "Solar Flare";

    /// <summary>
    ///     The additional Burn proc chance (0-1) added to Kindling's own roll while active. Set by the applying
    ///     script before Apply() is called, the same way <see cref="BleedEffect.BleedDamage" /> is set.
    /// </summary>
    public double BonusKindlingChance { get; set; } = 0.2;

    /// <summary>
    ///     The bonus Fire-element damage percentage granted while active
    /// </summary>
    public int FireDamageBonusPct { get; set; } = 25;

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[FireDamageBonusPctTag] = FireDamageBonusPct.ToString();
        Subject.Trackers.Tags[BonusKindlingChanceTag] = BonusKindlingChance.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Subject.Animate(FlareAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated()
    {
        Subject.Trackers.Tags.TryRemove(FireDamageBonusPctTag, out _);
        Subject.Trackers.Tags.TryRemove(BonusKindlingChanceTag, out _);
    }
}
