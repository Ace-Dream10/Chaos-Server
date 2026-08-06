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
    ///     Deduction grows 10% per member starting at group size 3 (0%, 0%, 20%, 30%, 40%, 50%... - matches the
    ///     original hardcoded 1-6 table exactly), clamped to 100% instead of throwing once <see cref="Chaos.Services.Servers.Options.WorldOptions.MaxGroupSize" />
    ///     allows groups larger than the old size-6 ceiling this was originally written for. Note: this formula
    ///     hits a 100% deduction (zero group XP) at group size 11 and stays there for anything larger - that's a
    ///     straight-line continuation of the existing 1-6 curve, not a deliberately tuned answer for what a
    ///     12-13 person group should earn. Revisit if very large groups getting zero group-kill XP isn't the
    ///     intended balance.
    /// </summary>
    protected virtual decimal GetGroupSizeDeductions(ICollection<Aisling> group)
    {
        var count = group.Count;

        if (count <= 2)
            return 0;

        return Math.Min(1m, 0.20m + (count - 3) * 0.10m);
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