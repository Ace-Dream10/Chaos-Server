#region
using Chaos.Formulae.Abstractions;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
#endregion

namespace Chaos.Formulae.Experience;

public class DefaultExperienceFormula : IExperienceFormula
{
    /// <inheritdoc />
    public long Calculate(Creature killedCreature, params ICollection<Aisling> aislings)
    {
        switch (killedCreature)
        {
            case Aisling:
                return 0;
            case Monster monster:
                var groupSizeDeductions = GetGroupSizeDeductions(aislings);
                var partyLevelDifferenceDeductions = GetPartyLevelDifferenceDeductions(aislings);
                var monsterLevelDeductions = GetMonsterLevelDifferenceDeductions(aislings, monster);

                var groupMultiplier = Math.Max(0, 1 - (groupSizeDeductions + partyLevelDifferenceDeductions));
                var monsterLevelMultiplier = Math.Max(0, 1 - monsterLevelDeductions);

                return Convert.ToInt64(monster.Experience * groupMultiplier * monsterLevelMultiplier);
        }

        return 0;
    }

    /// <summary>
    ///     No deduction for groups of 1-3. Starting at group size 4, deduction grows 5% per additional member (5%,
    ///     10%, 15%, ...), clamped to 100%. Retuned for <see cref="Chaos.Services.Servers.Options.WorldOptions.MaxGroupSize" />
    ///     going from the old retail default of 6 up to 13: at the new max group size this tops out at a 50%
    ///     deduction (still half of solo-kill XP per person) rather than the old 10%/member curve's zero-XP result
    ///     at size 11+. The clamp to 100% doesn't actually engage until group size 23, well past the current
    ///     MaxGroupSize - it's a safety bound, not part of the tuned range.
    /// </summary>
    protected virtual decimal GetGroupSizeDeductions(ICollection<Aisling> group)
    {
        var count = group.Count;

        if (count <= 3)
            return 0;

        return Math.Min(1m, (count - 3) * 0.05m);
    }

    // ReSharper disable once ParameterTypeCanBeEnumerable.Global
    protected virtual decimal GetMonsterLevelDifferenceDeductions(ICollection<Aisling> group, Monster monster)
    {
        var averageLevel = Convert.ToInt32(group.Average(p => p.StatSheet.Level));
        var monsterLevel = monster.StatSheet.Level;

        var upperBound = LevelRangeFormulae.Default.GetUpperBound(averageLevel);
        var lowerBound = LevelRangeFormulae.Default.GetLowerBound(averageLevel);

        if ((monsterLevel >= lowerBound) && (monsterLevel <= upperBound))
            return 0;

        var bounds = monsterLevel < averageLevel ? lowerBound : upperBound;
        var stepSize = Math.Abs(bounds - averageLevel) / 2.0m;
        var faultSize = Math.Abs(bounds - monsterLevel);

        return Math.Min(1, faultSize / stepSize * 0.25m);
    }

    protected virtual decimal GetPartyLevelDifferenceDeductions(ICollection<Aisling> group)
    {
        var lowestMember = group.MinBy(p => p.StatSheet.Level)!;
        var highestMember = group.MaxBy(p => p.StatSheet.Level)!;

        if (lowestMember.WithinLevelRange(highestMember))
            return 0;

        var lowerBound = LevelRangeFormulae.Default.GetLowerBound(highestMember.StatSheet.Level);
        var stepSize = (highestMember.StatSheet.Level - lowerBound) / 2.0m;
        var faultSize = lowerBound - lowestMember.StatSheet.Level;

        return Math.Min(1, faultSize / stepSize * 0.25m);
    }
}