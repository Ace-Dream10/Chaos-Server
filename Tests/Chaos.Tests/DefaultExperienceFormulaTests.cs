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
///     past the old retail default. Also covers the current curve: 0% deduction for group sizes 1-3, then +5%
///     per member starting at size 4.
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
    public void Calculate_ShouldApplyCurrentCurve_ForSizes1Through13()
    {
        var formula = new DefaultExperienceFormula();

        // current curve: 0% deduction for sizes 1-3, then +5% per member starting at size 4
        var expectedDeductionPct = new Dictionary<int, decimal>
        {
            [1] = 0.00m,
            [2] = 0.00m,
            [3] = 0.00m,
            [4] = 0.05m,
            [5] = 0.10m,
            [6] = 0.15m,
            [7] = 0.20m,
            [10] = 0.35m,
            [13] = 0.50m
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
    public void Calculate_ShouldClampAtZero_ForGroupSize23AndAbove()
    {
        var formula = new DefaultExperienceFormula();

        foreach (var size in new[] { 23, 30 })
        {
            var map = MockMapInstance.Create();
            var members = CreateGroupOfSize(size);
            var monster = MockMonster.Create(map, level: 1, templateSetup: t => t with { ExpReward = 1000 });

            var exp = formula.Calculate(monster, members);

            exp.Should()
               .Be(0, $"group size {size} hits the clamped 100% deduction (the safety clamp, not part of the " +
                      "tuned range - see the class doc comment)");
        }
    }
}
