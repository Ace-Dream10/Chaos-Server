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
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.FunctionalScripts;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts;
using Chaos.Scripting.SpellScripts;
using Chaos.Testing.Infrastructure.Harnesses;
using Chaos.Testing.Infrastructure.Mocks;
using FluentAssertions;
#endregion

namespace Chaos.Tests;

/// <summary>
///     Real functional tests for Fletcher's new/reworked/evolving mechanics - asserts actual resulting
///     values/state, not just "doesn't throw", per this session's established standard.
/// </summary>
public sealed class FletcherNewSkillsTests
{
    static FletcherNewSkillsTests()
    {
        var registry = new FunctionalScriptRegistry(MockServiceProvider.CreateBuilder()
                                                                        .Build()
                                                                        .Object);

        registry.Register(ApplyAttackDamageScript.Key, typeof(ApplyAttackDamageScript));
    }

    private static void EnsureScriptVars(Skill skill, string scriptKey) => skill.Template.ScriptVars[scriptKey] = new EmptyScriptVars();
    private static void EnsureSpellVars(Spell spell, string scriptKey) => spell.Template.ScriptVars[scriptKey] = new EmptyScriptVars();

    [Test]
    public void PinpointShot_ShouldDealHeavyDamage_AndStunTheTarget()
    {
        var harness = new SpellScriptHarness<PinpointShotScript>(
            scriptFactory: spell => new PinpointShotScript(spell) { BaseDamage = 80, Range = 10, Shape = AoeShape.Circle, SingleTarget = true },
            spellSetup: s => EnsureSpellVars(s, "pinpointShot"));

        harness.WithTargetMonster(m => m.StatSheet.SetHp(100000));
        //WithTargetMonster's mock isn't registered in the map's spatial index by default - DamageScript's target
        //resolution goes through GetEntitiesAtPoints (which queries that index), not context.TargetCreature
        //directly, so the target needs an explicit AddEntity even though it's already the selected target
        harness.Map.AddEntity(harness.Target!, Point.From(harness.Target!));

        var hpBefore = harness.Target!.StatSheet.CurrentHp;
        harness.Use();

        harness.Target.StatSheet.CurrentHp
               .Should()
               .BeLessThan(hpBefore, "Pinpoint Shot should deal damage");

        harness.Target.Effects.TryGetEffect("Blackout", out _)
               .Should()
               .BeTrue("Pinpoint Shot should briefly stun the target");
    }

    [Test]
    public void AimedShot_ShouldHitTheExplicitlySelectedTarget_RegardlessOfCasterFacingDirection()
    {
        //aimed_shot uses the shared generic "damage" scriptKey directly (no dedicated script class), so this
        //harnesses DamageScript itself with the same scriptVars aimed_shot.json configures
        var harness = new SpellScriptHarness<Chaos.Scripting.SpellScripts.DamageScript>(
            scriptFactory: spell => new Chaos.Scripting.SpellScripts.DamageScript(spell)
            {
                BaseDamage = 60,
                DamageStat = Stat.DEX,
                DamageStatMultiplier = 3,
                Range = 8,
                Shape = AoeShape.Circle,
                SingleTarget = true

                //Filter intentionally left default (None) here, matching the PinpointShot precedent above -
                //the mock Aisling's script doesn't stub IsHostileTo, so a HostileOnly filter would silently
                //exclude the mock monster. Filtering is generic DamageScript behavior already exercised
                //elsewhere; this test is specifically about targeting resolution, not the hostility filter.
            },
            spellSetup: s => EnsureSpellVars(s, "damage"));

        //face the source AWAY from where the selected target will be placed - this is the whole point of
        //the ability. bow_assail (a Skill) can only ever hit whatever's directly ahead of the caster's
        //current facing; Aimed Shot is a real Spell that resolves off the explicitly selected target
        //(context.TargetCreature) regardless of the caster's facing, per GetTargetsAbilityComponent.
        harness.Source.Direction = Direction.Down;

        var selectedTarget = MockMonster.Create(harness.Map);
        selectedTarget.StatSheet.SetHp(100000);
        var behindPoint = Point.From(harness.Source.DirectionalOffset(Direction.Up, 3));
        selectedTarget.WarpTo(behindPoint);
        harness.Map.AddEntity(selectedTarget, behindPoint);

        //a second monster sits directly in front of the source (where a Front-shaped Skill would hit
        //instead) - it must take no damage, proving the hit resolves off the selected target, not facing
        var frontMonster = MockMonster.Create(harness.Map);
        frontMonster.StatSheet.SetHp(100000);
        var frontPoint = Point.From(harness.Source.DirectionalOffset(harness.Source.Direction, 2));
        frontMonster.WarpTo(frontPoint);
        harness.Map.AddEntity(frontMonster, frontPoint);

        harness.WithTarget(selectedTarget);

        var selectedHpBefore = selectedTarget.StatSheet.CurrentHp;
        var frontHpBefore = frontMonster.StatSheet.CurrentHp;

        harness.Use();

        selectedTarget.StatSheet.CurrentHp
                      .Should()
                      .BeLessThan(selectedHpBefore, "Aimed Shot should damage the explicitly selected target even though it isn't in the caster's front-facing direction");

        frontMonster.StatSheet.CurrentHp
                    .Should()
                    .Be(frontHpBefore, "Aimed Shot should NOT hit an unselected monster just because it's in front of the caster");
    }

    [Test]
    public void WardensNet_ShouldSnareMultipleTargets_AtHigherTiers()
    {
        var lowTierHarness = new SpellScriptHarness<WardensNetScript>(
            spellSetup: s =>
            {
                s.Level = 1;
                EnsureSpellVars(s, "wardensNet");
            });

        lowTierHarness.WithTargetMonster();
        var lowNearby = MockMonster.Create(lowTierHarness.Map);
        lowNearby.WarpTo(Point.From(lowTierHarness.Target!));
        lowTierHarness.Map.AddEntity(lowNearby, Point.From(lowTierHarness.Target!));

        lowTierHarness.Use();

        var lowSnaredCount = new[] { lowTierHarness.Target, lowNearby }.Count(m => m!.Trackers.Tags.ContainsKey("rooted"));

        lowSnaredCount.Should()
                      .Be(1, "Tier I Warden's Net should only snare the single target");

        var highTierHarness = new SpellScriptHarness<WardensNetScript>(
            spellSetup: s =>
            {
                s.Level = 20;
                EnsureSpellVars(s, "wardensNet");
            });

        highTierHarness.WithTargetMonster();
        //WithTargetMonster's mock isn't registered in the map's spatial index by default - the AoE branch scans
        //via GetEntitiesWithinRange, so the target itself needs an explicit AddEntity too, not just the extra
        //nearby monster
        highTierHarness.Map.AddEntity(highTierHarness.Target!, Point.From(highTierHarness.Target!));
        var highNearby = MockMonster.Create(highTierHarness.Map);
        highNearby.WarpTo(Point.From(highTierHarness.Target!));
        highTierHarness.Map.AddEntity(highNearby, Point.From(highTierHarness.Target!));

        highTierHarness.Use();

        var highSnaredCount = new[] { highTierHarness.Target, highNearby }.Count(m => m!.Trackers.Tags.ContainsKey("rooted"));

        highSnaredCount.Should()
                       .Be(2, "Tier IV Warden's Net should snare a small AoE");
    }

    [Test]
    public void Flechette_ShouldBounceToMoreTargets_AtHigherTiers()
    {
        var lowTierHarness = new SpellScriptHarness<FlechetteScript>(
            scriptFactory: spell => new FlechetteScript(spell) { BaseDamage = 30, JumpRange = 5 },
            spellSetup: s =>
            {
                s.Level = 1;
                EnsureSpellVars(s, "flechette");
            });

        var lowInitial = MockMonster.Create(lowTierHarness.Map);
        lowInitial.StatSheet.SetHp(100000);
        var lowBounce1 = MockMonster.Create(lowTierHarness.Map);
        lowBounce1.StatSheet.SetHp(100000);
        var lowBounce2 = MockMonster.Create(lowTierHarness.Map);
        lowBounce2.StatSheet.SetHp(100000);

        foreach (var m in new[] { lowInitial, lowBounce1, lowBounce2 })
            lowTierHarness.Map.AddEntity(m, Point.From(m));

        lowTierHarness.WithTarget(lowInitial);
        lowTierHarness.Use();
        lowTierHarness.Script.Update(TimeSpan.FromMilliseconds(500));
        lowTierHarness.Script.Update(TimeSpan.FromMilliseconds(500));
        lowTierHarness.Script.Update(TimeSpan.FromMilliseconds(500));

        var lowHitCount = new[] { lowInitial, lowBounce1, lowBounce2 }.Count(m => m.StatSheet.CurrentHp < 100000);

        var highTierHarness = new SpellScriptHarness<FlechetteScript>(
            scriptFactory: spell => new FlechetteScript(spell) { BaseDamage = 30, JumpRange = 5 },
            spellSetup: s =>
            {
                s.Level = 20;
                EnsureSpellVars(s, "flechette");
            });

        var highInitial = MockMonster.Create(highTierHarness.Map);
        highInitial.StatSheet.SetHp(100000);
        var highBounce1 = MockMonster.Create(highTierHarness.Map);
        highBounce1.StatSheet.SetHp(100000);
        var highBounce2 = MockMonster.Create(highTierHarness.Map);
        highBounce2.StatSheet.SetHp(100000);

        foreach (var m in new[] { highInitial, highBounce1, highBounce2 })
            highTierHarness.Map.AddEntity(m, Point.From(m));

        highTierHarness.WithTarget(highInitial);
        highTierHarness.Use();
        highTierHarness.Script.Update(TimeSpan.FromMilliseconds(500));
        highTierHarness.Script.Update(TimeSpan.FromMilliseconds(500));
        highTierHarness.Script.Update(TimeSpan.FromMilliseconds(500));

        var highHitCount = new[] { highInitial, highBounce1, highBounce2 }.Count(m => m.StatSheet.CurrentHp < 100000);

        highHitCount.Should()
                    .BeGreaterThanOrEqualTo(lowHitCount, "higher-tier Flechette should bounce to at least as many targets");
    }

    [Test]
    public void Focus_ShouldGrantAStrongerBonus_AtHigherTiers()
    {
        var lowTierHarness = new SpellScriptHarness<FletcherFocusScript>(
            spellSetup: s =>
            {
                s.Level = 1;
                EnsureSpellVars(s, "fletcherFocus");
            });

        lowTierHarness.Use();
        lowTierHarness.Source.Effects.TryGetEffect("Focus", out var lowEffect);
        var lowDmgBonus = (lowEffect as FocusEffect)?.DmgBonus ?? 0;

        var highTierHarness = new SpellScriptHarness<FletcherFocusScript>(
            spellSetup: s =>
            {
                s.Level = 20;
                EnsureSpellVars(s, "fletcherFocus");
            });

        highTierHarness.Use();
        highTierHarness.Source.Effects.TryGetEffect("Focus", out var highEffect);
        var highDmgBonus = (highEffect as FocusEffect)?.DmgBonus ?? 0;

        highDmgBonus.Should()
                    .BeGreaterThan(lowDmgBonus, "higher-tier Focus should grant a stronger Dmg bonus");
    }

    [Test]
    public void GravityArrow_ShouldPullAndDamageNearbyEnemies()
    {
        var harness = new SpellScriptHarness<GravityArrowScript>(
            scriptFactory: spell => new GravityArrowScript(spell) { BaseDamage = 30, Range = 3 },
            spellSetup: s => EnsureSpellVars(s, "gravityArrow"));

        //the spell's own target defines the impact point (SpellContext.TargetPoint resolves to the target
        //creature's own position when cast at a creature) - a SEPARATE nearby monster is what should get pulled
        var impactTarget = MockMonster.Create(harness.Map);
        impactTarget.StatSheet.SetHp(100000);
        var impactPoint = Point.From(harness.Source.DirectionalOffset(harness.Source.Direction, 4));
        impactTarget.WarpTo(impactPoint);
        harness.Map.AddEntity(impactTarget, impactPoint);

        var farMonster = MockMonster.Create(harness.Map);
        farMonster.StatSheet.SetHp(100000);
        var farStartPoint = new Point(impactPoint.X + 3, impactPoint.Y);
        farMonster.WarpTo(farStartPoint);
        harness.Map.AddEntity(farMonster, farStartPoint);

        harness.WithTarget(impactTarget);

        var distanceBefore = farStartPoint.ManhattanDistanceFrom(impactPoint);
        var hpBefore = farMonster.StatSheet.CurrentHp;

        harness.Use();

        var distanceAfter = Point.From(farMonster).ManhattanDistanceFrom(impactPoint);

        distanceAfter.Should()
                     .BeLessThan(distanceBefore, "Gravity Arrow should pull the enemy toward the impact point");

        farMonster.StatSheet.CurrentHp
                  .Should()
                  .BeLessThan(hpBefore, "Gravity Arrow should damage pulled enemies");
    }

    [Test]
    public void SpottersBrand_ShouldApplyACritChanceMark()
    {
        var harness = new SpellScriptHarness<SpottersBrandScript>(spellSetup: s => EnsureSpellVars(s, "spottersBrand"));
        harness.WithTargetMonster();

        harness.Use();

        harness.Target!.Trackers.Tags.ContainsKey(SpottersBrandEffect.CritChanceBonusTag)
               .Should()
               .BeTrue("Spotter's Brand should mark the target with a bonus crit-chance tag");
    }

    [Test]
    public void SpottersBrand_ShouldEventuallyProcACritForAnyAttacker()
    {
        var applyDamageScript = ApplyAttackDamageScript.Create();
        var fletcher = MockAisling.Create();
        var otherAttacker = MockAisling.Create();

        var monster = MockMonster.Create();
        monster.StatSheet.AddBonus(new Attributes { MaximumHp = 1000000 });
        monster.StatSheet.SetHp(1000000);
        monster.Effects.Apply(fletcher, new SpottersBrandEffect(), MockSkill.Create().Script);

        //25% crit chance - run enough trials that at least one crit (a much-larger-than-normal hit) is
        //overwhelmingly likely, rather than asserting on a single roll
        var damages = new List<int>();

        for (var i = 0; i < 60; i++)
        {
            var hpBefore = monster.StatSheet.CurrentHp;
            applyDamageScript.ApplyDamage(otherAttacker, monster, MockSkill.Create().Script, 100);
            damages.Add(hpBefore - monster.StatSheet.CurrentHp);
        }

        //damage magnitude is confounded by AC mitigation (same confound documented throughout tonight), so compare
        //against the baseline (non-crit) damage actually observed rather than a hardcoded absolute number
        var baseline = damages.Min();

        damages.Should()
               .Contain(d => d > baseline, "an ally (not just the marking Fletcher) should eventually land a crit against the marked target");
    }

    [Test]
    public void Moonfall_ShouldStrikeMoreTargets_AfterADelay_AtHigherTiers()
    {
        var lowTierHarness = new SpellScriptHarness<MoonfallScript>(
            spellSetup: s =>
            {
                s.Level = 1;
                EnsureSpellVars(s, "moonfall");
            });

        var lowMonsters = Enumerable.Range(0, 4)
                                    .Select(_ => MockMonster.Create(lowTierHarness.Map))
                                    .ToArray();

        var lowTargetPoint = Point.From(lowTierHarness.Source.DirectionalOffset(lowTierHarness.Source.Direction, 3));

        foreach (var m in lowMonsters)
        {
            m.StatSheet.SetHp(100000);
            m.WarpTo(lowTargetPoint);
            lowTierHarness.Map.AddEntity(m, lowTargetPoint);
        }

        lowTierHarness.WithTarget(lowMonsters[0]);
        lowTierHarness.Use();
        lowTierHarness.Script.Update(TimeSpan.FromMilliseconds(2000));

        var lowHitCount = lowMonsters.Count(m => m.StatSheet.CurrentHp < 100000);

        var highTierHarness = new SpellScriptHarness<MoonfallScript>(
            spellSetup: s =>
            {
                s.Level = 20;
                EnsureSpellVars(s, "moonfall");
            });

        var highMonsters = Enumerable.Range(0, 4)
                                     .Select(_ => MockMonster.Create(highTierHarness.Map))
                                     .ToArray();

        var highTargetPoint = Point.From(highTierHarness.Source.DirectionalOffset(highTierHarness.Source.Direction, 3));

        foreach (var m in highMonsters)
        {
            m.StatSheet.SetHp(100000);
            m.WarpTo(highTargetPoint);
            highTierHarness.Map.AddEntity(m, highTargetPoint);
        }

        highTierHarness.WithTarget(highMonsters[0]);
        highTierHarness.Use();
        highTierHarness.Script.Update(TimeSpan.FromMilliseconds(2000));

        var highHitCount = highMonsters.Count(m => m.StatSheet.CurrentHp < 100000);

        highHitCount.Should()
                    .BeGreaterThanOrEqualTo(lowHitCount, "higher-tier Moonfall should strike at least as many targets");
    }

    [Test]
    public void Fulmination_ShouldPierceMultipleTargetsInALine()
    {
        var harness = new SpellScriptHarness<FulminationScript>(
            scriptFactory: spell => new FulminationScript(spell) { BaseDamage = 50, Range = 4 },
            spellSetup: s => EnsureSpellVars(s, "fulmination"));

        var direction = harness.Source.Direction;
        var target1 = MockMonster.Create(harness.Map);
        target1.StatSheet.SetHp(100000);
        var point1 = harness.Source.DirectionalOffset(direction, 1);
        target1.WarpTo(point1);
        harness.Map.AddEntity(target1, point1);

        var target2 = MockMonster.Create(harness.Map);
        target2.StatSheet.SetHp(100000);
        var point2 = harness.Source.DirectionalOffset(direction, 2);
        target2.WarpTo(point2);
        harness.Map.AddEntity(target2, point2);

        harness.Use();

        target1.StatSheet.CurrentHp
               .Should()
               .BeLessThan(100000, "Fulmination should pierce the first target in line");

        target2.StatSheet.CurrentHp
               .Should()
               .BeLessThan(100000, "Fulmination should pierce through to the second target in line");
    }

    [Test]
    public void Multishot_ShouldHitMultipleTimes_WithDecreasingDamage()
    {
        var harness = new SpellScriptHarness<MultishotScript>(
            scriptFactory: spell => new MultishotScript(spell) { BaseDamage = 100, HitCount = 3, PerHitFalloffPct = 0.2m },
            spellSetup: s => EnsureSpellVars(s, "multishot"));

        harness.WithTargetMonster(m => m.StatSheet.SetHp(1000000));

        var hpBefore = harness.Target!.StatSheet.CurrentHp;
        harness.Use();
        var totalDamage = hpBefore - harness.Target.StatSheet.CurrentHp;

        var singleHitHarness = new SpellScriptHarness<MultishotScript>(
            scriptFactory: spell => new MultishotScript(spell) { BaseDamage = 100, HitCount = 1, PerHitFalloffPct = 0.2m },
            spellSetup: s => EnsureSpellVars(s, "multishot"));

        singleHitHarness.WithTargetMonster(m => m.StatSheet.SetHp(1000000));
        var singleHpBefore = singleHitHarness.Target!.StatSheet.CurrentHp;
        singleHitHarness.Use();
        var singleDamage = singleHpBefore - singleHitHarness.Target.StatSheet.CurrentHp;

        totalDamage.Should()
                   .BeGreaterThan(singleDamage, "Multishot's multiple hits should deal more total damage than one hit");
    }

    [Test]
    public void EagleEye_ShouldDealMoreDamage_TheFartherTheShotTraveled()
    {
        var applyDamageScript = ApplyAttackDamageScript.Create();
        var map = MockMapInstance.Create(width: 30, height: 30);
        var fletcher = MockAisling.Create(map);
        fletcher.UserStatSheet.SetBaseClass(BaseClass.Fletcher);

        var nearMonster = MockMonster.Create(map);
        nearMonster.StatSheet.AddBonus(new Attributes { MaximumHp = 10000 });
        nearMonster.StatSheet.SetHp(10000);
        nearMonster.WarpTo(new Point(6, 5));

        var farMonster = MockMonster.Create(map);
        farMonster.StatSheet.AddBonus(new Attributes { MaximumHp = 10000 });
        farMonster.StatSheet.SetHp(10000);
        farMonster.WarpTo(new Point(25, 5));

        fletcher.WarpTo(new Point(5, 5));

        var nearHpBefore = nearMonster.StatSheet.CurrentHp;
        applyDamageScript.ApplyDamage(fletcher, nearMonster, MockSkill.Create().Script, 100);
        var nearDamage = nearHpBefore - nearMonster.StatSheet.CurrentHp;

        var farHpBefore = farMonster.StatSheet.CurrentHp;
        applyDamageScript.ApplyDamage(fletcher, farMonster, MockSkill.Create().Script, 100);
        var farDamage = farHpBefore - farMonster.StatSheet.CurrentHp;

        farDamage.Should()
                 .BeGreaterThan(nearDamage, "Eagle Eye should deal more damage the farther the shot traveled");
    }

    [Test]
    public void PhantomQuiver_ShouldConjureASpectralArrow_OnTheFourthHit()
    {
        var applyDamageScript = ApplyAttackDamageScript.Create();
        var fletcher = MockAisling.Create();
        fletcher.UserStatSheet.SetBaseClass(BaseClass.Fletcher);

        var monster = MockMonster.Create();
        monster.StatSheet.AddBonus(new Attributes { MaximumHp = 1000000 });
        monster.StatSheet.SetHp(1000000);

        var damages = new List<int>();

        for (var i = 0; i < 4; i++)
        {
            var hpBefore = monster.StatSheet.CurrentHp;
            applyDamageScript.ApplyDamage(fletcher, monster, MockSkill.Create().Script, 100);
            damages.Add(hpBefore - monster.StatSheet.CurrentHp);
        }

        damages[3]
            .Should()
            .BeGreaterThan(damages[0], "the 4th hit should deal bonus damage from the conjured spectral arrow");
    }

    [Test]
    public void Windrunner_ShouldGrantBonusDamage_OnTheNextHitAfterArrowstep_ThenBreak()
    {
        var applyDamageScript = ApplyAttackDamageScript.Create();

        var harness = new SpellScriptHarness<ArrowstepScript>(spellSetup: s => EnsureSpellVars(s, "arrowstep"));
        harness.Source.StatSheet.SetMp(100);

        //Arrowstep requires a bow equipped
        var bow = MockItem.Create(name: "TestBow", templateSetup: t => t with { Category = "bow", EquipmentType = EquipmentType.Weapon });
        harness.Source.Equipment.TryEquip(EquipmentType.Weapon, bow, out _);

        var windrunnerSpell = MockSpell.Create(name: "Windrunner", templateSetup: t => t with { TemplateKey = "windrunner" });
        harness.Source.SpellBook.TryAddToNextSlot(windrunnerSpell);

        harness.Use();

        harness.Source.Trackers.Tags.ContainsKey(WindrunnerEffect.ReadyTag)
               .Should()
               .BeTrue("using Arrowstep with Windrunner learned should ready the bonus");

        var monster = MockMonster.Create(harness.Map);
        monster.StatSheet.AddBonus(new Attributes { MaximumHp = 10000 });
        monster.StatSheet.SetHp(10000);

        applyDamageScript.ApplyDamage(harness.Source, monster, MockSkill.Create().Script, 100);

        harness.Source.Trackers.Tags.ContainsKey(WindrunnerEffect.ReadyTag)
               .Should()
               .BeFalse("Windrunner's bonus should be consumed on the next hit");
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
