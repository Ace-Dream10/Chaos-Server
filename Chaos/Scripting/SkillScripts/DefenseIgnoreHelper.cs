#region
using Chaos.Models.World.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Shared helper for Slayer's defense-ignoring strikes (<see cref="MeasuredSliceScript" />,
///     <see cref="PrecisionScript" />, <see cref="CruelThrustScript" />).
/// </summary>
/// <remarks>
///     <see cref="Chaos.Formulae.Damage.DefaultDamageFormula" /> multiplies damage by
///     <c>(1 + defenderAc / 100)</c> inside the shared damage pipeline (AC is inverted in this engine - negative
///     = stronger defense). That step can't be skipped without bypassing the shared pipeline entirely, so this
///     pre-compensates the raw damage passed in: it computes what multiplier the pipeline is about to apply, and
///     what multiplier SHOULD apply if only <c>(1 - ignorePct)</c> of the target's AC counted, then scales the
///     input damage by the ratio of the two - so after the pipeline's own AC multiply, the result lands at the
///     desired partial-mitigation figure. Placeholder approximation, not balance-tested; holds for normal AC
///     ranges but isn't exact at extremes.
/// </remarks>
internal static class DefenseIgnoreHelper
{
    /// <summary>
    ///     Returns <paramref name="rawDamage" /> adjusted so the shared damage pipeline's own AC mitigation
    ///     produces the equivalent of only <c>(1 - ignorePct)</c> of <paramref name="target" />'s AC applying.
    /// </summary>
    public static int ApplyIgnoreDefense(Creature target, int rawDamage, decimal ignorePct)
    {
        var ac = target.StatSheet.EffectiveAc;
        var fullMultiplier = 1 + (ac / 100m);

        if (fullMultiplier == 0)
            return rawDamage;

        var desiredMultiplier = 1 + ((ac * (1 - ignorePct)) / 100m);

        return Convert.ToInt32(rawDamage * (desiredMultiplier / fullMultiplier));
    }
}
