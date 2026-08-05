#region
using Chaos.Scripting.AislingScripts;
using Chaos.Testing.Infrastructure.Harnesses;
using Chaos.Testing.Infrastructure.Mocks;
using FluentAssertions;
#endregion

namespace Chaos.Tests;

public sealed class AscensionFloorTrackerScriptTests
{
    [Test]
    public void OnAttacked_ShouldAddSubjectToBossParticipants_WhenSourceIsTaggedBoss()
    {
        var harness = new AislingScriptHarness<AscensionFloorTrackerScript>();
        var boss = MockMonster.Create(name: "FloorBoss");
        boss.Trackers.Tags["ascensionFloor"] = "2";

        harness.Attacked(boss, 15);

        boss.AscensionParticipants.Should()
            .ContainSingle()
            .Which.Should()
            .Be(harness.Source.Name);
    }

    [Test]
    public void OnAttacked_ShouldNotAddSubject_WhenSourceIsUntaggedMonster()
    {
        var harness = new AislingScriptHarness<AscensionFloorTrackerScript>();
        var regularMonster = MockMonster.Create(name: "RegularMonster");

        harness.Attacked(regularMonster, 15);

        regularMonster.AscensionParticipants.Should()
                      .BeEmpty();
    }

    [Test]
    public void OnAttacked_ShouldNotThrow_WhenSourceIsAnotherAisling()
    {
        var harness = new AislingScriptHarness<AscensionFloorTrackerScript>();
        var otherAisling = MockAisling.Create(name: "Attacker");

        var act = () => harness.Attacked(otherAisling, 15);

        act.Should()
           .NotThrow();
    }
}
