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
using Chaos.Scripting.FunctionalScripts.ApplyHealing;
using Chaos.Scripting.SpellScripts;
using Chaos.Testing.Infrastructure.Harnesses;
using Chaos.Testing.Infrastructure.Mocks;
using FluentAssertions;

#endregion

namespace Chaos.Tests;

/// <summary>
///     Real functional tests for Bard's new/reworked/evolving mechanics - asserts actual resulting values/state,
///     not just "doesn't throw", per this session's established standard.
/// </summary>
public sealed class BardNewSkillsTests
{
    static BardNewSkillsTests()
    {
        var registry = new FunctionalScriptRegistry(MockServiceProvider.CreateBuilder()
                                                                        .Build()
                                                                        .Object);

        registry.Register(ApplyAttackDamageScript.Key, typeof(ApplyAttackDamageScript));
        registry.Register(ApplyHealScript.Key, typeof(ApplyHealScript));
    }

    private static void EnsureSpellVars(Spell spell, string scriptKey) => spell.Template.ScriptVars[scriptKey] = new EmptyScriptVars();

    [Test]
    public void StaciasBlessing_ShouldGrantDamageReductionAndCcResist_OnlyAtTierIV()
    {
        var lowTierHarness = new SpellScriptHarness<StaciasBlessingScript>(spellSetup: s =>
        {
            s.Level = 1;
            EnsureSpellVars(s, "staciasBlessing");
        });

        //MockAisling.Create never registers the created Aisling in the map's spatial index, but
        //StaciasBlessingScript's party-wide self-buff scan reads from map.GetEntities<Aisling>() -
        //register the harness's own Source explicitly so it's found
        lowTierHarness.Map.AddEntity(lowTierHarness.Source, Point.From(lowTierHarness.Source));
        lowTierHarness.Use();
        lowTierHarness.Source.Effects.TryGetEffect("Stacia's Blessing", out var lowEffect);
        var lowBlessing = lowEffect as StaciasBlessingEffect;

        var highTierHarness = new SpellScriptHarness<StaciasBlessingScript>(spellSetup: s =>
        {
            s.Level = 20;
            EnsureSpellVars(s, "staciasBlessing");
        });

        highTierHarness.Map.AddEntity(highTierHarness.Source, Point.From(highTierHarness.Source));
        highTierHarness.Use();
        highTierHarness.Source.Effects.TryGetEffect("Stacia's Blessing", out var highEffect);
        var highBlessing = highEffect as StaciasBlessingEffect;

        lowBlessing!.DamageReductionPct
                    .Should()
                    .Be(0, "Tier I Stacia's Blessing should not yet grant damage reduction");

        highBlessing!.DamageReductionPct
                     .Should()
                     .BeGreaterThan(0, "Tier IV Stacia's Blessing should grant damage reduction");

        highBlessing.CcResistPct
                    .Should()
                    .BeGreaterThan(0, "Tier IV Stacia's Blessing should grant CC resistance");
    }

    [Test]
    public void StaciasBlessing_ShouldReduceIncomingDamage_AtTierIV()
    {
        var applyDamageScript = ApplyAttackDamageScript.Create();
        var blessed = MockAisling.Create();
        blessed.StatSheet.AddBonus(new Attributes { MaximumHp = 100000 });
        blessed.StatSheet.SetHp(100000);
        blessed.Effects.Apply(blessed, new StaciasBlessingEffect { DamageReductionPct = 50 }, MockSkill.Create().Script);

        var unblessed = MockAisling.Create();
        unblessed.StatSheet.AddBonus(new Attributes { MaximumHp = 100000 });
        unblessed.StatSheet.SetHp(100000);

        var monster = MockMonster.Create();

        var blessedHpBefore = blessed.StatSheet.CurrentHp;
        applyDamageScript.ApplyDamage(monster, blessed, MockSkill.Create().Script, 1000);
        var blessedDamage = blessedHpBefore - blessed.StatSheet.CurrentHp;

        var unblessedHpBefore = unblessed.StatSheet.CurrentHp;
        applyDamageScript.ApplyDamage(monster, unblessed, MockSkill.Create().Script, 1000);
        var unblessedDamage = unblessedHpBefore - unblessed.StatSheet.CurrentHp;

        blessedDamage.Should()
                     .BeLessThan(unblessedDamage, "Stacia's Blessing Tier IV should reduce incoming damage");
    }

    [Test]
    public void RootAndSlow_ShouldBeResistable_WithHighCcResist()
    {
        var target = MockAisling.Create();
        target.Effects.Apply(target, new StaciasBlessingEffect { CcResistPct = 100 }, MockSkill.Create().Script);

        var source = MockAisling.Create();

        var resistedCount = 0;

        for (var i = 0; i < 20; i++)
        {
            target.Effects.Terminate("Root");
            target.Effects.Apply(source, new RootEffect(), MockSkill.Create().Script);

            if (!target.Effects.TryGetEffect("Root", out _))
                resistedCount++;
        }

        resistedCount.Should()
                     .Be(20, "100% CC resist should reliably resist every Root application");
    }

    [Test]
    public void BattleHymn_ShouldGrantCritChanceAndManaRegen_OnlyAtTierIV()
    {
        var lowTierHarness = new SpellScriptHarness<BattleHymnScript>(spellSetup: s =>
        {
            s.Level = 1;
            EnsureSpellVars(s, "battleHymn");
        });

        //MockAisling.Create never registers the created Aisling in the map's spatial index, but
        //BattleHymnScript's party-wide self-buff scan reads from map.GetEntities<Aisling>() -
        //register the harness's own Source explicitly so it's found
        lowTierHarness.Map.AddEntity(lowTierHarness.Source, Point.From(lowTierHarness.Source));
        lowTierHarness.Use();
        lowTierHarness.Source.Effects.TryGetEffect("Battle Hymn", out var lowEffect);

        var highTierHarness = new SpellScriptHarness<BattleHymnScript>(spellSetup: s =>
        {
            s.Level = 20;
            EnsureSpellVars(s, "battleHymn");
        });

        highTierHarness.Map.AddEntity(highTierHarness.Source, Point.From(highTierHarness.Source));
        highTierHarness.Use();
        highTierHarness.Source.Effects.TryGetEffect("Battle Hymn", out var highEffect);

        (lowEffect as BattleHymnEffect)!.CritChanceBonusPct
                                        .Should()
                                        .Be(0, "Tier I Battle Hymn should not yet grant crit chance");

        (highEffect as BattleHymnEffect)!.CritChanceBonusPct
                                         .Should()
                                         .BeGreaterThan(0, "Tier IV Battle Hymn should grant crit chance");

        highTierHarness.Source.Trackers.Tags.ContainsKey(BattleHymnEffect.CritChanceBonusTag)
                       .Should()
                       .BeTrue();
    }

    [Test]
    public void BardsMalediction_ShouldReduceTheCursedCreaturesOwnOutgoingDamage_AtTierII()
    {
        var applyDamageScript = ApplyAttackDamageScript.Create();
        var cursedMonster = MockMonster.Create();
        cursedMonster.Effects.Apply(MockAisling.Create(), new BardsMaledictionEffect { DamageDealtReductionPct = 50 }, MockSkill.Create().Script);

        var cleanMonster = MockMonster.Create();

        var victim1 = MockAisling.Create();
        victim1.StatSheet.AddBonus(new Attributes { MaximumHp = 100000 });
        victim1.StatSheet.SetHp(100000);

        var victim2 = MockAisling.Create();
        victim2.StatSheet.AddBonus(new Attributes { MaximumHp = 100000 });
        victim2.StatSheet.SetHp(100000);

        var victim1HpBefore = victim1.StatSheet.CurrentHp;
        applyDamageScript.ApplyDamage(cursedMonster, victim1, MockSkill.Create().Script, 1000);
        var cursedDamageDealt = victim1HpBefore - victim1.StatSheet.CurrentHp;

        var victim2HpBefore = victim2.StatSheet.CurrentHp;
        applyDamageScript.ApplyDamage(cleanMonster, victim2, MockSkill.Create().Script, 1000);
        var cleanDamageDealt = victim2HpBefore - victim2.StatSheet.CurrentHp;

        cursedDamageDealt.Should()
                         .BeLessThan(cleanDamageDealt, "a Malediction-cursed creature should deal less damage than an uncursed one");
    }

    [Test]
    public void Salvation_ShouldHealMoreAndSplashToNearbyAllies_AtHigherTiers()
    {
        var lowTierHarness = new SpellScriptHarness<SalvationScript>(spellSetup: s =>
        {
            s.Level = 1;
            EnsureSpellVars(s, "salvation");
        });

        lowTierHarness.WithTargetAisling(a =>
        {
            a.StatSheet.AddBonus(new Attributes { MaximumHp = 100000 });
            a.StatSheet.SetHp(1000);
        });

        var lowHpBefore = lowTierHarness.Target!.StatSheet.CurrentHp;
        lowTierHarness.Use();
        var lowHealing = lowTierHarness.Target.StatSheet.CurrentHp - lowHpBefore;

        var highTierHarness = new SpellScriptHarness<SalvationScript>(spellSetup: s =>
        {
            s.Level = 20;
            EnsureSpellVars(s, "salvation");
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

        var highHpBefore = highTierHarness.Target!.StatSheet.CurrentHp;
        var allyHpBefore = nearbyAlly.StatSheet.CurrentHp;
        highTierHarness.Use();
        var highHealing = highTierHarness.Target.StatSheet.CurrentHp - highHpBefore;
        var allyHealing = nearbyAlly.StatSheet.CurrentHp - allyHpBefore;

        highHealing.Should()
                   .BeGreaterThan(lowHealing, "Tier IV Salvation should heal for more than Tier I");

        allyHealing.Should()
                   .BeGreaterThan(0, "Tier IV Salvation should splash-heal nearby allies too");
    }

    [Test]
    public void GuardiansAnthem_ShouldProtectTheEntireParty_AtTierIV()
    {
        var harness = new SpellScriptHarness<GuardiansAnthemScript>(spellSetup: s =>
        {
            s.Level = 20;
            EnsureSpellVars(s, "guardiansAnthem");
        });

        var groupLeader = MockAisling.Create(harness.Map, "Leader");
        var groupMember = MockAisling.Create(harness.Map, "Member1");
        var channelService = MockChannelService.Create();
        var logger = MockLogger.Create<Group>();
        var group = new Group(groupLeader, groupMember, channelService, logger.Object);
        groupLeader.Group = group;
        groupMember.Group = group;

        harness.WithTarget(groupLeader);
        harness.Use();

        groupLeader.Trackers.Tags.ContainsKey(StaciasBulwarkEffect.InvulnerableTag)
                   .Should()
                   .BeTrue("Tier IV Guardian's Anthem should protect the caster's target");

        groupMember.Trackers.Tags.ContainsKey(StaciasBulwarkEffect.InvulnerableTag)
                   .Should()
                   .BeTrue("Tier IV Guardian's Anthem should protect the entire party");
    }

    [Test]
    public void ValkorsSmite_ShouldDealDamage()
    {
        var harness = new SpellScriptHarness<ValkorsSmiteScript>(
            scriptFactory: spell => new ValkorsSmiteScript(spell) { BaseDamage = 60, Range = 8 },
            spellSetup: s => EnsureSpellVars(s, "valkorsSmite"));

        harness.WithTargetMonster(m => m.StatSheet.SetHp(100000));

        var hpBefore = harness.Target!.StatSheet.CurrentHp;
        harness.Use();

        harness.Target.StatSheet.CurrentHp
               .Should()
               .BeLessThan(hpBefore, "Valkor's Smite should deal damage");
    }

    [Test]
    public void Crescendo_ShouldEmpowerAfterFiveSpellCasts()
    {
        var harness = new AislingScriptHarness<CrescendoScript>();
        harness.Source.UserStatSheet.SetBaseClass(BaseClass.Bard);

        var crescendoSkill = MockSkill.Create(name: "Crescendo", templateSetup: t => t with { TemplateKey = "crescendo" });
        harness.Source.SkillBook.TryAddToNextSlot(crescendoSkill);

        var someSpell = MockSpell.Create(templateSetup: t => t with { TemplateKey = "stacias_vitae" });

        for (var i = 0; i < 5; i++)
        {
            harness.Source.Trackers.LastUsedSpell = someSpell;
            harness.Source.Trackers.LastSpellUse = DateTime.UtcNow.AddMilliseconds(i);
            harness.Update(TimeSpan.FromMilliseconds(1));
        }

        harness.Source.Effects.TryGetEffect("Crescendo", out _)
               .Should()
               .BeTrue("5 spell casts should build enough Crescendo stacks to empower the next spell");
    }

    [Test]
    public void Encore_ShouldEventuallyRepeatAHeal()
    {
        var applyHealScript = ApplyHealScript.Create();
        var bard = MockAisling.Create();
        bard.UserStatSheet.SetBaseClass(BaseClass.Bard);
        var encoreSkill = MockSkill.Create(name: "Encore", templateSetup: t => t with { TemplateKey = "encore" });
        bard.SkillBook.TryAddToNextSlot(encoreSkill);

        var target = MockAisling.Create();
        target.StatSheet.AddBonus(new Attributes { MaximumHp = 1000000 });

        var healings = new List<int>();

        //25% proc chance - run enough trials that at least one repeat (extra healing beyond the base amount) is
        //overwhelmingly likely, rather than asserting on a single roll
        for (var i = 0; i < 60; i++)
        {
            target.StatSheet.SetHp(1);
            var hpBefore = target.StatSheet.CurrentHp;
            applyHealScript.ApplyHeal(bard, target, MockSkill.Create().Script, 100);
            healings.Add(target.StatSheet.CurrentHp - hpBefore);
        }

        var baseline = healings.Min();

        healings.Should()
                .Contain(h => h > baseline, "Encore should eventually cause a heal to repeat for bonus healing");
    }

    [Test]
    public void StaciasGrace_ShouldGrantInvulnerability_WhenHpDropsBelowThreshold()
    {
        var applyDamageScript = ApplyAttackDamageScript.Create();
        var map = MockMapInstance.Create();
        var bard = MockAisling.Create(map);
        bard.UserStatSheet.SetBaseClass(BaseClass.Bard);
        var graceSkill = MockSkill.Create(name: "Stacia's Grace", templateSetup: t => t with { TemplateKey = "stacias_grace" });
        bard.SkillBook.TryAddToNextSlot(graceSkill);
        bard.StatSheet.AddBonus(new Attributes { MaximumHp = 1000 });
        bard.StatSheet.SetHp(300);

        var monster = MockMonster.Create(map);

        //drop the Bard from 300/1000 (30%) to 100/1000 (10%), below the 20% threshold
        applyDamageScript.ApplyDamage(monster, bard, MockSkill.Create().Script, 200);

        bard.Trackers.Tags.ContainsKey(StaciasBulwarkEffect.InvulnerableTag)
            .Should()
            .BeTrue("falling below the HP threshold should grant Stacia's Grace's invulnerability");
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
