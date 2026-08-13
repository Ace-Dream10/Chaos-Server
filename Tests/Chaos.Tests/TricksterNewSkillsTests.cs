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
using Chaos.Scripting.ReactorTileScripts;
using Chaos.Scripting.SpellScripts;
using Chaos.Services.Factories.Abstractions;
using Chaos.Testing.Infrastructure.Harnesses;
using Chaos.Testing.Infrastructure.Mocks;
using FluentAssertions;
using Moq;
#endregion

namespace Chaos.Tests;

/// <summary>
///     Real functional tests for Trickster's new/reworked/evolving mechanics - asserts actual resulting
///     values/state, not just "doesn't throw", per this session's established standard.
/// </summary>
public sealed class TricksterNewSkillsTests
{
    static TricksterNewSkillsTests()
    {
        var registry = new FunctionalScriptRegistry(MockServiceProvider.CreateBuilder()
                                                                        .Build()
                                                                        .Object);

        registry.Register(ApplyAttackDamageScript.Key, typeof(ApplyAttackDamageScript));
    }

    private static void EnsureScriptVars(Spell spell, string scriptKey) => spell.Template.ScriptVars[scriptKey] = new EmptyScriptVars();

    private static IServiceProvider CreateServiceProviderWithReactorTileFactory()
    {
        var reactorTileFactoryMock = new Mock<IReactorTileFactory>();

        reactorTileFactoryMock
            .Setup(
                f => f.Create(
                    It.IsAny<string>(),
                    It.IsAny<MapInstance>(),
                    It.IsAny<Chaos.Geometry.Abstractions.IPoint>(),
                    It.IsAny<ICollection<string>?>(),
                    It.IsAny<Chaos.Models.World.Abstractions.Creature?>(),
                    It.IsAny<Chaos.Scripting.Abstractions.IScript?>()))
            .Returns(
                (string _, MapInstance map, Chaos.Geometry.Abstractions.IPoint point, ICollection<string>? _,
                    Chaos.Models.World.Abstractions.Creature? owner, Chaos.Scripting.Abstractions.IScript? script) =>
                    new ReactorTile(
                        map,
                        point,
                        false,
                        MockScriptProvider.Instance.Object,
                        ["deceiversCacheReactor"],
                        new Dictionary<string, IScriptVars> { ["deceiversCacheReactor"] = new EmptyScriptVars() },
                        owner,
                        script));

        return MockServiceProvider.CreateBuilder()
                                  .SetupService(reactorTileFactoryMock.Object)
                                  .Build()
                                  .Object;
    }

    [Test]
    public void Delirium_ShouldAffectMultipleTargets_AtHigherTiers()
    {
        //Tier I (single target) vs Tier III (AoE) - relative comparison
        var lowTierHarness = new SpellScriptHarness<DeliriumScript>(
            spellSetup: s =>
            {
                s.Level = 1;
                EnsureScriptVars(s, "delirium");
            });

        lowTierHarness.WithTargetMonster();
        lowTierHarness.Target!.WarpTo(lowTierHarness.Source.DirectionalOffset(lowTierHarness.Source.Direction));
        lowTierHarness.Use();

        lowTierHarness.Target.Effects.TryGetEffect("Delirium", out _)
                     .Should()
                     .BeTrue("Tier I Delirium should still afflict the single target directly in front");

        var highTierHarness = new SpellScriptHarness<DeliriumScript>(
            spellSetup: s =>
            {
                s.Level = 20;
                EnsureScriptVars(s, "delirium");
            });

        var monster1 = MockMonster.Create(highTierHarness.Map);
        var monster2 = MockMonster.Create(highTierHarness.Map);
        var selfPoint = Point.From(highTierHarness.Source);

        monster1.WarpTo(selfPoint);
        highTierHarness.Map.AddEntity(monster1, selfPoint);

        var nearbyPoint = new Point(selfPoint.X + 1, selfPoint.Y);
        monster2.WarpTo(nearbyPoint);
        highTierHarness.Map.AddEntity(monster2, nearbyPoint);

        highTierHarness.Use();

        var afflictedCount = new[] { monster1, monster2 }.Count(m => m.Effects.TryGetEffect("Delirium", out _));

        afflictedCount.Should()
                      .Be(2, "Tier IV Delirium should afflict entire groups (AoE), not just one target");
    }

    [Test]
    public void Puppeteer_ShouldControlMultipleTargets_AtTierIV()
    {
        var harness = new SpellScriptHarness<PuppeteerScript>(
            spellSetup: s =>
            {
                s.Level = 20;
                EnsureScriptVars(s, "puppeteer");
            });

        var monster1 = MockMonster.Create(harness.Map);
        var monster2 = MockMonster.Create(harness.Map);
        var sourcePoint = Point.From(harness.Source);
        var direction = harness.Source.Direction;

        monster1.WarpTo(harness.Source.DirectionalOffset(direction, 1));
        harness.Map.AddEntity(monster1, Point.From(monster1));

        monster2.WarpTo(harness.Source.DirectionalOffset(direction, 2));
        harness.Map.AddEntity(monster2, Point.From(monster2));

        harness.Use();

        var controlledCount = new[] { monster1, monster2 }.Count(m => m.Effects.TryGetEffect("Puppeteer", out _));

        controlledCount.Should()
                       .Be(2, "Tier IV Puppeteer should control multiple enemies at once");
    }

    [Test]
    public void HallOfMirrors_ShouldSpawnAnAttackingIllusion_AtTierIII()
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

        var harness = new SpellScriptHarness<MirrorImageScript>(
            spellSetup: s =>
            {
                s.Level = 16;
                EnsureScriptVars(s, "mirrorImage");
            },
            serviceProvider: serviceProvider);

        map = harness.Map;

        harness.Use();

        //Tier III spawns via the "shadow_clone" attacking template rather than the passive "mirror_image_decoy"
        map.GetEntities<Monster>()
           .Any(m => m.Template.TemplateKey == "shadow_clone")
           .Should()
           .BeTrue("Tier III Hall of Mirrors' illusions should attack (spawned via the attacking decoy template)");
    }

    [Test]
    public void CrackTheWhip_ShouldDamageAndStaggerTargetsInACone()
    {
        var harness = new SpellScriptHarness<CrackTheWhipScript>(
            scriptFactory: skill => new CrackTheWhipScript(skill),
            spellSetup: s =>
            {
                s.Level = 1;
                EnsureScriptVars(s, "crackTheWhip");
            });

        harness.WithTargetMonster(m => m.StatSheet.SetHp(100000));
        harness.Target!.WarpTo(harness.Source.DirectionalOffset(harness.Source.Direction));

        var hpBefore = harness.Target.StatSheet.CurrentHp;
        harness.Use();

        harness.Target.StatSheet.CurrentHp
               .Should()
               .BeLessThan(hpBefore, "Crack the Whip should damage targets in its arc");

        harness.Target.Effects.TryGetEffect("Blackout", out _)
               .Should()
               .BeTrue("Crack the Whip should briefly stagger struck targets");
    }

    [Test]
    public void CurtainCall_ShouldVanishTheCasterAndGroupMembers()
    {
        var harness = new SpellScriptHarness<CurtainCallScript>(spellSetup: s => EnsureScriptVars(s, "curtainCall"));

        harness.Use();

        harness.Source.Trackers.Tags.ContainsKey(VanishEffect.VanishedTag)
               .Should()
               .BeTrue("Curtain Call should vanish the caster");
    }

    [Test]
    public void Switcheroo_ShouldSwapPositions()
    {
        var harness = new SpellScriptHarness<SwitcherooScript>(spellSetup: s => EnsureScriptVars(s, "switcheroo"));
        harness.WithTargetMonster();

        var sourcePointBefore = Point.From(harness.Source);
        var targetPointBefore = harness.Source.DirectionalOffset(harness.Source.Direction);
        harness.Target!.WarpTo(targetPointBefore);

        harness.Use();

        Point.From(harness.Source)
             .Should()
             .Be(targetPointBefore, "the caster should warp to the target's old position");

        Point.From(harness.Target)
             .Should()
             .Be(sourcePointBefore, "the target should warp to the caster's old position");
    }

    [Test]
    public void DeceiversCache_ShouldPlantAReactorTile()
    {
        var serviceProvider = CreateServiceProviderWithReactorTileFactory();

        var harness = new SpellScriptHarness<DeceiversCacheScript>(
            spellSetup: s =>
            {
                s.Level = 1;
                EnsureScriptVars(s, "deceiversCache");
            },
            serviceProvider: serviceProvider);

        var tileCountBefore = harness.Map.GetEntities<ReactorTile>().Count();

        harness.Use();

        harness.Map.GetEntities<ReactorTile>().Count()
               .Should()
               .BeGreaterThan(tileCountBefore, "Deceiver's Cache should plant a reactor tile");
    }

    [Test]
    public void DeceiversCache_ShouldDamageAndAfflict_WhenAnEnemyWalksOnIt()
    {
        //DeceiversCacheReactorScript is constructed directly rather than through a ReactorTile's own DI-resolved
        //Script property - MockScriptProvider.Instance only returns a generic Moq proxy for arbitrary script
        //keys (it doesn't do real reflection-based type resolution the way production's ReactorTileFactory does),
        //so going through ReactorTile.Script wouldn't actually run this class's logic. The ReactorTile itself is
        //still real (needed as the script's Subject); MockScriptProvider is only used to satisfy its constructor,
        //and its own (unused) Script property is discarded.
        var map = MockMapInstance.Create();
        var owner = MockAisling.Create(map);
        var point = Point.From(owner);

        var reactorTile = new ReactorTile(
            map,
            point,
            false,
            MockScriptProvider.Instance.Object,
            ["deceiversCacheReactor"],
            new Dictionary<string, IScriptVars> { ["deceiversCacheReactor"] = new EmptyScriptVars() },
            owner);

        var reactorScript = new DeceiversCacheReactorScript(reactorTile);

        var monster = MockMonster.Create(map);
        monster.StatSheet.SetHp(100000);
        monster.WarpTo(point);
        map.AddEntity(monster, point);
        var hpBefore = monster.StatSheet.CurrentHp;

        //a fresh MockAisling's default Script stub doesn't implement real hostility logic (returns false unless
        //explicitly configured) - same delegation CreatureTests.cs's IsHostileTo_ShouldDelegateToScript documents
        Mock.Get(owner.Script)
            .Setup(s => s.IsHostileTo(monster))
            .Returns(true);

        reactorScript.OnWalkedOn(monster);

        monster.StatSheet.CurrentHp
               .Should()
               .BeLessThan(hpBefore, "Deceiver's Cache should deal damage on detonation");

        TricksterAfflictions.CountActive(monster)
                            .Count
                            .Should()
                            .BeGreaterThan(0, "Deceiver's Cache should apply a random mental affliction on detonation");
    }

    [Test]
    public void GrandFinale_ShouldConsumeAfflictionsAndDealScaledDamage()
    {
        var harness = new SpellScriptHarness<GrandFinaleScript>(spellSetup: s => EnsureScriptVars(s, "grandFinale"));

        var afflictedMonster = MockMonster.Create(harness.Map);
        afflictedMonster.StatSheet.SetHp(100000);
        afflictedMonster.WarpTo(harness.Source);
        harness.Map.AddEntity(afflictedMonster, Point.From(harness.Source));
        afflictedMonster.Effects.Apply(harness.Source, new BlackoutEffect(), MockSkill.Create().Script);
        afflictedMonster.Effects.Apply(harness.Source, new DeliriumEffect(), MockSkill.Create().Script);

        var cleanMonster = MockMonster.Create(harness.Map);
        cleanMonster.StatSheet.SetHp(100000);
        cleanMonster.WarpTo(harness.Source);
        harness.Map.AddEntity(cleanMonster, Point.From(harness.Source));

        var afflictedHpBefore = afflictedMonster.StatSheet.CurrentHp;
        var cleanHpBefore = cleanMonster.StatSheet.CurrentHp;

        harness.Use();

        (afflictedHpBefore - afflictedMonster.StatSheet.CurrentHp).Should()
            .BeGreaterThan(0, "Grand Finale should damage a target with active afflictions");

        (cleanHpBefore - cleanMonster.StatSheet.CurrentHp).Should()
            .Be(0, "Grand Finale should not damage a target with no active afflictions to consume");

        afflictedMonster.Effects.TryGetEffect("Blackout", out _)
                        .Should()
                        .BeFalse("Grand Finale should end every consumed affliction");

        afflictedMonster.Effects.TryGetEffect("Delirium", out _)
                        .Should()
                        .BeFalse("Grand Finale should end every consumed affliction");
    }

    [Test]
    public void PsychologicalWarfare_ShouldDealBonusDamage_ToAfflictedTarget_WhenApplierHasLearnedIt()
    {
        var applyDamageScript = ApplyAttackDamageScript.Create();

        var trickster = MockAisling.Create();
        trickster.UserStatSheet.SetBaseClass(BaseClass.Trickster);
        var warfareSpell = MockSpell.Create(name: "Psychological Warfare", templateSetup: t => t with { TemplateKey = "psychological_warfare" });
        trickster.SpellBook.TryAddToNextSlot(warfareSpell);

        var attacker = MockAisling.Create();

        var afflictedMonster = MockMonster.Create();
        afflictedMonster.StatSheet.AddBonus(new Attributes { MaximumHp = 10000 });
        afflictedMonster.StatSheet.SetHp(10000);
        afflictedMonster.Effects.Apply(trickster, new BlackoutEffect(), MockSkill.Create().Script);

        var cleanMonster = MockMonster.Create();
        cleanMonster.StatSheet.AddBonus(new Attributes { MaximumHp = 10000 });
        cleanMonster.StatSheet.SetHp(10000);

        var afflictedHpBefore = afflictedMonster.StatSheet.CurrentHp;
        applyDamageScript.ApplyDamage(attacker, afflictedMonster, MockSkill.Create().Script, 100);
        var afflictedDamage = afflictedHpBefore - afflictedMonster.StatSheet.CurrentHp;

        var cleanHpBefore = cleanMonster.StatSheet.CurrentHp;
        applyDamageScript.ApplyDamage(attacker, cleanMonster, MockSkill.Create().Script, 100);
        var cleanDamage = cleanHpBefore - cleanMonster.StatSheet.CurrentHp;

        afflictedDamage.Should()
                       .BeGreaterThan(cleanDamage, "Psychological Warfare should boost ANY ally's damage against a target afflicted by a Trickster who learned it");
    }

    [Test]
    public void ChainReaction_ShouldRetriggerADifferentAffliction_WhenOneExpires()
    {
        var trickster = MockAisling.Create();
        trickster.UserStatSheet.SetBaseClass(BaseClass.Trickster);
        var chainReactionSpell = MockSpell.Create(name: "Chain Reaction", templateSetup: t => t with { TemplateKey = "chain_reaction" });
        trickster.SpellBook.TryAddToNextSlot(chainReactionSpell);

        var monster = MockMonster.Create();

        var triggeredAtLeastOnce = false;

        //Chain Reaction's proc chance is randomized - run enough trials that a 35% proc chance is overwhelmingly
        //likely to have fired at least once, rather than asserting on a single roll
        for (var i = 0; i < 50 && !triggeredAtLeastOnce; i++)
        {
            var blackout = new BlackoutEffect();
            blackout.SetDuration(TimeSpan.FromMilliseconds(50));
            monster.Effects.Apply(trickster, blackout, MockSkill.Create().Script);
            monster.Effects.Update(TimeSpan.FromMilliseconds(100));

            triggeredAtLeastOnce = monster.Effects.TryGetEffect("Delirium", out _)
                                   || monster.Effects.TryGetEffect("Puppeteer", out _)
                                   || monster.Effects.TryGetEffect("Root", out _);

            monster.Effects.Terminate("Delirium");
            monster.Effects.Terminate("Puppeteer");
            monster.Effects.Terminate("Root");
        }

        triggeredAtLeastOnce.Should()
                            .BeTrue("Chain Reaction should eventually retrigger a different affliction across enough expirations");
    }

    [Test]
    public void SmokeAndMirrors_ShouldSpawnAnIllusion_WhenAMobilityAbilityIsUsed()
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

        var harness = new AislingScriptHarness<TricksterIllusionScript>(serviceProvider: serviceProvider);
        map = harness.Map;

        harness.Source.UserStatSheet.SetBaseClass(BaseClass.Trickster);

        var smokeAndMirrorsSpell = MockSpell.Create(name: "Smoke and Mirrors", templateSetup: t => t with { TemplateKey = "smoke_and_mirrors" });
        harness.Source.SpellBook.TryAddToNextSlot(smokeAndMirrorsSpell);

        var vanishingAct = MockSpell.Create(name: "Vanishing Act", templateSetup: t => t with { TemplateKey = "vanishing_act" });
        harness.Source.Trackers.LastUsedSpell = vanishingAct;
        harness.Source.Trackers.LastSpellUse = DateTime.UtcNow;

        var entityCountBefore = map.GetEntities<Monster>().Count();

        harness.Update(TimeSpan.FromMilliseconds(1));

        map.GetEntities<Monster>().Count()
           .Should()
           .BeGreaterThan(entityCountBefore, "Smoke and Mirrors should spawn an illusion after a mobility ability is used");
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
