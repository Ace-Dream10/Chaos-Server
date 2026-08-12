#region
using Chaos.Scripting.Behaviors;
using Chaos.Testing.Infrastructure.Mocks;
using FluentAssertions;
#endregion

namespace Chaos.Tests;

/// <summary>
///     Confirms the Aisling-side half of CC enforcement actually works, not just the pre-existing Monster-side half.
///     Before this, Root/Blackout/Stasis/Lullaby/Silence set their tags on an Aisling target but had zero
///     mechanical effect - CanMove/CanUseSkill/CanUseSpell all defaulted straight through to just an IsAlive check.
///     Tests target RestrictionBehavior directly since that's the actual single source of truth both
///     DefaultAislingScript and Monster's DefaultBehaviorsScript delegate to - a full Aisling+Skill+Spell harness
///     would just be indirection around the same tag check.
/// </summary>
public sealed class RestrictionBehaviorCcTests
{
    private readonly RestrictionBehavior Behavior = new();

    #region Root
    [Test]
    public void CanMove_ShouldBeFalse_WhenRooted()
    {
        var aisling = MockAisling.Create(setup: a => a.StatSheet.SetHp(100));
        aisling.Trackers.Tags["rooted"] = bool.TrueString;

        Behavior.CanMove(aisling)
                .Should()
                .BeFalse();
    }

    [Test]
    public void CanUseSkill_ShouldBeTrue_WhenOnlyRooted()
    {
        var aisling = MockAisling.Create(setup: a => a.StatSheet.SetHp(100));
        aisling.Trackers.Tags["rooted"] = bool.TrueString;
        var skill = MockSkill.Create();

        Behavior.CanUseSkill(aisling, skill)
                .Should()
                .BeTrue();
    }

    [Test]
    public void CanUseSpell_ShouldBeTrue_WhenOnlyRooted()
    {
        var aisling = MockAisling.Create(setup: a => a.StatSheet.SetHp(100));
        aisling.Trackers.Tags["rooted"] = bool.TrueString;
        var spell = MockSpell.Create();

        Behavior.CanUseSpell(aisling, spell)
                .Should()
                .BeTrue();
    }
    #endregion

    #region Blackout
    [Test]
    public void CanMove_ShouldBeTrue_WhenOnlyBlackedOut()
    {
        var aisling = MockAisling.Create(setup: a => a.StatSheet.SetHp(100));
        aisling.Trackers.Tags["blackout"] = bool.TrueString;

        Behavior.CanMove(aisling)
                .Should()
                .BeTrue();
    }

    [Test]
    public void CanUseSkill_ShouldBeFalse_WhenBlackedOut()
    {
        var aisling = MockAisling.Create(setup: a => a.StatSheet.SetHp(100));
        aisling.Trackers.Tags["blackout"] = bool.TrueString;
        var skill = MockSkill.Create();

        Behavior.CanUseSkill(aisling, skill)
                .Should()
                .BeFalse();
    }

    [Test]
    public void CanUseSpell_ShouldBeFalse_WhenBlackedOut()
    {
        var aisling = MockAisling.Create(setup: a => a.StatSheet.SetHp(100));
        aisling.Trackers.Tags["blackout"] = bool.TrueString;
        var spell = MockSpell.Create();

        Behavior.CanUseSpell(aisling, spell)
                .Should()
                .BeFalse();
    }
    #endregion

    #region Lullaby (asleep)
    [Test]
    public void CanMove_ShouldBeFalse_WhenAsleep()
    {
        var aisling = MockAisling.Create(setup: a => a.StatSheet.SetHp(100));
        aisling.Trackers.Tags["asleep"] = bool.TrueString;

        Behavior.CanMove(aisling)
                .Should()
                .BeFalse();
    }

    [Test]
    public void CanUseSkill_ShouldBeFalse_WhenAsleep()
    {
        var aisling = MockAisling.Create(setup: a => a.StatSheet.SetHp(100));
        aisling.Trackers.Tags["asleep"] = bool.TrueString;
        var skill = MockSkill.Create();

        Behavior.CanUseSkill(aisling, skill)
                .Should()
                .BeFalse();
    }

    [Test]
    public void CanUseSpell_ShouldBeFalse_WhenAsleep()
    {
        var aisling = MockAisling.Create(setup: a => a.StatSheet.SetHp(100));
        aisling.Trackers.Tags["asleep"] = bool.TrueString;
        var spell = MockSpell.Create();

        Behavior.CanUseSpell(aisling, spell)
                .Should()
                .BeFalse();
    }
    #endregion

    #region Stasis (fixes the "cast isn't stopped" gap Part A found)
    [Test]
    public void CanMove_ShouldBeFalse_WhenInStasis()
    {
        var aisling = MockAisling.Create(setup: a => a.StatSheet.SetHp(100));
        aisling.Trackers.Tags["stasis"] = bool.TrueString;

        Behavior.CanMove(aisling)
                .Should()
                .BeFalse();
    }

    [Test]
    public void CanUseSkill_ShouldBeFalse_WhenInStasis()
    {
        var aisling = MockAisling.Create(setup: a => a.StatSheet.SetHp(100));
        aisling.Trackers.Tags["stasis"] = bool.TrueString;
        var skill = MockSkill.Create();

        Behavior.CanUseSkill(aisling, skill)
                .Should()
                .BeFalse();
    }

    [Test]
    public void CanUseSpell_ShouldBeFalse_WhenInStasis()
    {
        var aisling = MockAisling.Create(setup: a => a.StatSheet.SetHp(100));
        aisling.Trackers.Tags["stasis"] = bool.TrueString;
        var spell = MockSpell.Create();

        Behavior.CanUseSpell(aisling, spell)
                .Should()
                .BeFalse();
    }
    #endregion

    #region Silence
    [Test]
    public void CanUseSpell_ShouldBeFalse_WhenSilenced()
    {
        var aisling = MockAisling.Create(setup: a => a.StatSheet.SetHp(100));
        aisling.Trackers.Tags["silenced"] = bool.TrueString;
        var spell = MockSpell.Create();

        Behavior.CanUseSpell(aisling, spell)
                .Should()
                .BeFalse();
    }

    [Test]
    public void CanMove_ShouldBeTrue_WhenOnlySilenced()
    {
        var aisling = MockAisling.Create(setup: a => a.StatSheet.SetHp(100));
        aisling.Trackers.Tags["silenced"] = bool.TrueString;

        Behavior.CanMove(aisling)
                .Should()
                .BeTrue();
    }

    [Test]
    public void CanUseSkill_ShouldBeTrue_WhenOnlySilenced()
    {
        var aisling = MockAisling.Create(setup: a => a.StatSheet.SetHp(100));
        aisling.Trackers.Tags["silenced"] = bool.TrueString;
        var skill = MockSkill.Create();

        Behavior.CanUseSkill(aisling, skill)
                .Should()
                .BeTrue();
    }
    #endregion

    #region No tags - baseline unaffected
    [Test]
    public void AllCanChecks_ShouldBeTrue_WhenNoCcTagsPresent()
    {
        var aisling = MockAisling.Create(setup: a => a.StatSheet.SetHp(100));
        var skill = MockSkill.Create();
        var spell = MockSpell.Create();

        Behavior.CanMove(aisling)
                .Should()
                .BeTrue();

        Behavior.CanUseSkill(aisling, skill)
                .Should()
                .BeTrue();

        Behavior.CanUseSpell(aisling, spell)
                .Should()
                .BeTrue();
    }
    #endregion

    #region Monster-side still works (nothing broken by the shared RestrictionBehavior change)
    [Test]
    public void CanMove_ShouldBeFalse_WhenMonsterRooted()
    {
        var monster = MockMonster.Create();
        monster.Trackers.Tags["rooted"] = bool.TrueString;

        Behavior.CanMove(monster)
                .Should()
                .BeFalse();
    }

    [Test]
    public void CanUseSkill_ShouldBeFalse_WhenMonsterBlackedOut()
    {
        var monster = MockMonster.Create();
        monster.Trackers.Tags["blackout"] = bool.TrueString;
        var skill = MockSkill.Create();

        Behavior.CanUseSkill(monster, skill)
                .Should()
                .BeFalse();
    }
    #endregion
}
