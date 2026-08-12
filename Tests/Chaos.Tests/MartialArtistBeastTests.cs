#region
using Chaos.Collections;
using Chaos.Common.Abstractions;
using Chaos.DarkAges.Definitions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.FunctionalScripts;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.FunctionalScripts.ApplyHealing;
using Chaos.Scripting.SkillScripts;
using Chaos.Testing.Infrastructure.Harnesses;
using Chaos.Testing.Infrastructure.Mocks;
using FluentAssertions;
#endregion

namespace Chaos.Tests;

/// <summary>
///     Real functional tests for Martial Artist's shared foundation and Beast's new/reworked/evolving mechanics -
///     asserts actual resulting values/state, not just "doesn't throw", per this session's established standard.
/// </summary>
public sealed class MartialArtistBeastTests
{
    static MartialArtistBeastTests()
    {
        var registry = new FunctionalScriptRegistry(MockServiceProvider.CreateBuilder()
                                                                        .Build()
                                                                        .Object);

        registry.Register(ApplyAttackDamageScript.Key, typeof(ApplyAttackDamageScript));
        registry.Register(ApplyHealScript.Key, typeof(ApplyHealScript));
    }

    private static void EnsureScriptVars(Skill skill, string scriptKey) => skill.Template.ScriptVars[scriptKey] = new EmptyScriptVars();

    [Test]
    public void AlphaStrike_ShouldLandNearTheFinalTarget_NotReturnToTheStartingPosition()
    {
        var harness = new SkillScriptHarness<AlphaStrikeScript>(
            scriptFactory: skill => new AlphaStrikeScript(skill) { BaseDamage = 80, Range = 5 },
            skillSetup: s =>
            {
                s.Level = 20;
                EnsureScriptVars(s, "alphaStrike");
            });

        //AlphaStrikeScript.Update checks pending.Source.IsAlive (IsAlive => CurrentHp > 0) before applying each
        //delayed hit - a freshly-created MockAisling has no HP configured by default, which is fine for tests that
        //apply damage synchronously, but this script's later blinks are processed through Update(), so the source
        //needs real HP for those hits to actually land
        harness.Source.StatSheet.AddBonus(new Attributes { MaximumHp = 100000 });
        harness.Source.StatSheet.SetHp(100000);

        var startPoint = Point.From(harness.Source);

        var target1 = MockMonster.Create(harness.Map);
        target1.WarpTo(new Point(startPoint.X + 2, startPoint.Y));
        harness.Map.AddEntity(target1, Point.From(target1));

        var target2 = MockMonster.Create(harness.Map);
        target2.WarpTo(new Point(startPoint.X + 2, startPoint.Y + 2));
        harness.Map.AddEntity(target2, Point.From(target2));

        harness.Use();
        harness.Script.Update(TimeSpan.FromMilliseconds(5000));

        var finalPoint = Point.From(harness.Source);

        finalPoint.Should()
                  .NotBe(startPoint, "Alpha Strike should no longer return the caster to their starting position");
    }

    //Points near the harness's default (5,5) source spawn, all within a 10x10 map's bounds, all within Alpha
    //Strike's default Range of 5, and none overlapping the source's own tile
    private static readonly Point[] NearbyPoints = [new(6, 5), new(7, 5), new(6, 6), new(6, 7), new(4, 6)];

    [Test]
    public void AlphaStrike_ShouldHitMoreTargets_AtHigherTiers()
    {
        var lowTierHarness = new SkillScriptHarness<AlphaStrikeScript>(
            scriptFactory: skill => new AlphaStrikeScript(skill) { BaseDamage = 80, Range = 5 },
            skillSetup: s =>
            {
                s.Level = 1;
                EnsureScriptVars(s, "alphaStrike");
            });

        lowTierHarness.Source.StatSheet.AddBonus(new Attributes { MaximumHp = 100000 });
        lowTierHarness.Source.StatSheet.SetHp(100000);

        var monsters = NearbyPoints.Select(point =>
        {
            var monster = MockMonster.Create(lowTierHarness.Map, setup: m => m.StatSheet.SetHp(100000));
            monster.WarpTo(point);
            lowTierHarness.Map.AddEntity(monster, point);

            return monster;
        }).ToList();

        lowTierHarness.Use();
        lowTierHarness.Script.Update(TimeSpan.FromMilliseconds(5000));
        var lowTierHitCount = monsters.Count(m => m.StatSheet.CurrentHp < 100000);

        var highTierHarness = new SkillScriptHarness<AlphaStrikeScript>(
            scriptFactory: skill => new AlphaStrikeScript(skill) { BaseDamage = 80, Range = 5 },
            skillSetup: s =>
            {
                s.Level = 20;
                EnsureScriptVars(s, "alphaStrike");
            });

        highTierHarness.Source.StatSheet.AddBonus(new Attributes { MaximumHp = 100000 });
        highTierHarness.Source.StatSheet.SetHp(100000);

        var highMonsters = NearbyPoints.Select(point =>
        {
            var monster = MockMonster.Create(highTierHarness.Map, setup: m => m.StatSheet.SetHp(100000));
            monster.WarpTo(point);
            highTierHarness.Map.AddEntity(monster, point);

            return monster;
        }).ToList();

        highTierHarness.Use();
        highTierHarness.Script.Update(TimeSpan.FromMilliseconds(5000));
        var highTierHitCount = highMonsters.Count(m => m.StatSheet.CurrentHp < 100000);

        highTierHitCount.Should()
                        .BeGreaterThan(lowTierHitCount, "Tier IV Alpha Strike should hit more targets than Tier I");
    }

    [Test]
    public void HowlingSequence_ShouldHitTheTargetMultipleTimes_AndMoreAtHigherTiers()
    {
        var lowTierHarness = new SkillScriptHarness<HowlingSequenceScript>(skillSetup: s =>
        {
            s.Level = 1;
            EnsureScriptVars(s, "howlingSequence");
        });

        //HowlingSequenceScript.Update checks pending.Source.IsAlive before applying each delayed hit - see the
        //identical comment on the Alpha Strike tests above
        lowTierHarness.Source.StatSheet.AddBonus(new Attributes { MaximumHp = 100000 });
        lowTierHarness.Source.StatSheet.SetHp(100000);

        lowTierHarness.Source.Direction = Direction.Down;
        var lowTarget = MockMonster.Create(lowTierHarness.Map, setup: m => m.StatSheet.SetHp(1000000));
        lowTarget.WarpTo(lowTierHarness.Source.DirectionalOffset(Direction.Down));
        lowTierHarness.Map.AddEntity(lowTarget, Point.From(lowTarget));

        var lowHpBefore = lowTarget.StatSheet.CurrentHp;
        lowTierHarness.Use();
        lowTierHarness.Script.Update(TimeSpan.FromMilliseconds(3000));
        var lowDamage = lowHpBefore - lowTarget.StatSheet.CurrentHp;

        var highTierHarness = new SkillScriptHarness<HowlingSequenceScript>(skillSetup: s =>
        {
            s.Level = 20;
            EnsureScriptVars(s, "howlingSequence");
        });

        highTierHarness.Source.StatSheet.AddBonus(new Attributes { MaximumHp = 100000 });
        highTierHarness.Source.StatSheet.SetHp(100000);

        highTierHarness.Source.Direction = Direction.Down;
        var highTarget = MockMonster.Create(highTierHarness.Map, setup: m => m.StatSheet.SetHp(1000000));
        highTarget.WarpTo(highTierHarness.Source.DirectionalOffset(Direction.Down));
        highTierHarness.Map.AddEntity(highTarget, Point.From(highTarget));

        var highHpBefore = highTarget.StatSheet.CurrentHp;
        highTierHarness.Use();
        highTierHarness.Script.Update(TimeSpan.FromMilliseconds(3000));
        var highDamage = highHpBefore - highTarget.StatSheet.CurrentHp;

        lowDamage.Should()
                .BeGreaterThan(0, "Howling Sequence should deal damage across its multi-hit combo");

        highDamage.Should()
                 .BeGreaterThan(lowDamage, "Tier IV Howling Sequence should deal more total damage than Tier I");
    }

    [Test]
    public void PerfectForm_ShouldGrantStrongerAttackSpeed_AtHigherTiers()
    {
        var lowTierHarness = new SkillScriptHarness<PerfectFormScript>(skillSetup: s =>
        {
            s.Level = 1;
            EnsureScriptVars(s, "perfectForm");
        });

        lowTierHarness.Use();
        lowTierHarness.Source.Effects.TryGetEffect("Perfect Form", out var lowEffect);

        var highTierHarness = new SkillScriptHarness<PerfectFormScript>(skillSetup: s =>
        {
            s.Level = 20;
            EnsureScriptVars(s, "perfectForm");
        });

        highTierHarness.Use();
        highTierHarness.Source.Effects.TryGetEffect("Perfect Form", out var highEffect);

        (lowEffect as PerfectFormEffect)!.AtkSpeedBonus
                                         .Should()
                                         .BeLessThan((highEffect as PerfectFormEffect)!.AtkSpeedBonus, "Tier IV Perfect Form should grant more Attack Speed than Tier I");
    }

    [Test]
    public void Break_ShouldIncreaseIncomingDamage()
    {
        var applyDamageScript = ApplyAttackDamageScript.Create();
        var harness = new SkillScriptHarness<BreakScript>(skillSetup: s => EnsureScriptVars(s, "break"));

        harness.WithTargetMonster(m => m.StatSheet.SetHp(100000));
        harness.Use();

        var attacker = MockMonster.Create();
        var brokenHpBefore = harness.Target!.StatSheet.CurrentHp;
        applyDamageScript.ApplyDamage(attacker, harness.Target, MockSkill.Create().Script, 1000);
        var brokenDamage = brokenHpBefore - harness.Target.StatSheet.CurrentHp;

        var unbroken = MockMonster.Create();
        unbroken.StatSheet.SetHp(100000);
        var unbrokenHpBefore = unbroken.StatSheet.CurrentHp;
        applyDamageScript.ApplyDamage(attacker, unbroken, MockSkill.Create().Script, 1000);
        var unbrokenDamage = unbrokenHpBefore - unbroken.StatSheet.CurrentHp;

        brokenDamage.Should()
                   .BeGreaterThan(unbrokenDamage, "Break should increase damage taken by the target");
    }

    [Test]
    public void PredatorsPounce_ShouldDamageAndStunTheTarget()
    {
        var harness = new SkillScriptHarness<PredatorsPounceScript>(
            scriptFactory: skill => new PredatorsPounceScript(skill) { BaseDamage = 100, StunDurationMs = 1000 },
            skillSetup: s => EnsureScriptVars(s, "predatorsPounce"));

        harness.Source.Direction = Direction.Down;
        harness.WithTargetMonster(m => m.StatSheet.SetHp(100000));
        harness.Target!.WarpTo(harness.Source.DirectionalOffset(Direction.Down));
        harness.Map.AddEntity(harness.Target, Point.From(harness.Target));

        var hpBefore = harness.Target.StatSheet.CurrentHp;
        harness.Use();

        harness.Target.StatSheet.CurrentHp
               .Should()
               .BeLessThan(hpBefore, "Predator's Pounce should deal damage");

        harness.Target.Effects.TryGetEffect("Root", out _)
               .Should()
               .BeTrue("Predator's Pounce should stun the target on landing");
    }

    [Test]
    public void BloodSacrifice_ShouldCostHealthAndDealBonusDamage()
    {
        var harness = new SkillScriptHarness<BloodSacrificeScript>(skillSetup: s => EnsureScriptVars(s, "bloodSacrifice"));

        harness.Source.StatSheet.AddBonus(new Attributes { MaximumHp = 100000 });
        harness.Source.StatSheet.SetHp(10000);
        harness.WithTargetMonster(m => m.StatSheet.SetHp(1000000));

        var casterHpBefore = harness.Source.StatSheet.CurrentHp;
        var targetHpBefore = harness.Target!.StatSheet.CurrentHp;
        harness.Use();

        harness.Source.StatSheet.CurrentHp
               .Should()
               .BeLessThan(casterHpBefore, "Blood Sacrifice should cost the caster Health");

        harness.Target.StatSheet.CurrentHp
               .Should()
               .BeLessThan(targetHpBefore, "Blood Sacrifice should deal damage to the target");
    }

    [Test]
    public void TraversePunch_ShouldLandNearTheTargetAndDealDamage()
    {
        var harness = new SkillScriptHarness<TraversePunchScript>(
            scriptFactory: skill => new TraversePunchScript(skill) { BaseDamage = 70, Range = 6 },
            skillSetup: s => EnsureScriptVars(s, "traversePunch"));

        var startPoint = Point.From(harness.Source);
        var target = MockMonster.Create(harness.Map, setup: m => m.StatSheet.SetHp(100000));
        target.WarpTo(new Point(startPoint.X + 3, startPoint.Y));
        harness.Map.AddEntity(target, Point.From(target));

        var hpBefore = target.StatSheet.CurrentHp;
        harness.Use();

        target.StatSheet.CurrentHp
              .Should()
              .BeLessThan(hpBefore, "Traverse Punch should deal damage");

        Point.From(harness.Source)
             .Should()
             .NotBe(startPoint, "Traverse Punch should dash the caster toward the target");
    }

    [Test]
    public void Meditate_ShouldRestoreMoreMp_AtHigherTiers()
    {
        var lowTierHarness = new SkillScriptHarness<MeditateScript>(skillSetup: s =>
        {
            s.Level = 1;
            EnsureScriptVars(s, "meditate");
        });

        lowTierHarness.Use();
        lowTierHarness.Source.Effects.TryGetEffect("Meditate", out var lowEffect);

        var highTierHarness = new SkillScriptHarness<MeditateScript>(skillSetup: s =>
        {
            s.Level = 20;
            EnsureScriptVars(s, "meditate");
        });

        highTierHarness.Use();
        highTierHarness.Source.Effects.TryGetEffect("Meditate", out var highEffect);

        (lowEffect as MeditateEffect)!.MpPerTick
                                      .Should()
                                      .BeLessThan((highEffect as MeditateEffect)!.MpPerTick, "Tier IV Meditate should restore more Mp per tick than Tier I");
    }

    [Test]
    public void MartialForm_ShouldGrantStatBonusAndChangeSprite_ForBeastSpecialization()
    {
        var harness = new SkillScriptHarness<MartialFormScript>(skillSetup: s => EnsureScriptVars(s, "martialForm"));

        harness.Source.UserStatSheet.SetBaseClass(BaseClass.MartialArtist);
        harness.Source.UserStatSheet.SetAdvClass(AdvClass.Fighter);
        harness.Source.StatSheet.AddBonus(new Attributes { MaximumMp = 1000 });
        harness.Source.StatSheet.SetMp(1000);

        var spriteBefore = harness.Source.Sprite;
        var strBefore = harness.Source.StatSheet.EffectiveStr;

        harness.Use();

        harness.Source.Effects.TryGetEffect("Martial Form", out _)
               .Should()
               .BeTrue("using Martial Form should transform the caster");

        harness.Source.StatSheet.EffectiveStr
               .Should()
               .BeGreaterThan(strBefore, "Beast's Martial Form should grant a Str bonus");

        harness.Source.Sprite
               .Should()
               .NotBe(spriteBefore, "Beast's Martial Form should change the caster's sprite");
    }

    [Test]
    public void BoneMemory_ShouldIncreaseDamage_WithConsecutiveHits()
    {
        var applyDamageScript = ApplyAttackDamageScript.Create();
        var attacker = MockAisling.Create();
        attacker.UserStatSheet.SetBaseClass(BaseClass.MartialArtist);
        var boneMemorySkill = MockSkill.Create(name: "Bone Memory", templateSetup: t => t with { TemplateKey = "bone_memory" });
        attacker.SkillBook.TryAddToNextSlot(boneMemorySkill);

        var target = MockMonster.Create();
        target.StatSheet.SetHp(1000000);

        var firstHpBefore = target.StatSheet.CurrentHp;
        applyDamageScript.ApplyDamage(attacker, target, MockSkill.Create().Script, 1000);
        var firstHitDamage = firstHpBefore - target.StatSheet.CurrentHp;

        //land several more consecutive hits, building Bone Memory stacks
        for (var i = 0; i < 5; i++)
        {
            var hpBefore = target.StatSheet.CurrentHp;
            applyDamageScript.ApplyDamage(attacker, target, MockSkill.Create().Script, 1000);
        }

        var lastHpBefore = target.StatSheet.CurrentHp;
        applyDamageScript.ApplyDamage(attacker, target, MockSkill.Create().Script, 1000);
        var laterHitDamage = lastHpBefore - target.StatSheet.CurrentHp;

        laterHitDamage.Should()
                      .BeGreaterThan(firstHitDamage, "Bone Memory should increase damage as consecutive-hit stacks build");
    }

    [Test]
    public void SurvivalInstinct_ShouldReduceIncomingDamage_AfterTakingAHit()
    {
        var applyDamageScript = ApplyAttackDamageScript.Create();
        var defender = MockAisling.Create();
        defender.UserStatSheet.SetBaseClass(BaseClass.MartialArtist);
        defender.StatSheet.AddBonus(new Attributes { MaximumHp = 1000000 });
        defender.StatSheet.SetHp(1000000);
        var survivalSkill = MockSkill.Create(name: "Survival Instinct", templateSetup: t => t with { TemplateKey = "survival_instinct" });
        defender.SkillBook.TryAddToNextSlot(survivalSkill);

        var attacker = MockMonster.Create();

        var firstHpBefore = defender.StatSheet.CurrentHp;
        applyDamageScript.ApplyDamage(attacker, defender, MockSkill.Create().Script, 1000);
        var firstHitDamage = firstHpBefore - defender.StatSheet.CurrentHp;

        var secondHpBefore = defender.StatSheet.CurrentHp;
        applyDamageScript.ApplyDamage(attacker, defender, MockSkill.Create().Script, 1000);
        var secondHitDamage = secondHpBefore - defender.StatSheet.CurrentHp;

        secondHitDamage.Should()
                       .BeLessThan(firstHitDamage, "Survival Instinct should reduce damage taken after the first hit");
    }

    [Test]
    public void FeralHunger_ShouldHealTheAttackerOnBasicAttacks()
    {
        var applyDamageScript = ApplyAttackDamageScript.Create();
        var attacker = MockAisling.Create();
        attacker.UserStatSheet.SetBaseClass(BaseClass.MartialArtist);
        attacker.StatSheet.AddBonus(new Attributes { MaximumHp = 1000 });
        attacker.StatSheet.SetHp(500);
        var feralHungerSkill = MockSkill.Create(name: "Feral Hunger", templateSetup: t => t with { TemplateKey = "feral_hunger" });
        attacker.SkillBook.TryAddToNextSlot(feralHungerSkill);

        var target = MockMonster.Create();
        target.StatSheet.SetHp(1000000);

        var hpBefore = attacker.StatSheet.CurrentHp;
        applyDamageScript.ApplyDamage(attacker, target, MockSkill.Create().Script, 1000);

        attacker.StatSheet.CurrentHp
                .Should()
                .BeGreaterThan(hpBefore, "Feral Hunger should heal the attacker on basic attacks");
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
