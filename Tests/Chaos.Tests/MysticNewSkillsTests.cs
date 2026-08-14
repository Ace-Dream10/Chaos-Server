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
using Chaos.Networking.Abstractions;
using Chaos.Scripting.AislingScripts;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.FunctionalScripts;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.FunctionalScripts.ApplyHealing;
using Chaos.Scripting.SpellScripts;
using Chaos.Services.Factories.Abstractions;
using Chaos.Testing.Infrastructure.Harnesses;
using Chaos.Testing.Infrastructure.Mocks;
using FluentAssertions;
using Moq;
#endregion

namespace Chaos.Tests;

/// <summary>
///     Real functional tests for Mystic's new/reworked/evolving mechanics - asserts actual resulting values/state,
///     not just "doesn't throw", per this session's established standard.
/// </summary>
public sealed class MysticNewSkillsTests
{
    static MysticNewSkillsTests()
    {
        var registry = new FunctionalScriptRegistry(MockServiceProvider.CreateBuilder()
                                                                        .Build()
                                                                        .Object);

        registry.Register(ApplyAttackDamageScript.Key, typeof(ApplyAttackDamageScript));
        registry.Register(ApplyHealScript.Key, typeof(ApplyHealScript));
    }

    private static void EnsureSpellVars(Spell spell, string scriptKey) => spell.Template.ScriptVars[scriptKey] = new EmptyScriptVars();

    [Test]
    public void BloomingLife_ShouldHealMoreAndSplashToNearbyAllies_AtHigherTiers()
    {
        var lowTierHarness = new SpellScriptHarness<BloomingLifeScript>(spellSetup: s =>
        {
            s.Level = 1;
            EnsureSpellVars(s, "bloomingLife");
        });

        lowTierHarness.WithTargetAisling(a =>
        {
            a.StatSheet.AddBonus(new Attributes { MaximumHp = 100000 });
            a.StatSheet.SetHp(1000);
        });

        lowTierHarness.Use();
        lowTierHarness.Target!.Effects.TryGetEffect("Blooming Life", out var lowEffect);
        var lowBloom = lowEffect as BloomingLifeEffect;

        var highTierHarness = new SpellScriptHarness<BloomingLifeScript>(spellSetup: s =>
        {
            s.Level = 20;
            EnsureSpellVars(s, "bloomingLife");
        });

        highTierHarness.WithTargetAisling(a =>
        {
            a.StatSheet.AddBonus(new Attributes { MaximumHp = 100000 });
            a.StatSheet.SetHp(1000);
        });

        var nearbyAlly = MockAisling.Create(highTierHarness.Map);
        nearbyAlly.StatSheet.AddBonus(new Attributes { MaximumHp = 100000 });
        nearbyAlly.StatSheet.SetHp(1000);
        nearbyAlly.WarpTo(Point.From(highTierHarness.Target!));
        highTierHarness.Map.AddEntity(nearbyAlly, Point.From(highTierHarness.Target!));

        highTierHarness.Use();
        highTierHarness.Target!.Effects.TryGetEffect("Blooming Life", out var highEffect);
        var highBloom = highEffect as BloomingLifeEffect;

        lowBloom!.HealPerTick
                 .Should()
                 .BeLessThan(highBloom!.HealPerTick, "Tier IV Blooming Life should heal more per tick than Tier I");

        nearbyAlly.Effects.TryGetEffect("Blooming Life", out _)
                  .Should()
                  .BeTrue("Tier IV Blooming Life should splash to nearby allies");
    }

    [Test]
    public void SpiritRend_ShouldBeSingleConsumption_AtTierI_ButAWindow_AtTierII()
    {
        var lowTierHarness = new SpellScriptHarness<SpiritRendScript>(spellSetup: s =>
        {
            s.Level = 1;
            EnsureSpellVars(s, "spiritRend");
        });

        lowTierHarness.WithTargetMonster();
        lowTierHarness.Use();
        lowTierHarness.Target!.Effects.TryGetEffect("Spirit Rend", out var lowEffect);

        var highTierHarness = new SpellScriptHarness<SpiritRendScript>(spellSetup: s =>
        {
            s.Level = 20;
            EnsureSpellVars(s, "spiritRend");
        });

        highTierHarness.WithTargetMonster();
        highTierHarness.Use();
        highTierHarness.Target!.Effects.TryGetEffect("Spirit Rend", out var highEffect);

        (lowEffect as SpiritRendEffect)!.ConsumeOnHit
                                        .Should()
                                        .BeTrue("Tier I Spirit Rend marks only the next magical hit, then is consumed");

        (highEffect as SpiritRendEffect)!.ConsumeOnHit
                                         .Should()
                                         .BeFalse("Tier IV Spirit Rend is a standing window, not single-consumption");
    }

    [Test]
    public void SpiritRend_ShouldSwapDefenseElementToDarkness_WhileActive_AndRestoreItOnTermination()
    {
        //reworked to actually function like retail Dark Ages' "Fas Nadur" (researched, not guessed at): swaps the
        //target's defensive element to make them vulnerable to a follow-up Darkness spell, restoring their
        //original element when the mark ends - regardless of natural expiry or Tier I's on-hit consumption
        var harness = new SpellScriptHarness<SpiritRendScript>(
            spellSetup: s =>
            {
                s.Level = 20; //Tier IV - standing window, not single-consumption, easier to assert mid-effect
                EnsureSpellVars(s, "spiritRend");
            });

        harness.WithTargetMonster(m => m.StatSheet.SetDefenseElement(Element.Fire));
        var originalElement = harness.Target!.StatSheet.DefenseElement;

        harness.Use();

        harness.Target.StatSheet.DefenseElement
               .Should()
               .Be(Element.Darkness, "Spirit Rend should make the target vulnerable to Darkness while active");

        harness.Target.Effects.Terminate("Spirit Rend");

        harness.Target.StatSheet.DefenseElement
               .Should()
               .Be(originalElement, "the target's original defense element should be restored once Spirit Rend ends");
    }

    [Test]
    public void SpiritRend_ShouldReAnimate_NotJustApplyOnce()
    {
        //per the explicit instruction not to ship this as an invisible stat change - confirms the persistent
        //re-animating indicator actually fires again over time, not just once on application. Animate() on a
        //Creature broadcasts to nearby Aislings observing it, so the caster needs to be registered in the map's
        //spatial index to actually receive (and let us count) those broadcasts - same gotcha noted in builder.md.
        var harness = new SpellScriptHarness<SpiritRendScript>(
            spellSetup: s =>
            {
                s.Level = 20;
                EnsureSpellVars(s, "spiritRend");
            });

        harness.Map.AddEntity(harness.Source, Point.From(harness.Source));
        harness.WithTargetMonster();
        harness.Map.AddEntity(harness.Target!, Point.From(harness.Target!));

        harness.Use();

        var animateCallsBefore = Mock.Get(harness.Source.Client)
                                     .Invocations.Count(i => i.Method.Name == nameof(IChaosWorldClient.SendAnimation));

        harness.Target!.Effects.Update(TimeSpan.FromMilliseconds(1300));

        var animateCallsAfter = Mock.Get(harness.Source.Client)
                                    .Invocations.Count(i => i.Method.Name == nameof(IChaosWorldClient.SendAnimation));

        animateCallsAfter.Should()
                         .BeGreaterThan(animateCallsBefore, "Spirit Rend's visual should keep re-playing while active, not just on initial apply");
    }

    [Test]
    public void CommunionRite_ShouldReviveWithMoreHealth_AtHigherTiers()
    {
        var lowTierHarness = new SpellScriptHarness<CommunionRiteScript>(spellSetup: s =>
        {
            s.Level = 1;
            EnsureSpellVars(s, "communionRite");
        });

        lowTierHarness.WithTargetAisling(a =>
        {
            a.StatSheet.AddBonus(new Attributes { MaximumHp = 1000 });
            a.IsDead = true;
        });

        lowTierHarness.Use();
        var lowHp = lowTierHarness.Target!.StatSheet.CurrentHp;

        var highTierHarness = new SpellScriptHarness<CommunionRiteScript>(spellSetup: s =>
        {
            s.Level = 20;
            EnsureSpellVars(s, "communionRite");
        });

        highTierHarness.WithTargetAisling(a =>
        {
            a.StatSheet.AddBonus(new Attributes { MaximumHp = 1000 });
            a.IsDead = true;
        });

        highTierHarness.Use();
        var highHp = highTierHarness.Target!.StatSheet.CurrentHp;

        highTierHarness.Target.IsDead
                       .Should()
                       .BeFalse("Communion Rite should revive a skulled ally");

        highHp.Should()
              .BeGreaterThan(lowHp, "Tier IV Communion Rite should revive with more Health than Tier I");
    }

    [Test]
    public void StaciasJudgment_ShouldSpawnASecondShrine_AtTierIV()
    {
        MapInstance? map = null;
        var monsterFactoryMock = new Mock<IMonsterFactory>();

        monsterFactoryMock
            .Setup(
                f => f.Create(
                    It.IsAny<string>(),
                    It.IsAny<MapInstance>(),
                    It.IsAny<Chaos.Geometry.Abstractions.IPoint>(),
                    It.IsAny<ICollection<string>?>()))
            .Returns(
                (string templateKey, MapInstance _, Chaos.Geometry.Abstractions.IPoint _, ICollection<string>? _) =>
                    MockMonster.Create(map!, templateSetup: t => t with { TemplateKey = templateKey }));

        var serviceProvider = MockServiceProvider.CreateBuilder()
                                                  .SetupService(monsterFactoryMock.Object)
                                                  .Build()
                                                  .Object;

        var harness = new SpellScriptHarness<StaciasJudgmentScript>(
            scriptFactory: spell => new StaciasJudgmentScript(spell, monsterFactoryMock.Object),
            spellSetup: s =>
            {
                s.Level = 20;
                EnsureSpellVars(s, "staciasJudgment");
            },
            serviceProvider: serviceProvider);

        map = harness.Map;
        harness.WithTargetMonster();
        harness.Use();

        var shrineCountAfterFirstStrike = map.GetEntities<Monster>().Count(m => m.Template.TemplateKey == string.Empty);

        harness.Script.Update(TimeSpan.FromMilliseconds(2100));

        var shrineCountAfterSecondStrike = map.GetEntities<Monster>().Count(m => m.Template.TemplateKey == string.Empty);

        shrineCountAfterSecondStrike.Should()
                                    .BeGreaterThan(shrineCountAfterFirstStrike, "Tier IV Stacia's Judgment should strike twice");
    }

    [Test]
    public void StaciasShrine_ShouldCastAtAGroundPoint_RegardlessOfWhatEntityIsThere()
    {
        //reworked per playtest feedback from entity-targeted to ground-targeted casting. Proves the rework: the
        //real template's filter is "friendlyOnly" - under the OLD entity-targeted CanUse, selecting a HOSTILE
        //monster would have failed that filter check outright. Now that cast-time validation only checks range
        //to the clicked point (not the entity there), this should succeed and spawn the shrine at that point.
        MapInstance? map = null;
        var monsterFactoryMock = new Mock<IMonsterFactory>();

        monsterFactoryMock
            .Setup(
                f => f.Create(
                    It.IsAny<string>(),
                    It.IsAny<MapInstance>(),
                    It.IsAny<Chaos.Geometry.Abstractions.IPoint>(),
                    It.IsAny<ICollection<string>?>()))
            .Returns(
                (string templateKey, MapInstance _, Chaos.Geometry.Abstractions.IPoint spawnPoint, ICollection<string>? _) =>
                    MockMonster.Create(map!, templateSetup: t => t with { TemplateKey = templateKey }, setup: m => m.WarpTo(spawnPoint)));

        var serviceProvider = MockServiceProvider.CreateBuilder()
                                                  .SetupService(monsterFactoryMock.Object)
                                                  .Build()
                                                  .Object;

        var harness = new SpellScriptHarness<StaciasShrineScript>(
            scriptFactory: spell => new StaciasShrineScript(spell, monsterFactoryMock.Object)
            {
                Filter = TargetFilter.FriendlyOnly | TargetFilter.AliveOnly,
                Range = 8,
                ShrineTemplateKey = "stacias_shrine"
            },
            spellSetup: s => EnsureSpellVars(s, "staciasShrine"),
            serviceProvider: serviceProvider);

        map = harness.Map;
        harness.Source.StatSheet.SetHp(500);

        //a HOSTILE monster at the clicked point - would have failed the old friendlyOnly entity-target check
        harness.WithTargetMonster();
        var clickedPoint = Point.From(harness.Target!);

        harness.CanUse()
               .Should()
               .BeTrue("Stacia's Shrine should only validate range to the clicked point now, not the entity standing there");

        harness.Use();

        var shrine = map.GetEntities<Monster>()
                        .Should()
                        .ContainSingle(m => m.Template.TemplateKey == "stacias_shrine")
                        .Which;

        Point.From(shrine)
             .Should()
             .Be(clickedPoint, "the shrine should spawn at the clicked ground point");
    }

    [Test]
    public void SpiritBurst_ShouldDamageTargetsInAFrontalCone()
    {
        //reworked per playtest feedback from single-target to a frontal cone (matching Crack the Whip's shape) -
        //this asserts a target directly in front of the caster takes damage from the NoTarget cone cast
        var harness = new SpellScriptHarness<SpiritBurstScript>(
            scriptFactory: spell => new SpiritBurstScript(spell) { BaseDamage = 60, Range = 3 },
            spellSetup: s => EnsureSpellVars(s, "spiritBurst"));

        harness.WithTargetMonster(m => m.StatSheet.SetHp(100000));
        harness.Target!.WarpTo(harness.Source.DirectionalOffset(harness.Source.Direction));

        var hpBefore = harness.Target.StatSheet.CurrentHp;
        harness.Use();

        harness.Target.StatSheet.CurrentHp
               .Should()
               .BeLessThan(hpBefore, "Spirit Burst should damage targets in its frontal cone");
    }

    [Test]
    public void Unravel_ShouldStripBuffsFromAHostileTarget()
    {
        var harness = new SpellScriptHarness<UnravelScript>(spellSetup: s => EnsureSpellVars(s, "unravel"));

        harness.WithTargetMonster();
        harness.Target!.Effects.Apply(harness.Target, new BattleHymnEffect { FlatDamageBonus = 10 }, MockSkill.Create().Script);

        harness.Use();

        harness.Target.Effects.TryGetEffect("Battle Hymn", out _)
               .Should()
               .BeFalse("Unravel should strip known buffs from a hostile target");
    }

    [Test]
    public void Regression_ShouldCleanseDebuffsFromAnAlly()
    {
        var harness = new SpellScriptHarness<RegressionScript>(spellSetup: s => EnsureSpellVars(s, "regression"));

        harness.WithTargetAisling();
        harness.Target!.Effects.Apply(harness.Target, new RootEffect(), MockSkill.Create().Script);

        harness.Use();

        harness.Target.Effects.TryGetEffect("Root", out _)
               .Should()
               .BeFalse("Regression should cleanse known debuffs from an ally");
    }

    [Test]
    public void SoulTether_ShouldShareAPortionOfHealingWithTheBoundPartner()
    {
        var applyHealScript = ApplyHealScript.Create();
        var harness = new SpellScriptHarness<SoulTetherScript>(spellSetup: s => EnsureSpellVars(s, "soulTether"));

        harness.WithTargetAisling(a =>
        {
            a.StatSheet.AddBonus(new Attributes { MaximumHp = 100000 });
            a.StatSheet.SetHp(1000);
        });

        harness.Use();

        var caster = harness.Source;
        var partner = harness.Target!;
        caster.StatSheet.AddBonus(new Attributes { MaximumHp = 100000 });
        caster.StatSheet.SetHp(1000);

        var casterHpBefore = caster.StatSheet.CurrentHp;
        var partnerHpBefore = partner.StatSheet.CurrentHp;

        //heal the partner directly - some of it should also flow back to the tethered caster
        applyHealScript.ApplyHeal(MockAisling.Create(), partner, MockSkill.Create().Script, 500);

        partner.StatSheet.CurrentHp
               .Should()
               .BeGreaterThan(partnerHpBefore, "Soul Tether shouldn't prevent the target from being healed");

        caster.StatSheet.CurrentHp
              .Should()
              .BeGreaterThan(casterHpBefore, "Soul Tether should share a portion of the partner's healing back to the caster");
    }

    [Test]
    public void Bloomkeeper_ShouldIncreaseHealingEffectiveness_PerActiveBloomingLife()
    {
        var applyHealScript = ApplyHealScript.Create();
        var mystic = MockAisling.Create();
        mystic.UserStatSheet.SetBaseClass(BaseClass.Mystic);
        var bloomkeeperSkill = MockSkill.Create(name: "Bloomkeeper", templateSetup: t => t with { TemplateKey = "bloomkeeper" });
        mystic.SkillBook.TryAddToNextSlot(bloomkeeperSkill);

        var target = MockAisling.Create();
        target.StatSheet.AddBonus(new Attributes { MaximumHp = 100000 });

        target.StatSheet.SetHp(1);
        var hpBeforeNoStacks = target.StatSheet.CurrentHp;
        applyHealScript.ApplyHeal(mystic, target, MockSkill.Create().Script, 100);
        var healingWithNoActiveBlooms = target.StatSheet.CurrentHp - hpBeforeNoStacks;

        //simulate 3 active Blooming Lifes out on other allies
        mystic.Trackers.Counters.Set(BloomingLifeEffect.ActiveCountCounterKey, 3);

        target.StatSheet.SetHp(1);
        var hpBeforeStacked = target.StatSheet.CurrentHp;
        applyHealScript.ApplyHeal(mystic, target, MockSkill.Create().Script, 100);
        var healingWithActiveBlooms = target.StatSheet.CurrentHp - hpBeforeStacked;

        healingWithActiveBlooms.Should()
                               .BeGreaterThan(healingWithNoActiveBlooms, "Bloomkeeper should boost healing per active Blooming Life");
    }

    [Test]
    public void SpiritOverflow_ShouldConvertFullHealthHealingIntoMana()
    {
        var applyHealScript = ApplyHealScript.Create();
        var mystic = MockAisling.Create();
        mystic.UserStatSheet.SetBaseClass(BaseClass.Mystic);
        var overflowSkill = MockSkill.Create(name: "Spirit Overflow", templateSetup: t => t with { TemplateKey = "spirit_overflow" });
        mystic.SkillBook.TryAddToNextSlot(overflowSkill);

        mystic.StatSheet.AddBonus(new Attributes { MaximumHp = 1000, MaximumMp = 1000 });
        mystic.StatSheet.SetHp(1000);
        mystic.StatSheet.SetMp(0);

        var hpBefore = mystic.StatSheet.CurrentHp;
        var mpBefore = mystic.StatSheet.CurrentMp;

        applyHealScript.ApplyHeal(MockAisling.Create(), mystic, MockSkill.Create().Script, 100);

        mystic.StatSheet.CurrentHp
              .Should()
              .Be(hpBefore, "healing at full Health shouldn't overheal HP once Spirit Overflow is learned");

        mystic.StatSheet.CurrentMp
              .Should()
              .BeGreaterThan(mpBefore, "Spirit Overflow should convert the wasted heal into Mana instead");
    }

    [Test]
    public void SpiritualAttunement_ShouldEmpowerTheNextNonHealingSpell_AfterAHealingCast()
    {
        var harness = new AislingScriptHarness<SpiritualAttunementScript>();
        harness.Source.UserStatSheet.SetBaseClass(BaseClass.Mystic);

        var attunementSkill = MockSkill.Create(name: "Spiritual Attunement", templateSetup: t => t with { TemplateKey = "spiritual_attunement" });
        harness.Source.SkillBook.TryAddToNextSlot(attunementSkill);

        var healingSpell = MockSpell.Create(templateSetup: t => t with { TemplateKey = "blooming_life" });
        harness.Source.Trackers.LastUsedSpell = healingSpell;
        harness.Source.Trackers.LastSpellUse = DateTime.UtcNow;
        harness.Update(TimeSpan.FromMilliseconds(1));

        harness.Source.Effects.TryGetEffect("Spiritual Attunement", out _)
               .Should()
               .BeTrue("casting a healing spell should grant Spiritual Attunement");

        var nonHealingSpell = MockSpell.Create(templateSetup: t => t with { TemplateKey = "spirit_burst" });
        harness.Source.Trackers.LastUsedSpell = nonHealingSpell;
        harness.Source.Trackers.LastSpellUse = DateTime.UtcNow.AddMilliseconds(50);
        harness.Update(TimeSpan.FromMilliseconds(1));

        harness.Source.Effects.TryGetEffect("Spiritual Attunement", out _)
               .Should()
               .BeFalse("the next non-healing spell cast should consume Spiritual Attunement");
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
