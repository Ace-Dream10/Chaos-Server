#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     One of Bard's 5 evolving abilities. Renamed from "Cradh" per the locked design. Detailed tiers were given
///     explicitly (unlike Fletcher's evolving abilities, which had none): "Early: reduce target's defense by 10%.
///     Mid: reduce defense by 20%, slightly lower damage dealt. Late: reduce defense by 30%, lower damage dealt,
///     small chance for attacks against the target to critically strike." Only 3 named stages for the floor
///     schedule's 4 checkpoints (obtain, Floor4, Floor7, Floor10 max) - Tier II interpolates between Early and Mid,
///     flagged as an interpolation rather than a literal locked value. "Reduce defense by X%" is approximated as a
///     flat AC penalty (worse defense - this game's AC convention is inverted, confirmed by the old Stacia's
///     Armor's negative-AC-improves-defense pattern) rather than a true percentage-of-total calculation, since no
///     "percent of AC" convention exists elsewhere in the codebase either. <see cref="DamageDealtReductionPct" /> is
///     read from the SOURCE (attacker) side of ApplyAttackDamageScript's multiplier chain - this debuff reduces the
///     cursed creature's own outgoing damage, the mirror image of every other damage-modifier tag tonight which
///     read from the target side. <see cref="CritVulnerabilityPct" /> reuses the same scoped crit-roll shape
///     Spotter's Brand/Windrunner/Battle Hymn established. All placeholder values, not balance-tested.
/// </summary>
public sealed class BardsMaledictionEffect : EffectBase
{
    public const string CritVulnerabilityTag = "maledictionCritVulnerability";
    public const string DamageDealtReductionTag = "maledictionDamageDealtReduction";

    private static readonly Animation MarkAnimation = new()
    {
        TargetAnimation = 56,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(15);

    public int AcPenalty { get; set; }
    public int CritVulnerabilityPct { get; set; }
    public int DamageDealtReductionPct { get; set; }

    /// <inheritdoc />
    public override byte Icon => 56;

    /// <inheritdoc />
    public override string Name => "Bard's Malediction";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.StatSheet.AddBonus(new Attributes { Ac = AcPenalty });

        if (DamageDealtReductionPct > 0)
            Subject.Trackers.Tags[DamageDealtReductionTag] = DamageDealtReductionPct.ToString();

        if (CritVulnerabilityPct > 0)
            Subject.Trackers.Tags[CritVulnerabilityTag] = CritVulnerabilityPct.ToString();

        Subject.Animate(MarkAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated()
    {
        Subject.StatSheet.SubtractBonus(new Attributes { Ac = AcPenalty });
        Subject.Trackers.Tags.TryRemove(DamageDealtReductionTag, out _);
        Subject.Trackers.Tags.TryRemove(CritVulnerabilityTag, out _);
    }
}
