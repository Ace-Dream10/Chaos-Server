#region
using Chaos.Formulae.Experience;
using Chaos.Models.World;
using Chaos.Testing.Infrastructure.Mocks;
using FluentAssertions;
#endregion

namespace Chaos.Tests;

/// <summary>
///     Covers GetGroupSizeDeductions for group sizes beyond the old hardcoded 1-6 table, which used to throw
///     ArgumentOutOfRangeException for any group larger than 6 - a real crash risk once MaxGroupSize was raised
///     past the old retail default.
/// </summary>
public sealed class DefaultExperienceFormulaTests
{
    private static Aisling[] CreateGroupOfSize(int size)
    {
        var map = MockMapInstance.Create();

        return Enumerable.Range(0, size)
                          .Select(i => MockAisling.Create(map, $"Member{i}"))
                          .ToArray();
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    [Arguments(5)]
    [Arguments(6)]
    [Arguments(7)]
    [Arguments(10)]
    [Arguments(13)]
    [Arguments(20)]
    public void Calculate_ShouldNotThrow_ForAnyGroupSize(int size)
    {
        var formula = new DefaultExperienceFormula();
        var members = CreateGroupOfSize(size);
        var monster = MockMonster.Create(members[0].MapInstance, level: 1);

        var act = () => formula.Calculate(monster, members);

        act.Should()
           .NotThrow();
    }

    [Test]
    public void Calculate_ShouldMatchOriginalHardcodedTable_ForSizes1Through6()
    {
        var formula = new DefaultExperienceFormula();

        // original hardcoded switch: 1=>0%, 2=>0%, 3=>20%, 4=>30%, 5=>40%, 6=>50% deduction
        var expectedDeductionPct = new Dictionary<int, decimal>
        {
            [1] = 0.0m,
            [2] = 0.0m,
            [3] = 0.20m,
            [4] = 0.30m,
            [5] = 0.40m,
            [6] = 0.50m
        };

        foreach ((var size, var deductionPct) in expectedDeductionPct)
        {
            var map = MockMapInstance.Create();
            var members = CreateGroupOfSize(size);
            var monster = MockMonster.Create(map, level: 1, templateSetup: t => t with { ExpReward = 1000 });

            var exp = formula.Calculate(monster, members);
            var expectedExp = Convert.ToInt64(1000 * (1 - deductionPct));

            exp.Should()
               .Be(expectedExp, $"group size {size} should apply a {deductionPct:P0} deduction");
        }
    }

    [Test]
    public void Calculate_ShouldReturnZero_ForGroupSize11AndAbove()
    {
        var formula = new DefaultExperienceFormula();

        foreach (var size in new[] { 11, 13, 20 })
        {
            var map = MockMapInstance.Create();
            var members = CreateGroupOfSize(size);
            var monster = MockMonster.Create(map, level: 1, templateSetup: t => t with { ExpReward = 1000 });

            var exp = formula.Calculate(monster, members);

            exp.Should()
               .Be(0, $"group size {size} hits the clamped 100% deduction (a straight-line continuation of the " +
                      "original 1-6 curve, not a deliberately tuned answer for large groups - see the class doc comment)");
        }
    }
}
