#region
using Chaos.Collections;
using Chaos.Testing.Infrastructure.Mocks;
using FluentAssertions;
#endregion

namespace Chaos.Tests;

public sealed class ParticipantSetTests
{
    [Test]
    public void Add_ShouldIncludeAislingName()
    {
        var set = new ParticipantSet();
        var aisling = MockAisling.Create(name: "Ace");

        set.Add(aisling);

        set.Should()
           .ContainSingle()
           .Which.Should()
           .Be("Ace");
    }

    [Test]
    public void Add_ShouldNotDuplicate_WhenSameAislingAddedTwice()
    {
        var set = new ParticipantSet();
        var aisling = MockAisling.Create(name: "Ace");

        set.Add(aisling);
        set.Add(aisling);

        set.Should()
           .ContainSingle();
    }

    [Test]
    public void Add_ShouldBeCaseInsensitive()
    {
        var set = new ParticipantSet();
        var lower = MockAisling.Create(name: "ace");
        var upper = MockAisling.Create(name: "ACE");

        set.Add(lower);
        set.Add(upper);

        set.Should()
           .ContainSingle();
    }

    [Test]
    public void Add_ShouldAccumulateDistinctParticipants()
    {
        var set = new ParticipantSet();
        var first = MockAisling.Create(name: "Ace");
        var second = MockAisling.Create(name: "Dream");

        set.Add(first);
        set.Add(second);

        set.Should()
           .BeEquivalentTo(["Ace", "Dream"]);
    }
}
