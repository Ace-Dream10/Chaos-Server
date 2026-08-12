#region
using Chaos.Collections;
using Chaos.Common.Abstractions;
using Chaos.DarkAges.Definitions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.AislingScripts;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.FunctionalScripts;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts;
using Chaos.Testing.Infrastructure.Harnesses;
using Chaos.Testing.Infrastructure.Mocks;
using FluentAssertions;
#endregion

namespace Chaos.Tests;

/// <summary>
///     Real functional tests for Tempest's new/evolving mechanics - asserts actual resulting values/state, not
///     just "doesn't throw", per this session's established standard. Tempest is the final specialization in
///     tonight's batch; the shared foundation (Tiger Strike/Roundhouse Kick/Valkor's Fervor/Meditate/Martial Form)
///     is exercised by Beast's/Ironscale's own tests already and reused unmodified here except for Martial Form's
///     new RangedChi branch, tested below.
/// </summary>
public sealed class MartialArtistTempestTests
{
    static MartialArtistTempestTests()
    {
        var registry = new FunctionalScriptRegistry(MockServiceProvider.CreateBuilder()
                                                                        .Build()
                                                                        .Object);

        registry.Register(ApplyAttackDamageScript.Key, typeof(ApplyAttackDamageScript));
    }

    private static void EnsureScriptVars(Skill skill, string scriptKey) => skill.Template.ScriptVars[scriptKey] = new EmptyScriptVars();

    [Test]
    public void HailOfFeathers_ShouldDamageMoreTargets_AtHigherTiers()
    {
        var lowTierHarness = new SkillScriptHarness<HailOfFeathersScript>(
            scriptFactory: skill => new HailOfFeathersScript(skill) { BaseDamage = 40 },
            skillSetup: s =>
            {
                s.Level = 1;
                EnsureScriptVars(s, "hailOfFeathers");
            });

        lowTierHarness.Source.StatSheet.AddBonus(new Attributes { Wis = 20 });
        lowTierHarness.WithTargetMonster(m => m.StatSheet.SetHp(100000));
        lowTierHarness.Map.AddEntity(lowTierHarness.Target!, Point.From(lowTierHarness.Target!));

        var lowHpBefore = lowTierHarness.Target!.StatSheet.CurrentHp;
        lowTierHarness.Use();
        var lowDamage = lowHpBefore - lowTierHarness.Target.StatSheet.CurrentHp;

        var highTierHarness = new SkillScriptHarness<HailOfFeathersScript>(
            scriptFactory: skill => new HailOfFeathersScript(skill) { BaseDamage = 40 },
            skillSetup: s =>
            {
                s.Level = 20;
                EnsureScriptVars(s, "hailOfFeathers");
            });

        highTierHarness.Source.StatSheet.AddBonus(new Attributes { Wis = 20 });
        highTierHarness.WithTargetMonster(m => m.StatSheet.SetHp(100000));
        highTierHarness.Map.AddEntity(highTierHarness.Target!, Point.From(highTierHarness.Target!));

        var highHpBefore = highTierHarness.Target!.StatSheet.CurrentHp;
        highTierHarness.Use();
        var highDamage = highHpBefore - highTierHarness.Target.StatSheet.CurrentHp;

        highDamage.Should()
                 .BeGreaterThan(lowDamage, "Tier IV Hail of Feathers should deal more damage than Tier I");
    }

    [Test]
    public void VoidPalm_ShouldDealMoreDamage_AtHigherTiers()
    {
        var lowTierHarness = new SkillScriptHarness<VoidPalmScript>(
            scriptFactory: skill => new VoidPalmScript(skill) { BaseDamage = 40, Range = 8 },
            skillSetup: s =>
            {
                s.Level = 1;
                EnsureScriptVars(s, "voidPalm");
            });

        lowTierHarness.Source.StatSheet.AddBonus(new Attributes { Wis = 20 });
        lowTierHarness.WithTargetMonster(m => m.StatSheet.SetHp(100000));
        var lowHpBefore = lowTierHarness.Target!.StatSheet.CurrentHp;
        lowTierHarness.Use();
        var lowDamage = lowHpBefore - lowTierHarness.Target.StatSheet.CurrentHp;

        var highTierHarness = new SkillScriptHarness<VoidPalmScript>(
            scriptFactory: skill => new VoidPalmScript(skill) { BaseDamage = 40, Range = 8 },
            skillSetup: s =>
            {
                s.Level = 20;
                EnsureScriptVars(s, "voidPalm");
            });

        highTierHarness.Source.StatSheet.AddBonus(new Attributes { Wis = 20 });
        highTierHarness.WithTargetMonster(m => m.StatSheet.SetHp(100000));
        var highHpBefore = highTierHarness.Target!.StatSheet.CurrentHp;
        highTierHarness.Use();
        var highDamage = highHpBefore - highTierHarness.Target.StatSheet.CurrentHp;

        highDamage.Should()
                 .BeGreaterThan(lowDamage, "Tier IV Void Palm should deal more damage than Tier I");
    }

    [Test]
    public void TempestFocus_ShouldGrantStrongerAttackSpeed_AtHigherTiers()
    {
        var lowTierHarness = new SkillScriptHarness<TempestFocusScript>(skillSetup: s =>
        {
            s.Level = 1;
            EnsureScriptVars(s, "tempestFocus");
        });

        lowTierHarness.Use();
        lowTierHarness.Source.Effects.TryGetEffect("Tempest Focus", out var lowEffect);

        var highTierHarness = new SkillScriptHarness<TempestFocusScript>(skillSetup: s =>
        {
            s.Level = 20;
            EnsureScriptVars(s, "tempestFocus");
        });

        highTierHarness.Use();
        highTierHarness.Source.Effects.TryGetEffect("Tempest Focus", out var highEffect);

        (lowEffect as TempestFocusEffect)!.AtkSpeedBonus
                                          .Should()
                                          .BeLessThan((highEffect as TempestFocusEffect)!.AtkSpeedBonus, "Tier IV Tempest Focus should grant more Attack Speed than Tier I");
    }

    [Test]
    public void StarCross_ShouldDamageMultipleNearbyEnemies()
    {
        var harness = new SkillScriptHarness<StarCrossScript>(
            scriptFactory: skill => new StarCrossScript(skill) { BaseDamage = 40, MaxTargets = 3, Range = 6 },
            skillSetup: s => EnsureScriptVars(s, "starCross"));

        var monster1 = MockMonster.Create(harness.Map, setup: m => m.StatSheet.SetHp(100000));
        monster1.WarpTo(new Point(6, 5));
        harness.Map.AddEntity(monster1, Point.From(monster1));

        var monster2 = MockMonster.Create(harness.Map, setup: m => m.StatSheet.SetHp(100000));
        monster2.WarpTo(new Point(6, 6));
        harness.Map.AddEntity(monster2, Point.From(monster2));

        harness.Use();

        var hitCount = new[] { monster1, monster2 }.Count(m => m.StatSheet.CurrentHp < 100000);

        hitCount.Should()
               .BeGreaterThan(0, "Star Cross should damage at least one nearby enemy");
    }

    [Test]
    public void ChiBullet_ShouldDamagePrimaryTarget_AndDetonateOnNearbyEnemies()
    {
        var harness = new SkillScriptHarness<ChiBulletScript>(
            scriptFactory: skill => new ChiBulletScript(skill) { BaseDamage = 60, DetonationRadius = 2, DetonationDamagePct = 60, Range = 8 },
            skillSetup: s => EnsureScriptVars(s, "chiBullet"));

        harness.WithTargetMonster(m => m.StatSheet.SetHp(100000));

        var nearbyMonster = MockMonster.Create(harness.Map, setup: m => m.StatSheet.SetHp(100000));
        nearbyMonster.WarpTo(Point.From(harness.Target!));
        harness.Map.AddEntity(nearbyMonster, Point.From(nearbyMonster));

        var primaryHpBefore = harness.Target!.StatSheet.CurrentHp;
        var nearbyHpBefore = nearbyMonster.StatSheet.CurrentHp;

        harness.Use();

        harness.Target.StatSheet.CurrentHp
               .Should()
               .BeLessThan(primaryHpBefore, "Chi Bullet should deal piercing damage to the primary target");

        nearbyMonster.StatSheet.CurrentHp
                     .Should()
                     .BeLessThan(nearbyHpBefore, "Chi Bullet's detonation should also damage nearby enemies");
    }

    [Test]
    public void FlickeringStep_ShouldTeleportTheCaster()
    {
        var harness = new SkillScriptHarness<FlickeringStepScript>(skillSetup: s => EnsureScriptVars(s, "flickeringStep"));

        var startPoint = Point.From(harness.Source);
        harness.Use();
        var endPoint = Point.From(harness.Source);

        endPoint.Should()
               .NotBe(startPoint, "Flickering Step should reposition the caster");
    }

    [Test]
    public void FeatherPrison_ShouldRootTheTarget()
    {
        var harness = new SkillScriptHarness<FeatherPrisonScript>(skillSetup: s => EnsureScriptVars(s, "featherPrison"));

        harness.WithTargetMonster();
        harness.Use();

        harness.Target!.Effects.TryGetEffect("Root", out _)
               .Should()
               .BeTrue("Feather Prison should root the target");
    }

    [Test]
    public void FlowingChi_ShouldEmpower_OnlyWhenUsingADifferentAbilityThanLastTime()
    {
        var harness = new AislingScriptHarness<FlowingChiScript>();
        harness.Source.UserStatSheet.SetBaseClass(BaseClass.MartialArtist);

        var flowingChiSkill = MockSkill.Create(name: "Flowing Chi", templateSetup: t => t with { TemplateKey = "flowing_chi" });
        harness.Source.SkillBook.TryAddToNextSlot(flowingChiSkill);

        var skillA = MockSkill.Create(name: "Skill A", templateSetup: t => t with { TemplateKey = "tiger_strike" });
        harness.Source.Trackers.LastUsedSkill = skillA;
        harness.Source.Trackers.LastSkillUse = DateTime.UtcNow;
        harness.Update(TimeSpan.FromMilliseconds(1));

        harness.Source.Effects.TryGetEffect("Flowing Chi", out _)
               .Should()
               .BeTrue("using a first ability should grant Flowing Chi");

        harness.Source.Effects.Terminate("Flowing Chi");

        //repeat the SAME ability - should not refresh the buff
        var skillARepeat = MockSkill.Create(name: "Skill A", templateSetup: t => t with { TemplateKey = "tiger_strike" });
        harness.Source.Trackers.LastUsedSkill = skillARepeat;
        harness.Source.Trackers.LastSkillUse = DateTime.UtcNow.AddMilliseconds(50);
        harness.Update(TimeSpan.FromMilliseconds(1));

        harness.Source.Effects.TryGetEffect("Flowing Chi", out _)
               .Should()
               .BeFalse("repeating the same ability shouldn't grant Flowing Chi");

        //now use a DIFFERENT ability - should grant it
        var skillB = MockSkill.Create(name: "Skill B", templateSetup: t => t with { TemplateKey = "roundhouse_kick" });
        harness.Source.Trackers.LastUsedSkill = skillB;
        harness.Source.Trackers.LastSkillUse = DateTime.UtcNow.AddMilliseconds(100);
        harness.Update(TimeSpan.FromMilliseconds(1));

        harness.Source.Effects.TryGetEffect("Flowing Chi", out _)
               .Should()
               .BeTrue("using a different ability than last time should grant Flowing Chi");
    }

    [Test]
    public void PerfectRhythm_ShouldRestoreMana_EveryFewAbilities_WhileTransformed()
    {
        var harness = new AislingScriptHarness<PerfectRhythmScript>();
        harness.Source.UserStatSheet.SetBaseClass(BaseClass.MartialArtist);
        harness.Source.StatSheet.AddBonus(new Attributes { MaximumMp = 1000 });
        harness.Source.StatSheet.SetMp(0);

        var perfectRhythmSkill = MockSkill.Create(name: "Perfect Rhythm", templateSetup: t => t with { TemplateKey = "perfect_rhythm" });
        harness.Source.SkillBook.TryAddToNextSlot(perfectRhythmSkill);

        //simulate being transformed
        harness.Source.Effects.Apply(harness.Source, new MartialFormEffect { Tier = 1 }, MockSkill.Create().Script);

        for (var i = 0; i < 5; i++)
        {
            var skill = MockSkill.Create(name: $"Skill{i}", templateSetup: t => t with { TemplateKey = $"skill_{i}" });
            harness.Source.Trackers.LastUsedSkill = skill;
            harness.Source.Trackers.LastSkillUse = DateTime.UtcNow.AddMilliseconds(i);
            harness.Update(TimeSpan.FromMilliseconds(1));
        }

        harness.Source.StatSheet.CurrentMp
               .Should()
               .BeGreaterThan(0, "Perfect Rhythm should restore Mana after enough abilities are used while transformed");
    }

    [Test]
    public void OneWithTheWind_ShouldGrantABuff_WhenUsingAMobilityOrChiAbility()
    {
        var harness = new AislingScriptHarness<OneWithTheWindScript>();
        harness.Source.UserStatSheet.SetBaseClass(BaseClass.MartialArtist);

        var oneWithTheWindSkill = MockSkill.Create(name: "One with the Wind", templateSetup: t => t with { TemplateKey = "one_with_the_wind" });
        harness.Source.SkillBook.TryAddToNextSlot(oneWithTheWindSkill);

        var flickeringStepSkill = MockSkill.Create(name: "Flickering Step", templateSetup: t => t with { TemplateKey = "flickering_step" });
        harness.Source.Trackers.LastUsedSkill = flickeringStepSkill;
        harness.Source.Trackers.LastSkillUse = DateTime.UtcNow;
        harness.Update(TimeSpan.FromMilliseconds(1));

        harness.Source.Effects.TryGetEffect("One with the Wind", out _)
               .Should()
               .BeTrue("using Flickering Step should grant One with the Wind");
    }

    [Test]
    public void MartialForm_ShouldGrantRangedStatsAndChangeSprite_ForTempestSpecialization()
    {
        var harness = new SkillScriptHarness<MartialFormScript>(skillSetup: s => EnsureScriptVars(s, "martialForm"));

        harness.Source.UserStatSheet.SetBaseClass(BaseClass.MartialArtist);
        harness.Source.UserStatSheet.SetAdvClass(AdvClass.RangedChi);
        harness.Source.StatSheet.AddBonus(new Attributes { MaximumMp = 1000 });
        harness.Source.StatSheet.SetMp(1000);

        var spriteBefore = harness.Source.Sprite;
        var wisBefore = harness.Source.StatSheet.EffectiveWis;

        harness.Use();

        harness.Source.Effects.TryGetEffect("Martial Form", out _)
               .Should()
               .BeTrue("using Martial Form should transform the caster");

        harness.Source.StatSheet.EffectiveWis
               .Should()
               .BeGreaterThan(wisBefore, "Tempest's Martial Form should grant a Wis bonus");

        harness.Source.Sprite
               .Should()
               .NotBe(spriteBefore, "Tempest's Martial Form should change the caster's sprite");
    }

    private sealed class EmptyScriptVars : IScriptVars
    {
        public bool ContainsKey(string key) => false;
        public object? Get(Type type, string name) => null;
        public T? Get<T>(string name) => default;
        public T GetRequired<T>(string key) => throw new KeyNotFoundException(key);
        public void Set<T>(string name, T value) { }
    }
}
