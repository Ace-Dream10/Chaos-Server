#region
using Chaos.Models.Data;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     The "marks a target" step of Slayer's identity loop ("Marks a target, builds Execution on a single enemy,
///     then finishes them with Scythe"). While marked, the target takes increased damage from all sources - the
///     multiplier itself is applied in
///     <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" />, the same tag-read
///     pattern already established for <see cref="BoilingBloodEffect" />. Precision refreshes this effect's
///     duration and gets bonus defense-ignore while it's active, per the locked design's combo note.
/// </summary>
/// <remarks>
///     Simplification, not balance-tested: applies from all sources like Boiling Blood does, rather than tracking
///     which Aisling applied the mark and restricting the bonus to them specifically - keeps this stateless and
///     consistent with the rest of this file's tag-based approach instead of inventing new per-attacker tracking.
/// </remarks>
public sealed class MarkOfTheBaneEffect : EffectBase
{
    public const string BonusDamagePctTag = "mark_of_the_bane_bonus_pct";

    private static readonly Animation MarkAnimation = new()
    {
        TargetAnimation = 56,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(10);

    /// <inheritdoc />
    public override byte Icon => 58;

    /// <inheritdoc />
    public override string Name => "Mark of the Bane";

    /// <summary>
    ///     The bonus damage percentage the mark grants - set by <see cref="Chaos.Scripting.SkillScripts.MarkOfTheBaneScript" /> per tier before
    ///     Apply() is called, the same way <see cref="BleedEffect.BleedDamage" /> is set.
    /// </summary>
    public int BonusDamagePct { get; set; }

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[BonusDamagePctTag] = BonusDamagePct.ToString();
        Subject.Animate(MarkAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(BonusDamagePctTag, out _);

    /// <summary>
    ///     Always allow reapplication so Precision can refresh the mark's duration mid-fight, matching
    ///     <see cref="SeveranceEffect.ShouldApply" />'s same reasoning.
    /// </summary>
    public override bool ShouldApply(Creature source, Creature target) => true;
}
