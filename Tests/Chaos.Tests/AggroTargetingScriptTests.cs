#region
using Chaos.Geometry;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.MonsterScripts;
using Chaos.Scripting.MonsterScripts.Abstractions;
using Chaos.Testing.Infrastructure.Harnesses;
using Chaos.Testing.Infrastructure.Mocks;
using FluentAssertions;
using Moq;
#endregion

namespace Chaos.Tests;

/// <summary>
///     Real functional tests for AggroTargetingScript's Delirium-confusion targeting, covering a bug found while
///     investigating Trickster's Delirium ("the confused target doesn't appear to actually attack another nearby
///     creature").
/// </summary>
public sealed class AggroTargetingScriptTests
{
    [Test]
    public void DeliriumConfusion_ShouldStickWithCurrentTarget_InsteadOfRerollingEveryTick()
    {
        //previously this branch called `Target = null` then re-picked a brand new random candidate every time
        //the ~250ms target-update timer elapsed, so a confused monster's target flickered constantly and never
        //stayed still long enough to actually close distance and land a hit. Confirms the fix: across several
        //elapsed intervals, the target should stay the same as long as it's still a valid candidate.
        var harness = new MonsterScriptHarness<AggroTargetingScript>();
        harness.Subject.Trackers.Tags["delirium"] = bool.TrueString;

        //AggroTargetingScript early-returns entirely unless Map.HasAislings - MockAisling.Create doesn't
        //register the harness's Source in the map's object collection by default, same "constructor sets a
        //MapInstance reference but doesn't index the entity" gotcha documented for MockMonster
        harness.Map.AddEntity(harness.Source, Chaos.Geometry.Point.From(harness.Source));

        //a fresh mock's Script stub doesn't implement real line-of-sight logic (returns false unless explicitly
        //configured) - same delegation gotcha noted elsewhere for IsHostileTo
        MockMonster.SetupScript(harness.Subject, mock => mock.Setup(s => s.CanSee(It.IsAny<VisibleEntity>())).Returns(true));

        var deliriumEffect = new DeliriumEffect();
        deliriumEffect.SetDuration(TimeSpan.FromSeconds(30));
        harness.Subject.Effects.Apply(harness.Source, deliriumEffect, MockSkill.Create().Script);

        var candidate1 = MockMonster.Create(harness.Map, setup: m => m.WarpTo(new Point(harness.Subject.X + 1, harness.Subject.Y)));
        harness.Map.AddEntity(candidate1, Point.From(candidate1));

        var candidate2 = MockMonster.Create(harness.Map, setup: m => m.WarpTo(new Point(harness.Subject.X - 1, harness.Subject.Y)));
        harness.Map.AddEntity(candidate2, Point.From(candidate2));

        //first interval elapses - a target gets picked
        harness.Update(TimeSpan.FromMilliseconds(300));
        var firstTarget = harness.Subject.Target;

        firstTarget.Should()
                   .NotBeNull("Delirium should pick a random nearby creature to target");

        //several more elapsed intervals - the target should NOT change as long as it's still valid
        for (var i = 0; i < 5; i++)
            harness.Update(TimeSpan.FromMilliseconds(300));

        harness.Subject.Target
               .Should()
               .Be(firstTarget, "a confused monster should stick with its current target instead of re-rolling every tick");
    }
}
