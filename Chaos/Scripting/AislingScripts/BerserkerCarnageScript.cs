#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Scripting.AislingScripts.Abstractions;
#endregion

namespace Chaos.Scripting.AislingScripts;

/// <summary>
///     Carnage - a true always-on Berserker passive, following the same pattern as
///     <see cref="BerserkerRageScript" /> (and <see cref="BerserkerUnbrokenScript" />): no-ops unless the subject
///     is currently a Berserker, no skill activation involved. Locked design: "the lower your Health, the more
///     damage you deal" - a glass-cannon passive that pairs with Unbroken (Unbroken keeps you alive at low HP,
///     Carnage rewards being there).
/// </summary>
/// <remarks>
///     Placeholder formula, not balance-tested: no bonus above <see cref="ThresholdPct" /> (50%) HP; below that,
///     skill damage bonus scales linearly from 0% up to <see cref="MaxBonusPct" /> (40%) as HP approaches 0,
///     capping at 40% rather than reaching an undefined value exactly at 0 HP. Uses the same recompute-and-diff
///     sync pattern <see cref="BerserkerRageScript.SyncDamageBonus" /> already established, so the bonus updates
///     continuously as HP changes rather than only on a fixed tick.
/// </remarks>
public class BerserkerCarnageScript : AislingScriptBase
{
    private const int MaxBonusPct = 40;
    private const decimal ThresholdPct = 0.5m;
    private int LastAppliedBonus;

    /// <inheritdoc />
    public BerserkerCarnageScript(Aisling subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (Subject.UserStatSheet.BaseClass != BaseClass.Berserker)
        {
            if (LastAppliedBonus != 0)
                RemoveBonus();

            return;
        }

        SyncDamageBonus();
    }

    private void RemoveBonus()
    {
        Subject.StatSheet.SubtractBonus(new Attributes { SkillDamagePct = LastAppliedBonus });
        LastAppliedBonus = 0;
    }

    private void SyncDamageBonus()
    {
        var maxHp = Subject.StatSheet.EffectiveMaximumHp;
        var currentHpPct = maxHp <= 0 ? 1m : Subject.StatSheet.CurrentHp / (decimal)maxHp;

        var desiredBonus = currentHpPct >= ThresholdPct
            ? 0
            : Convert.ToInt32(MaxBonusPct * (1 - currentHpPct / ThresholdPct));

        desiredBonus = Math.Min(desiredBonus, MaxBonusPct);

        if (desiredBonus == LastAppliedBonus)
            return;

        if (LastAppliedBonus != 0)
            Subject.StatSheet.SubtractBonus(new Attributes { SkillDamagePct = LastAppliedBonus });

        if (desiredBonus != 0)
            Subject.StatSheet.AddBonus(new Attributes { SkillDamagePct = desiredBonus });

        LastAppliedBonus = desiredBonus;
    }
}
