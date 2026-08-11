#region
using Chaos.Collections;
using Chaos.Common.Abstractions;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
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
using Chaos.Services.Factories.Abstractions;
using Chaos.Testing.Infrastructure.Harnesses;
using Chaos.Testing.Infrastructure.Mocks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
#endregion

namespace Chaos.Tests;

/// <summary>
///     Real functional tests for Assassin's new/reworked/evolving mechanics - asserts actual resulting
///     values/state, not just "doesn't throw", per this session's established standard.
/// </summary>
public sealed class AssassinNewSkillsTests
{
    static AssassinNewSkillsTests()
    {
        var registry = new FunctionalScriptRegistry(MockServiceProvider.CreateBuilder()
                                                                        .Build()
                                                                        .Object);

        registry.Register(ApplyAttackDamageScript.Key, typeof(ApplyAttackDamageScript));
    }

    private static void EnsureScriptVars(Skill skill, string scriptKey) => skill.Template.ScriptVars[scriptKey] = new EmptyScriptVars();

    [Test]
    public void DeathsStrike_ShouldDealBonusDamage_WhenInitiatingCombat()
    {
        //fresh monster has no prior aggro on the caster, so this hit "initiates combat". BaseDamage set directly
        //via scriptFactory (rather than relying on stat-scaling) since a fresh mock Aisling's DEX is 0.
        var initiatingHarness = new SkillScriptHarness<DeathsStrikeScript>(
            scriptFactory: skill => new DeathsStrikeScript(skill) { BaseDamage = 50, Range = 6 },
            skillSetup: s => EnsureScriptVars(s, "deathsStrike"));

        initiatingHarness.WithTargetMonster(m => m.StatSheet.SetHp(100000));
        initiatingHarness.Target!.WarpTo(initiatingHarness.Source.DirectionalOffset(initiatingHarness.Source.Direction));
        var initiatingHpBefore = initiatingHarness.Target.StatSheet.CurrentHp;
        initiatingHarness.Use();
        var initiatingDamage = initiatingHpBefore - initiatingHarness.Target.StatSheet.CurrentHp;

        //monster already has aggro on the caster, so this hit does NOT initiate combat, and the caster isn't hidden
        var repeatHarness = new SkillScriptHarness<DeathsStrikeScript>(
            scriptFactory: skill => new DeathsStrikeScript(skill) { BaseDamage = 50, Range = 6 },
            skillSetup: s => EnsureScriptVars(s, "deathsStrike"));

        repeatHarness.WithTargetMonster(m => m.StatSheet.SetHp(100000));
        repeatHarness.Target!.WarpTo(repeatHarness.Source.DirectionalOffset(repeatHarness.Source.Direction));

        if (repeatHarness.Target is Monster repeatMonster)
            repeatMonster.AggroList.AddAggro(repeatHarness.Source, 10);

        var repeatHpBefore = repeatHarness.Target.StatSheet.CurrentHp;
        repeatHarness.Use();
        var repeatDamage = repeatHpBefore - repeatHarness.Target.StatSheet.CurrentHp;

        initiatingDamage.Should()
                        .BeGreaterThan(repeatDamage, "Death's Strike deals bonus damage when it initiates combat");
    }

    [Test]
    public void DeathMark_ShouldHealNearbyAllies_WhenMarkedTargetDies()
    {
        var applyDamageScript = ApplyAttackDamageScript.Create();
        var map = MockMapInstance.Create();
        var assassin = MockAisling.Create(map);
        assassin.StatSheet.AddBonus(new Attributes { MaximumHp = 1000 });
        assassin.StatSheet.SetHp(500);
        map.AddEntity(assassin, Point.From(assassin));

        var monster = MockMonster.Create(map);
        var monsterPoint = Point.From(assassin);
        monster.WarpTo(monsterPoint);
        map.AddEntity(monster, monsterPoint);
        monster.StatSheet.SetHp(10);

        var mark = new DeathMarkEffect { HealAmount = 40, HealRadius = 5 };
        monster.Effects.Apply(assassin, mark, MockSkill.Create().Script);

        applyDamageScript.ApplyDamage(assassin, monster, MockSkill.Create().Script, 9999);

        assassin.StatSheet.CurrentHp
                .Should()
                .BeGreaterThan(500, "Death Mark should heal nearby allies (including the caster) when the marked target dies");
    }

    [Test]
    public void DeathMark_ShouldNotHeal_WhenTargetIsNotMarked()
    {
        var applyDamageScript = ApplyAttackDamageScript.Create();
        var map = MockMapInstance.Create();
        var assassin = MockAisling.Create(map);
        assassin.StatSheet.AddBonus(new Attributes { MaximumHp = 1000 });
        assassin.StatSheet.SetHp(500);
        map.AddEntity(assassin, Point.From(assassin));

        var monster = MockMonster.Create(map);
        var monsterPoint = Point.From(assassin);
        monster.WarpTo(monsterPoint);
        map.AddEntity(monster, monsterPoint);
        monster.StatSheet.SetHp(10);

        //no mark applied this time
        applyDamageScript.ApplyDamage(assassin, monster, MockSkill.Create().Script, 9999);

        assassin.StatSheet.CurrentHp
                .Should()
                .Be(500, "Death Mark's heal should not trigger on an unmarked kill");
    }

    [Test]
    public void Bloodlust_ShouldAutoTrigger_WhenKillEnergyReachesCap()
    {
        var harness = new AislingScriptHarness<AssassinFrenzyScript>();
        harness.Source.UserStatSheet.SetBaseClass(BaseClass.Assassin);

        var bloodlustSkill = MockSkill.Create(name: "Bloodlust", templateSetup: t => t with { TemplateKey = "bloodlust" });
        harness.Source.SkillBook.TryAddToNextSlot(bloodlustSkill);

        //5 kills at 20 energy each reaches the 100 hard cap and should auto-trigger Bloodlust
        for (var i = 0; i < 5; i++)
        {
            harness.Source.Trackers.LastKillTime = DateTime.UtcNow.AddMilliseconds(i);
            harness.Update(TimeSpan.FromMilliseconds(1));
        }

        harness.Source.Effects.Contains("Bloodlust")
               .Should()
               .BeTrue("Bloodlust should auto-trigger once banked kill energy reaches the hard cap");

        harness.Source.StatSheet.CurrentMp
               .Should()
               .Be(0, "all banked kill energy should be consumed when Bloodlust auto-triggers");
    }

    [Test]
    public void WitnessElimination_ShouldDealBonusDamage_ToIsolatedTarget()
    {
        var applyDamageScript = ApplyAttackDamageScript.Create();
        var assassin = MockAisling.Create();
        assassin.UserStatSheet.SetBaseClass(BaseClass.Assassin);

        var isolatedMap = MockMapInstance.Create();
        var isolatedMonster = MockMonster.Create(isolatedMap);
        isolatedMonster.StatSheet.AddBonus(new Attributes { MaximumHp = 10000 });
        isolatedMonster.StatSheet.SetHp(10000);
        AddEntityAtItsOwnPosition(isolatedMap, isolatedMonster);

        var crowdedMap = MockMapInstance.Create();
        var crowdedMonster = MockMonster.Create(crowdedMap);
        crowdedMonster.StatSheet.AddBonus(new Attributes { MaximumHp = 10000 });
        crowdedMonster.StatSheet.SetHp(10000);
        var crowdedPoint = Point.From(crowdedMonster);
        crowdedMap.AddEntity(crowdedMonster, crowdedPoint);

        var nearbyMonster = MockMonster.Create(crowdedMap);
        nearbyMonster.WarpTo(crowdedPoint);
        crowdedMap.AddEntity(nearbyMonster, crowdedPoint);

        var isolatedHpBefore = isolatedMonster.StatSheet.CurrentHp;
        applyDamageScript.ApplyDamage(assassin, isolatedMonster, MockSkill.Create().Script, 100);
        var isolatedDamage = isolatedHpBefore - isolatedMonster.StatSheet.CurrentHp;

        var crowdedHpBefore = crowdedMonster.StatSheet.CurrentHp;
        applyDamageScript.ApplyDamage(assassin, crowdedMonster, MockSkill.Create().Script, 100);
        var crowdedDamage = crowdedHpBefore - crowdedMonster.StatSheet.CurrentHp;

        isolatedDamage.Should()
                      .BeGreaterThan(crowdedDamage, "Witness Elimination should deal bonus damage against an isolated target");
    }

    private static void AddEntityAtItsOwnPosition(MapInstance map, Monster monster) => map.AddEntity(monster, Point.From(monster));

    [Test]
    public void Shadowmark_ShouldApplyMark_OnThirdHitAgainstTheSameTarget()
    {
        var applyDamageScript = ApplyAttackDamageScript.Create();
        var assassin = MockAisling.Create();
        assassin.UserStatSheet.SetBaseClass(BaseClass.Assassin);

        var monster = MockMonster.Create();
        monster.StatSheet.AddBonus(new Attributes { MaximumHp = 100000 });
        monster.StatSheet.SetHp(100000);

        applyDamageScript.ApplyDamage(assassin, monster, MockSkill.Create().Script, 50);
        applyDamageScript.ApplyDamage(assassin, monster, MockSkill.Create().Script, 50);

        monster.Trackers.Tags.ContainsKey(ShadowmarkEffect.OwnerIdTagPrefix + assassin.Id)
               .Should()
               .BeFalse("Shadowmark should not apply until the third hit");

        applyDamageScript.ApplyDamage(assassin, monster, MockSkill.Create().Script, 50);

        monster.Trackers.Tags.ContainsKey(ShadowmarkEffect.OwnerIdTagPrefix + assassin.Id)
               .Should()
               .BeTrue("Shadowmark should apply on the third damaging ability against the same target");
    }

    [Test]
    public void GhostStep_ShouldHideCasterAndBreak_OnNextLandedHit()
    {
        var applyDamageScript = ApplyAttackDamageScript.Create();
        var harness = new SkillScriptHarness<GhostStepScript>(skillSetup: s => EnsureScriptVars(s, "ghostStep"));

        harness.Use();

        harness.Source.Visibility
               .Should()
               .Be(VisibilityType.Hidden, "Ghost Step should hide the caster");

        harness.Source.Trackers.Tags.ContainsKey(GhostStepEffect.ReadyTag)
               .Should()
               .BeTrue();

        var monster = MockMonster.Create(harness.Map);
        monster.StatSheet.AddBonus(new Attributes { MaximumHp = 10000 });
        monster.StatSheet.SetHp(10000);

        applyDamageScript.ApplyDamage(harness.Source, monster, MockSkill.Create().Script, 100);

        harness.Source.Trackers.Tags.ContainsKey(GhostStepEffect.ReadyTag)
               .Should()
               .BeFalse("the ready tag should be consumed on the next landed hit");

        harness.Source.Visibility
               .Should()
               .Be(VisibilityType.Normal, "Ghost Step should break once its bonus is consumed");
    }

    [Test]
    public void KillingIntent_ShouldGrantBonusDamage_OnNextDamagingAbility_ThenBreak()
    {
        var applyDamageScript = ApplyAttackDamageScript.Create();

        var buffedAssassin = MockAisling.Create();
        buffedAssassin.Effects.Apply(buffedAssassin, new KillingIntentEffect(), MockSkill.Create().Script);

        var plainAssassin = MockAisling.Create();

        var buffedMonster = MockMonster.Create();
        buffedMonster.StatSheet.AddBonus(new Attributes { MaximumHp = 10000 });
        buffedMonster.StatSheet.SetHp(10000);

        var plainMonster = MockMonster.Create();
        plainMonster.StatSheet.AddBonus(new Attributes { MaximumHp = 10000 });
        plainMonster.StatSheet.SetHp(10000);

        var buffedHpBefore = buffedMonster.StatSheet.CurrentHp;
        applyDamageScript.ApplyDamage(buffedAssassin, buffedMonster, MockSkill.Create().Script, 100);
        var buffedDamage = buffedHpBefore - buffedMonster.StatSheet.CurrentHp;

        var plainHpBefore = plainMonster.StatSheet.CurrentHp;
        applyDamageScript.ApplyDamage(plainAssassin, plainMonster, MockSkill.Create().Script, 100);
        var plainDamage = plainHpBefore - plainMonster.StatSheet.CurrentHp;

        buffedDamage.Should()
                    .BeGreaterThan(plainDamage, "Killing Intent should empower the next damaging ability");

        buffedAssassin.Trackers.Tags.ContainsKey(KillingIntentEffect.ReadyTag)
                      .Should()
                      .BeFalse("Killing Intent should break once its bonus is consumed");
    }

    [Test]
    public void PhantomBlade_ShouldApplyLingeringShadowWound()
    {
        var harness = new SkillScriptHarness<PhantomBladeScript>(skillSetup: s => EnsureScriptVars(s, "phantomBlade"));
        harness.WithTargetMonster(m => m.StatSheet.SetHp(100000));
        harness.Target!.WarpTo(harness.Source.DirectionalOffset(harness.Source.Direction));

        harness.Use();

        harness.Target.Effects.TryGetEffect("Shadow Wound", out _)
               .Should()
               .BeTrue();

        var hpAfterHit = harness.Target.StatSheet.CurrentHp;
        harness.Target.Effects.Update(TimeSpan.FromMilliseconds(1100));

        harness.Target.StatSheet.CurrentHp
               .Should()
               .BeLessThan(hpAfterHit, "the shadow wound should tick damage over time");
    }

    [Test]
    public void DeathsConviction_ShouldSacrificeCasterHp_AndDealDamageBasedOnTargetHp()
    {
        var harness = new SkillScriptHarness<DeathsConvictionScript>(
            skillSetup: s =>
            {
                s.Level = 1;
                EnsureScriptVars(s, "deathsConviction");
            });

        harness.Source.StatSheet.AddBonus(new Attributes { MaximumHp = 1000 });
        harness.Source.StatSheet.SetHp(1000);

        harness.WithTargetMonster(
            m =>
            {
                m.StatSheet.AddBonus(new Attributes { MaximumHp = 1000 });
                m.StatSheet.SetHp(1000);
            });

        harness.Target!.WarpTo(harness.Source.DirectionalOffset(harness.Source.Direction));

        var casterHpBefore = harness.Source.StatSheet.CurrentHp;
        var targetHpBefore = harness.Target.StatSheet.CurrentHp;

        harness.Use();

        harness.Source.StatSheet.CurrentHp
               .Should()
               .BeLessThan(casterHpBefore, "Death's Conviction sacrifices a portion of the caster's own HP");

        harness.Target.StatSheet.CurrentHp
               .Should()
               .BeLessThan(targetHpBefore, "Death's Conviction deals damage based on the target's current HP");
    }

    [Test]
    public void DeathsConviction_ShouldGrantShield_AtTierIV()
    {
        var harness = new SkillScriptHarness<DeathsConvictionScript>(
            skillSetup: s =>
            {
                s.Level = 20;
                EnsureScriptVars(s, "deathsConviction");
            });

        harness.Source.StatSheet.AddBonus(new Attributes { MaximumHp = 1000 });
        harness.Source.StatSheet.SetHp(1000);
        harness.WithTargetMonster(m => m.StatSheet.SetHp(1000));
        harness.Target!.WarpTo(harness.Source.DirectionalOffset(harness.Source.Direction));

        harness.Use();

        harness.Source.Trackers.Counters.TryGetValue(DeathsConvictionShieldEffect.ShieldCounter, out _)
               .Should()
               .BeTrue("Tier IV Death's Conviction should grant a brief survivability shield");
    }

    [Test]
    public void SpectralWraith_ShouldStrikeMoreTargets_AtHigherTiers()
    {
        //relative comparison (Tier I's 2-target cap vs Tier III's 8-target cap, both scanning the same 4 nearby
        //monsters) rather than asserting an exact count against a specific tier - keeps the test robust to exactly
        //how many of the available monsters a given tier's cap allows through
        var lowTierHitCount = StrikeAndCountHits(level: 1);
        var highTierHitCount = StrikeAndCountHits(level: 18);

        highTierHitCount.Should()
                        .BeGreaterThan(lowTierHitCount, "Spectral Wraith should strike more targets at higher tiers");

        static int StrikeAndCountHits(byte level)
        {
            var harness = new SkillScriptHarness<SpectralWraithScript>(
                scriptFactory: skill => new SpectralWraithScript(skill) { BaseDamage = 30, Range = 5 },
                skillSetup: s =>
                {
                    s.Level = level;
                    EnsureScriptVars(s, "spectralWraith");
                });

            var sourcePoint = Point.From(harness.Source);

            //distinct nearby points, not the caster's own tile - warping a target onto the caster's exact tile
            //makes DirectionalRelationTo (used to materialize behind the final target) undefined
            var offsets = new[]
            {
                new Point(sourcePoint.X + 1, sourcePoint.Y),
                new Point(sourcePoint.X + 2, sourcePoint.Y),
                new Point(sourcePoint.X - 1, sourcePoint.Y),
                new Point(sourcePoint.X - 2, sourcePoint.Y)
            };

            var monsters = offsets.Select(_ => MockMonster.Create(harness.Map)).ToArray();

            for (var i = 0; i < monsters.Length; i++)
            {
                monsters[i].StatSheet.SetHp(10000);
                monsters[i].WarpTo(offsets[i]);
                harness.Map.AddEntity(monsters[i], offsets[i]);
            }

            harness.Use();

            return monsters.Count(m => m.StatSheet.CurrentHp < 10000);
        }
    }

    [Test]
    public void Eclipse_ShouldHitTargetMultipleTimes()
    {
        //5-hit flurry (default HitCount) vs a HitCount=1 baseline, to prove multiple hits actually landed rather
        //than one big one - relative comparison since AC mitigation applies per-hit, not to a hardcoded total
        var flurryHarness = new SkillScriptHarness<EclipseScript>(
            scriptFactory: skill => new EclipseScript(skill) { HitCount = 5, BaseDamage = 20 },
            skillSetup: s => EnsureScriptVars(s, "eclipse"));

        flurryHarness.WithTargetMonster(m => m.StatSheet.SetHp(100000));
        flurryHarness.Target!.WarpTo(flurryHarness.Source.DirectionalOffset(flurryHarness.Source.Direction));

        var flurryHpBefore = flurryHarness.Target.StatSheet.CurrentHp;
        flurryHarness.Use();
        var flurryDamage = flurryHpBefore - flurryHarness.Target.StatSheet.CurrentHp;

        var singleHitHarness = new SkillScriptHarness<EclipseScript>(
            scriptFactory: skill => new EclipseScript(skill) { HitCount = 1, BaseDamage = 20 },
            skillSetup: s => EnsureScriptVars(s, "eclipse"));

        singleHitHarness.WithTargetMonster(m => m.StatSheet.SetHp(100000));
        singleHitHarness.Target!.WarpTo(singleHitHarness.Source.DirectionalOffset(singleHitHarness.Source.Direction));

        var singleHpBefore = singleHitHarness.Target.StatSheet.CurrentHp;
        singleHitHarness.Use();
        var singleDamage = singleHpBefore - singleHitHarness.Target.StatSheet.CurrentHp;

        flurryDamage.Should()
                    .BeGreaterThan(singleDamage, "Eclipse's flurry should deal more total damage than a single hit");
    }

    [Test]
    public void ShadowClone_ShouldSpawnTwoClones_AtTierIII()
    {
        //the harness's own map isn't known until the harness is constructed, but the mock factory needs to spawn
        //clones onto that same map (not a separate one the caster was never placed on) - captured via a mutable
        //closure variable assigned right after construction, same map the caster already lives on
        MapInstance? map = null;
        var monsterFactoryMock = new Mock<IMonsterFactory>();

        monsterFactoryMock
            .Setup(
                f => f.Create(
                    It.IsAny<string>(),
                    It.IsAny<MapInstance>(),
                    It.IsAny<Chaos.Geometry.Abstractions.IPoint>(),
                    It.IsAny<ICollection<string>?>()))
            .Returns(() => MockMonster.Create(map!, templateSetup: t => t with { TemplateKey = "shadow_clone" }));

        var serviceProvider = MockServiceProvider.CreateBuilder()
                                                  .SetupService(monsterFactoryMock.Object)
                                                  .Build()
                                                  .Object;

        var harness = new SkillScriptHarness<ShadowCloneScript>(
            skillSetup: s =>
            {
                s.Level = 12;
                EnsureScriptVars(s, "shadowClone");
            },
            serviceProvider: serviceProvider);

        map = harness.Map;

        var cloneCountBefore = map.GetEntities<Monster>()
                                  .Count();

        harness.Use();

        var cloneCountAfter = map.GetEntities<Monster>()
                                 .Count();

        (cloneCountAfter - cloneCountBefore).Should()
                                             .Be(2, "Tier III Shadow Clone should spawn 2 clones");
    }

    [Test]
    public void Execute_ShouldUseAHigherThreshold_AtHigherTiers()
    {
        //target sitting at 22% HP: below Tier IV's 30% threshold (executed, huge damage) but above Tier I's 15%
        //threshold (normal damage only) - relative comparison, not an exact value, since the real pipeline applies
        //AC mitigation on top of both paths (same confound documented for Ragnarok/Divine Verdict this session)
        var lowTierHarness = new SkillScriptHarness<ExecuteScript>(
            skillSetup: s =>
            {
                s.Level = 1;
                EnsureScriptVars(s, "execute");
            });

        lowTierHarness.WithTargetMonster(
            m =>
            {
                m.StatSheet.AddBonus(new Attributes { MaximumHp = 1000 });
                m.StatSheet.SetHp(220);
            });

        lowTierHarness.Target!.WarpTo(lowTierHarness.Source.DirectionalOffset(lowTierHarness.Source.Direction));
        var lowHpBefore = lowTierHarness.Target.StatSheet.CurrentHp;
        lowTierHarness.Use();
        var lowTierDamage = lowHpBefore - lowTierHarness.Target.StatSheet.CurrentHp;

        var highTierHarness = new SkillScriptHarness<ExecuteScript>(
            skillSetup: s =>
            {
                s.Level = 20;
                EnsureScriptVars(s, "execute");
            });

        highTierHarness.WithTargetMonster(
            m =>
            {
                m.StatSheet.AddBonus(new Attributes { MaximumHp = 1000 });
                m.StatSheet.SetHp(220);
            });

        highTierHarness.Target!.WarpTo(highTierHarness.Source.DirectionalOffset(highTierHarness.Source.Direction));
        var highHpBefore = highTierHarness.Target.StatSheet.CurrentHp;
        highTierHarness.Use();
        var highTierDamage = highHpBefore - highTierHarness.Target.StatSheet.CurrentHp;

        highTierDamage.Should()
                      .BeGreaterThan(lowTierDamage, "a target at 22% HP should only be executed at the higher-tier threshold");
    }

    [Test]
    public void VanishingSlash_ShouldReturnCasterToHide_AfterTheStrike()
    {
        var harness = new SkillScriptHarness<VanishingSlashScript>(
            scriptFactory: skill => new VanishingSlashScript(skill, new Mock<ILogger<VanishingSlashScript>>().Object) { BaseDamage = 50, Range = 6 },
            skillSetup: s => EnsureScriptVars(s, "vanishingSlash"));

        harness.WithTargetMonster();
        harness.Target!.WarpTo(harness.Source.DirectionalOffset(harness.Source.Direction));

        harness.Use();

        harness.Source.Visibility
               .Should()
               .Be(VisibilityType.Hidden, "Vanishing Slash should return the caster to Hide after the strike");
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
