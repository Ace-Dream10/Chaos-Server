#region
using System.Text.Json;
using Chaos.Collections.Common;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Testing.Infrastructure.Mocks;
using Chaos.Utilities;
using FluentAssertions;
#endregion

namespace Chaos.Tests;

/// <summary>
///     Covers the three things Sorcerer's element-choice infrastructure most needs to get right: order-sensitive
///     Tier II/III module resolution, order-INsensitive final-specialization resolution, and persistence
///     surviving a save/load cycle.
/// </summary>
public sealed class SorcererProgressionHelperTests
{
    [Test]
    public void ResolveSpecialization_ShouldReturnPurePath_WhenBothPicksAreTheSameElement()
    {
        SorcererProgressionHelper.ResolveSpecialization(SorcererElement.Fire, SorcererElement.Fire)
                                  .Should()
                                  .Be(AdvClass.Ignis);

        SorcererProgressionHelper.ResolveSpecialization(SorcererElement.Earth, SorcererElement.Earth)
                                  .Should()
                                  .Be(AdvClass.Earthshaper);

        SorcererProgressionHelper.ResolveSpecialization(SorcererElement.Water, SorcererElement.Water)
                                  .Should()
                                  .Be(AdvClass.Hydrosage);

        SorcererProgressionHelper.ResolveSpecialization(SorcererElement.Wind, SorcererElement.Wind)
                                  .Should()
                                  .Be(AdvClass.Gale);

        SorcererProgressionHelper.ResolveSpecialization(SorcererElement.Arcane, SorcererElement.Arcane)
                                  .Should()
                                  .Be(AdvClass.Arcanist);
    }

    /// <summary>
    ///     Per the Hybrid Construction Rule, both orderings of the same two elements must reach the same Tier IV
    ///     specialization - Fire-then-Earth and Earth-then-Fire both resolve to Magma.
    /// </summary>
    [Test]
    [Arguments(SorcererElement.Fire, SorcererElement.Earth, AdvClass.Magma)]
    [Arguments(SorcererElement.Earth, SorcererElement.Fire, AdvClass.Magma)]
    [Arguments(SorcererElement.Fire, SorcererElement.Water, AdvClass.Cinder)]
    [Arguments(SorcererElement.Water, SorcererElement.Fire, AdvClass.Cinder)]
    [Arguments(SorcererElement.Fire, SorcererElement.Wind, AdvClass.Inferno)]
    [Arguments(SorcererElement.Wind, SorcererElement.Fire, AdvClass.Inferno)]
    [Arguments(SorcererElement.Earth, SorcererElement.Water, AdvClass.Torrent)]
    [Arguments(SorcererElement.Water, SorcererElement.Earth, AdvClass.Torrent)]
    [Arguments(SorcererElement.Earth, SorcererElement.Wind, AdvClass.Sirocco)]
    [Arguments(SorcererElement.Wind, SorcererElement.Earth, AdvClass.Sirocco)]
    [Arguments(SorcererElement.Water, SorcererElement.Wind, AdvClass.Blizzard)]
    [Arguments(SorcererElement.Wind, SorcererElement.Water, AdvClass.Blizzard)]
    public void ResolveSpecialization_ShouldReachSameHybrid_RegardlessOfPickOrder(
        SorcererElement first, SorcererElement second, AdvClass expected)
        => SorcererProgressionHelper.ResolveSpecialization(first, second)
                                     .Should()
                                     .Be(expected);

    /// <summary>
    ///     The MODULE a player receives (which element's Tier II vs Tier III content) IS order-sensitive, even
    ///     though the final specialization identity is not - this is the actual order-sensitivity the locked
    ///     design cares about ("Fire→Earth Magma uses Fire Tier II + Earth Tier III; Earth→Fire Magma uses Earth
    ///     Tier II + Fire Tier III").
    /// </summary>
    [Test]
    public void TierModuleElements_ShouldSwapWithPickOrder_ForTheSameHybridOutcome()
    {
        var fireFirst = (
            TierII: SorcererProgressionHelper.GetTierIIElement(SorcererElement.Fire, SorcererElement.Earth),
            TierIII: SorcererProgressionHelper.GetTierIIIElement(SorcererElement.Fire, SorcererElement.Earth));

        var earthFirst = (
            TierII: SorcererProgressionHelper.GetTierIIElement(SorcererElement.Earth, SorcererElement.Fire),
            TierIII: SorcererProgressionHelper.GetTierIIIElement(SorcererElement.Earth, SorcererElement.Fire));

        //both orderings reach Magma (confirmed above), but via swapped Tier II/III module assignments
        fireFirst.TierII.Should().Be(SorcererElement.Fire);
        fireFirst.TierIII.Should().Be(SorcererElement.Earth);

        earthFirst.TierII.Should().Be(SorcererElement.Earth);
        earthFirst.TierIII.Should().Be(SorcererElement.Fire);

        SorcererProgressionHelper.ResolveSpecialization(fireFirst.TierII, fireFirst.TierIII)
                                  .Should()
                                  .Be(SorcererProgressionHelper.ResolveSpecialization(earthFirst.TierII, earthFirst.TierIII));
    }

    [Test]
    public void ResolveSpecialization_ShouldThrow_ForUnpairedArcane()
    {
        var act = () => SorcererProgressionHelper.ResolveSpecialization(SorcererElement.Arcane, SorcererElement.Fire);

        act.Should()
           .Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void ElementPicks_ShouldPersist_OnASourceAisling()
    {
        var source = MockAisling.Create();

        SorcererProgressionHelper.TryGetElement1(source, out _)
                                  .Should()
                                  .BeFalse();

        SorcererProgressionHelper.SetElement1(source, SorcererElement.Fire);
        SorcererProgressionHelper.SetElement2(source, SorcererElement.Earth);

        SorcererProgressionHelper.TryGetElement1(source, out var element1)
                                  .Should()
                                  .BeTrue();

        element1.Should()
                .Be(SorcererElement.Fire);

        SorcererProgressionHelper.TryGetElement2(source, out var element2)
                                  .Should()
                                  .BeTrue();

        element2.Should()
                .Be(SorcererElement.Earth);
    }

    /// <summary>
    ///     Simulates a logout/reload: serializes the counters to JSON (the actual save mechanism
    ///     <see cref="CounterCollection" />'s own <c>[JsonConverter]</c> attribute supports) and deserializes into a
    ///     fresh instance, confirming the two element picks round-trip correctly rather than only working in-memory
    ///     within a single session.
    /// </summary>
    [Test]
    public void ElementPicks_ShouldSurviveSerializeDeserializeCycle()
    {
        var source = MockAisling.Create();
        SorcererProgressionHelper.SetElement1(source, SorcererElement.Water);
        SorcererProgressionHelper.SetElement2(source, SorcererElement.Wind);

        var json = JsonSerializer.Serialize(source.Trackers.Counters);
        var reloaded = JsonSerializer.Deserialize<CounterCollection>(json);

        reloaded.Should()
                .NotBeNull();

        reloaded!.TryGetValue("sorcererElement1", out var raw1)
                 .Should()
                 .BeTrue();

        ((SorcererElement)raw1).Should()
                                .Be(SorcererElement.Water);

        reloaded.TryGetValue("sorcererElement2", out var raw2)
                .Should()
                .BeTrue();

        ((SorcererElement)raw2).Should()
                                .Be(SorcererElement.Wind);
    }
}
