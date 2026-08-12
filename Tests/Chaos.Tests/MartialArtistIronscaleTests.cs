#region
using Chaos.Collections;
using Chaos.Common.Abstractions;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;
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
///     Real functional tests for Ironscale's new/reworked/evolving mechanics - asserts actual resulting
///     values/state, not just "doesn't throw", per this session's established standard.
/// </summary>
public sealed class MartialArtistIronscaleTests
{
    static MartialArtistIronscaleTests()
    {
        var registry = new FunctionalScriptRegistry(MockServiceProvider.CreateBuilder()
                                                                        .Build()
                                                                        .Object);

        registry.Register(ApplyAttackDamageScript.Key, typeof(ApplyAttackDamageScript));
    }

    private static void EnsureScriptVars(Skill skill, string scriptKey) => skill.Template.ScriptVars[scriptKey] = new EmptyScriptVars();

    [Test]
    public void IronBody_ShouldReflectDamage_RootTheCaster_AndPullAggro()
    {
        var applyDamageScript = ApplyAttackDamageScript.Create();
        var harness = new SkillScriptHarness<IronBodyScript>(skillSetup: s =>
        {
            s.Level = 20;
            EnsureScriptVars(s, "ironBody");
        });

        harness.Source.StatSheet.AddBonus(new Attributes { MaximumHp = 100000 });
        harness.Source.StatSheet.SetHp(100000);

        var monster = MockMonster.Create(harness.Map);
        monster.WarpTo(Point.From(harness.Source));
        harness.Map.AddEntity(monster, Point.From(monster));

        harness.Use();

        harness.Source.Effects.TryGetEffect("Iron Body", out _)
               .Should()
               .BeTrue("Iron Body should apply its own buff");

        harness.Source.Effects.TryGetEffect("Root", out _)
               .Should()
               .BeTrue("Iron Body should root the caster as its own cost");

        monster.AggroList.GetAggro(harness.Source)
               .Should()
               .BeGreaterThan(0, "Iron Body should pull threat from nearby monsters");

        monster.StatSheet.SetHp(100000);

        applyDamageScript.ApplyDamage(monster, harness.Source, MockSkill.Create().Script, 1000);

        monster.StatSheet.CurrentHp
               .Should()
               .BeLessThan(100000, "Iron Body should reflect a portion of incoming damage back at the attacker");
    }

    [Test]
    public void PressurePoint_ShouldIncreaseDamageTaken_AndSpreadToNearbyEnemies_AtMaxTier()
    {
        var applyDamageScript = ApplyAttackDamageScript.Create();
        var harness = new SkillScriptHarness<PressurePointScript>(skillSetup: s =>
        {
            s.Level = 20;
            EnsureScriptVars(s, "pressurePoint");
        });

        harness.WithTargetMonster(m => m.StatSheet.SetHp(100000));
        var nearbyMonster = MockMonster.Create(harness.Map, setup: m => m.StatSheet.SetHp(100000));
        nearbyMonster.WarpTo(Point.From(harness.Target!));
        harness.Map.AddEntity(nearbyMonster, Point.From(nearbyMonster));

        harness.Use();

        harness.Target!.Effects.TryGetEffect("Pressure Point", out _)
               .Should()
               .BeTrue("Pressure Point should mark the primary target");

        nearbyMonster.Effects.TryGetEffect("Pressure Point", out _)
                     .Should()
                     .BeTrue("Tier IV Pressure Point should spread the mark to nearby enemies");

        var attacker = MockMonster.Create();
        var hpBefore = harness.Target.StatSheet.CurrentHp;
        applyDamageScript.ApplyDamage(attacker, harness.Target, MockSkill.Create().Script, 1000);
        var markedDamage = hpBefore - harness.Target.StatSheet.CurrentHp;

        var unmarked = MockMonster.Create();
        unmarked.StatSheet.SetHp(100000);
        var unmarkedHpBefore = unmarked.StatSheet.CurrentHp;
        applyDamageScript.ApplyDamage(attacker, unmarked, MockSkill.Create().Script, 1000);
        var unmarkedDamage = unmarkedHpBefore - unmarked.StatSheet.CurrentHp;

        markedDamage.Should()
                   .BeGreaterThan(unmarkedDamage, "Pressure Point should increase damage taken by the marked target");
    }

    [Test]
    public void Shockwave_ShouldDamagePullAggro_AndRoot_AtHighTier()
    {
        var harness = new SkillScriptHarness<ShockwaveScript>(
            scriptFactory: skill => new ShockwaveScript(skill) { BaseDamage = 50, DamageStat = Stat.CON, DamageStatMultiplier = 2 },
            skillSetup: s =>
            {
                s.Level = 20;
                EnsureScriptVars(s, "shockwave");
            });

        var monster = MockMonster.Create(harness.Map, setup: m => m.StatSheet.SetHp(100000));
        monster.WarpTo(Point.From(harness.Source));
        harness.Map.AddEntity(monster, Point.From(monster));

        var hpBefore = monster.StatSheet.CurrentHp;
        harness.Use();

        monster.StatSheet.CurrentHp
               .Should()
               .BeLessThan(hpBefore, "Shockwave should deal damage to nearby monsters");

        monster.AggroList.GetAggro(harness.Source)
               .Should()
               .BeGreaterThan(0, "Shockwave should pull threat from nearby monsters");

        monster.Effects.TryGetEffect("Root", out _)
               .Should()
               .BeTrue("Tier IV Shockwave should root nearby monsters - its battlefield-control evolution");
    }

    [Test]
    public void CounterStrike_ShouldNegateAndCounterTheNextHit()
    {
        var applyDamageScript = ApplyAttackDamageScript.Create();
        var defender = MockAisling.Create();
        defender.StatSheet.AddBonus(new Attributes { MaximumHp = 100000, Dex = 10 });
        defender.StatSheet.SetHp(100000);
        defender.Effects.Apply(defender, new CounterStrikeEffect(), MockSkill.Create().Script);

        var attacker = MockAisling.Create();
        attacker.StatSheet.AddBonus(new Attributes { MaximumHp = 100000 });
        attacker.StatSheet.SetHp(100000);

        var defenderHpBefore = defender.StatSheet.CurrentHp;
        var attackerHpBefore = attacker.StatSheet.CurrentHp;

        applyDamageScript.ApplyDamage(attacker, defender, MockSkill.Create().Script, 1000);

        defender.StatSheet.CurrentHp
                .Should()
                .Be(defenderHpBefore, "Counter Strike should negate the incoming hit entirely");

        attacker.StatSheet.CurrentHp
                .Should()
                .BeLessThan(attackerHpBefore, "Counter Strike should counter-damage the attacker");
    }

    [Test]
    public void PerfectCounter_ShouldNegateAndReflectTheFullIncomingHit()
    {
        var applyDamageScript = ApplyAttackDamageScript.Create();
        var defender = MockAisling.Create();
        defender.StatSheet.AddBonus(new Attributes { MaximumHp = 100000 });
        defender.StatSheet.SetHp(100000);
        defender.Effects.Apply(defender, new PerfectCounterEffect(), MockSkill.Create().Script);

        var attacker = MockAisling.Create();
        attacker.StatSheet.AddBonus(new Attributes { MaximumHp = 100000 });
        attacker.StatSheet.SetHp(100000);

        var defenderHpBefore = defender.StatSheet.CurrentHp;
        var attackerHpBefore = attacker.StatSheet.CurrentHp;

        applyDamageScript.ApplyDamage(attacker, defender, MockSkill.Create().Script, 1000);

        defender.StatSheet.CurrentHp
                .Should()
                .Be(defenderHpBefore, "Perfect Counter should negate the incoming hit entirely");

        //the reflected hit goes back through the normal damage formula (same as Bastion's Retribution/Counter
        //Strike), so it isn't a raw 1:1 of the original 1000 input once the attacker's own mitigation applies -
        //just confirm a real reflect actually landed
        attacker.StatSheet.CurrentHp
                .Should()
                .BeLessThan(attackerHpBefore, "Perfect Counter should reflect the incoming hit back at the attacker");
    }

    [Test]
    public void Taunt_ShouldPullAggroFromNearbyMonsters()
    {
        var harness = new SkillScriptHarness<TauntScript>(skillSetup: s => EnsureScriptVars(s, "taunt"));

        var monster1 = MockMonster.Create(harness.Map);
        monster1.WarpTo(Point.From(harness.Source));
        harness.Map.AddEntity(monster1, Point.From(monster1));

        var monster2 = MockMonster.Create(harness.Map);
        monster2.WarpTo(new Point(6, 6));
        harness.Map.AddEntity(monster2, Point.From(monster2));

        harness.Use();

        monster1.AggroList.GetAggro(harness.Source)
                .Should()
                .BeGreaterThan(0, "Taunt should pull threat from nearby monsters");

        monster2.AggroList.GetAggro(harness.Source)
                .Should()
                .BeGreaterThan(0, "Taunt should pull threat from every nearby monster, not just one");
    }

    [Test]
    public void AnchorHold_ShouldRootTheTarget()
    {
        var harness = new SkillScriptHarness<AnchorHoldScript>(skillSetup: s => EnsureScriptVars(s, "anchorHold"));

        harness.WithTargetMonster();
        harness.Use();

        harness.Target!.Effects.TryGetEffect("Root", out _)
               .Should()
               .BeTrue("Anchor Hold should root the target");
    }

    [Test]
    public void ElementalDefense_ShouldReduceDamage_FromARepeatOfTheSameElement()
    {
        var applyDamageScript = ApplyAttackDamageScript.Create();
        var defender = MockAisling.Create();
        defender.UserStatSheet.SetBaseClass(BaseClass.MartialArtist);
        defender.StatSheet.AddBonus(new Attributes { MaximumHp = 1000000 });
        defender.StatSheet.SetHp(1000000);
        var elementalDefenseSkill = MockSkill.Create(name: "Elemental Defense", templateSetup: t => t with { TemplateKey = "elemental_defense" });
        defender.SkillBook.TryAddToNextSlot(elementalDefenseSkill);

        var attacker = MockAisling.Create();

        var firstHpBefore = defender.StatSheet.CurrentHp;
        applyDamageScript.ApplyDamage(attacker, defender, MockSkill.Create().Script, 1000, Element.Fire);
        var firstHitDamage = firstHpBefore - defender.StatSheet.CurrentHp;

        var secondHpBefore = defender.StatSheet.CurrentHp;
        applyDamageScript.ApplyDamage(attacker, defender, MockSkill.Create().Script, 1000, Element.Fire);
        var secondHitDamage = secondHpBefore - defender.StatSheet.CurrentHp;

        secondHitDamage.Should()
                       .BeLessThan(firstHitDamage, "Elemental Defense should reduce damage from a repeat hit of the same element");
    }

    [Test]
    public void Bedrock_ShouldGrantIncreasingDamageReduction_TheLongerStationary()
    {
        var harness = new AislingScriptHarness<BedrockScript>();
        harness.Source.UserStatSheet.SetBaseClass(BaseClass.MartialArtist);
        var bedrockSkill = MockSkill.Create(name: "Bedrock", templateSetup: t => t with { TemplateKey = "bedrock" });
        harness.Source.SkillBook.TryAddToNextSlot(bedrockSkill);

        for (var i = 0; i < 5; i++)
            harness.Update(TimeSpan.FromSeconds(1));

        harness.Source.Trackers.Tags.TryGetValue(BedrockEffect.DamageReductionPctTag, out var pctStr)
               .Should()
               .BeTrue("standing still long enough should grant Bedrock's damage reduction");

        int.Parse(pctStr!)
            .Should()
            .BeGreaterThan(0, "Bedrock's damage reduction should be a positive percentage");
    }

    [Test]
    public void LivingFortress_ShouldGrantIncreasingDamageReduction_WithConsecutiveHitsTaken()
    {
        var applyDamageScript = ApplyAttackDamageScript.Create();
        var defender = MockAisling.Create();
        defender.UserStatSheet.SetBaseClass(BaseClass.MartialArtist);
        defender.StatSheet.AddBonus(new Attributes { MaximumHp = 1000000 });
        defender.StatSheet.SetHp(1000000);
        var livingFortressSkill = MockSkill.Create(name: "Living Fortress", templateSetup: t => t with { TemplateKey = "living_fortress" });
        defender.SkillBook.TryAddToNextSlot(livingFortressSkill);

        var attacker = MockMonster.Create();

        var firstHpBefore = defender.StatSheet.CurrentHp;
        applyDamageScript.ApplyDamage(attacker, defender, MockSkill.Create().Script, 1000);
        var firstHitDamage = firstHpBefore - defender.StatSheet.CurrentHp;

        for (var i = 0; i < 5; i++)
            applyDamageScript.ApplyDamage(attacker, defender, MockSkill.Create().Script, 1000);

        var laterHpBefore = defender.StatSheet.CurrentHp;
        applyDamageScript.ApplyDamage(attacker, defender, MockSkill.Create().Script, 1000);
        var laterHitDamage = laterHpBefore - defender.StatSheet.CurrentHp;

        laterHitDamage.Should()
                      .BeLessThan(firstHitDamage, "Living Fortress should reduce damage taken as Fortitude stacks build");
    }

    [Test]
    public void MartialForm_ShouldGrantTankStatsAndChangeSprite_ForIronscaleSpecialization()
    {
        var harness = new SkillScriptHarness<MartialFormScript>(skillSetup: s => EnsureScriptVars(s, "martialForm"));

        harness.Source.UserStatSheet.SetBaseClass(BaseClass.MartialArtist);
        harness.Source.UserStatSheet.SetAdvClass(AdvClass.Tank);
        harness.Source.StatSheet.AddBonus(new Attributes { MaximumMp = 1000 });
        harness.Source.StatSheet.SetMp(1000);

        var spriteBefore = harness.Source.Sprite;
        var conBefore = harness.Source.StatSheet.EffectiveCon;

        harness.Use();

        harness.Source.Effects.TryGetEffect("Martial Form", out _)
               .Should()
               .BeTrue("using Martial Form should transform the caster");

        harness.Source.StatSheet.EffectiveCon
               .Should()
               .BeGreaterThan(conBefore, "Ironscale's Martial Form should grant a Con bonus");

        harness.Source.Sprite
               .Should()
               .NotBe(spriteBefore, "Ironscale's Martial Form should change the caster's sprite");
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
