#region
using Chaos.DarkAges.Definitions;
using Chaos.Scripting.AislingScripts;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Testing.Infrastructure.Harnesses;
using Chaos.Testing.Infrastructure.Mocks;
using FluentAssertions;
#endregion

namespace Chaos.Tests;

/// <summary>
///     Functional tests for Berserker's 3 passives, simulating real gameplay (an actual attack through
///     ApplyAttackDamageScript, not just constructing the script and asserting on its internals) per the explicit
///     ask to confirm these are mechanically working for a live Berserker character, not just that the code exists.
/// </summary>
public sealed class BerserkerPassivesTests
{
    private static readonly ApplyAttackDamageScript DamageScript = new();

    private static IServiceProvider CreateServiceProviderWithLogger()
        => MockServiceProvider.CreateBuilder()
                              .SetupService(MockLogger.Create<BerserkerRageScript>().Object)
                              .Build()
                              .Object;

    [Test]
    public void Rage_ShouldGenerateOnHit_AndApplyDamageBonus_ForBerserker()
    {
        var harness = new AislingScriptHarness<BerserkerRageScript>(serviceProvider: CreateServiceProviderWithLogger());
        var berserker = harness.Source;
        berserker.UserStatSheet.SetBaseClass(BaseClass.Berserker);

        var target = MockMonster.Create(harness.Map, name: "TestAscensionBoss", setup: m => m.StatSheet.SetHp(500));

        berserker.StatSheet.CurrentMp
                 .Should()
                 .Be(0, "a freshly-created Berserker should start with no rage");

        //simulate 4 real attacks - each ApplyDamage call goes through the actual production code path
        //(ApplyAttackDamageScript.ApplyDamage), setting Trackers.LastDamagedEnemy exactly like a live hit would
        for (var i = 0; i < 4; i++)
        {
            DamageScript.ApplyDamage(berserker, target, harness.Script, 50);
            harness.Update(TimeSpan.FromMilliseconds(1));
        }

        berserker.StatSheet.CurrentMp
                 .Should()
                 .Be(20, "4 hits at 5 rage/hit (RagePerHit) should generate 20 rage");

        berserker.StatSheet.EffectiveFlatSkillDamage
                 .Should()
                 .Be(10, "20 rage * 0.5 damage-bonus-per-mp (RageDamageBonusPerMp) should grant +10 flat skill damage");
    }

    [Test]
    public void Rage_ShouldNotGenerate_ForNonBerserker()
    {
        var harness = new AislingScriptHarness<BerserkerRageScript>(serviceProvider: CreateServiceProviderWithLogger());
        var nonBerserker = harness.Source;
        nonBerserker.UserStatSheet.SetBaseClass(BaseClass.Sorcerer);

        var target = MockMonster.Create(harness.Map, name: "TestAscensionBoss", setup: m => m.StatSheet.SetHp(500));

        DamageScript.ApplyDamage(nonBerserker, target, harness.Script, 50);
        harness.Update(TimeSpan.FromMilliseconds(1));

        nonBerserker.StatSheet.CurrentMp
                    .Should()
                    .Be(0, "Rage is Berserker-only - a non-Berserker landing hits should not accumulate it");
    }

    [Test]
    public void Unbroken_ShouldSurviveLethalHit_AtOneHp_WhenArmed()
    {
        var harness = new AislingScriptHarness<BerserkerUnbrokenScript>();
        var berserker = harness.Source;
        berserker.UserStatSheet.SetBaseClass(BaseClass.Berserker);
        berserker.StatSheet.SetHp(100);

        var attacker = MockMonster.Create(harness.Map, name: "Attacker");

        //arms almost immediately (SinceLastArm starts pre-primed) - one Update call is enough
        harness.Update(TimeSpan.FromMilliseconds(1));

        berserker.Trackers.Tags
                 .Should()
                 .ContainKey(BerserkerUnbrokenScript.ReadyTag, "the passive should auto-arm shortly after becoming active");

        //a hit for more than current HP would normally kill - Unbroken should intercept it
        DamageScript.ApplyDamage(attacker, berserker, harness.Script, 9999);

        berserker.StatSheet.CurrentHp
                 .Should()
                 .Be(1, "Unbroken should have saved the Berserker at 1 HP instead of letting the hit kill them");

        berserker.Trackers.Tags
                 .Should()
                 .NotContainKey(BerserkerUnbrokenScript.ReadyTag, "the save should consume the ready tag");
    }

    [Test]
    public void Carnage_ShouldIncreaseDamageBonus_AsHealthDrops()
    {
        var harness = new AislingScriptHarness<BerserkerCarnageScript>();
        var berserker = harness.Source;
        berserker.UserStatSheet.SetBaseClass(BaseClass.Berserker);
        berserker.StatSheet.AddBonus(new Chaos.Models.Data.Attributes { MaximumHp = 1000 });
        berserker.StatSheet.SetHp(1000);

        harness.Update(TimeSpan.FromMilliseconds(1));

        berserker.StatSheet.EffectiveSkillDamagePct
                 .Should()
                 .Be(0, "no bonus above the 50% HP threshold");

        //drop to 10% hp
        berserker.StatSheet.SetHp(100);
        harness.Update(TimeSpan.FromMilliseconds(1));

        berserker.StatSheet.EffectiveSkillDamagePct
                 .Should()
                 .BeGreaterThan(0, "below the threshold, missing HP should grant a skill damage bonus");

        //drop further to near-death
        berserker.StatSheet.SetHp(10);
        harness.Update(TimeSpan.FromMilliseconds(1));

        var nearDeathBonus = berserker.StatSheet.EffectiveSkillDamagePct;

        nearDeathBonus.Should()
                      .BeGreaterThan(0)
                      .And.BeLessThanOrEqualTo(40, "bonus is capped at MaxBonusPct");
    }
}
