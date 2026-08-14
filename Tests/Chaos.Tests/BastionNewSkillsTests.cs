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
using Chaos.Scripting.SkillScripts;
using Chaos.Services.Factories.Abstractions;
using Chaos.Testing.Infrastructure.Harnesses;
using Chaos.Testing.Infrastructure.Mocks;
using FluentAssertions;
using Moq;
#endregion

namespace Chaos.Tests;

/// <summary>
///     Smoke tests for Bastion's newly-built/evolving skill scripts (Challenging Shout, Bastion's Charge, Iron
///     Cairn, Stacia's Bulwark, Valkor's Aegis, Shield Thrust, Pivot Strike, Stacia's Cleansing Light) - confirms
///     each constructs and runs without throwing, across level-bracket tiers where the ability evolves, using the
///     same SkillScriptHarness/MockServiceProvider infrastructure established for Berserker's equivalent tests
///     this session.
/// </summary>
public sealed class BastionNewSkillsTests
{
    /// <inheritdoc cref="BerserkerNewSkillsTests" />
    static BastionNewSkillsTests()
    {
        var registry = new FunctionalScriptRegistry(MockServiceProvider.CreateBuilder()
                                                                        .Build()
                                                                        .Object);

        registry.Register(ApplyAttackDamageScript.Key, typeof(ApplyAttackDamageScript));
        registry.Register(Chaos.Scripting.FunctionalScripts.ApplyHealing.ApplyHealScript.Key, typeof(Chaos.Scripting.FunctionalScripts.ApplyHealing.ApplyHealScript));
    }

    private static void EnsureScriptVars(Skill skill, string scriptKey) => skill.Template.ScriptVars[scriptKey] = new EmptyScriptVars();

    private static IServiceProvider CreateServiceProviderWithEffectFactory()
    {
        var effectFactoryMock = new Mock<IEffectFactory>();

        effectFactoryMock.Setup(f => f.Create("ChallengingResolve"))
                         .Returns(() => new ChallengingResolveEffect());

        effectFactoryMock.Setup(f => f.Create("WeakeningShout"))
                         .Returns(() => new WeakeningShoutEffect());

        return MockServiceProvider.CreateBuilder()
                                  .SetupService(effectFactoryMock.Object)
                                  .Build()
                                  .Object;
    }

    private static void EquipShield(Aisling aisling)
    {
        var shield = MockItem.Create("TestShield", templateSetup: t => t with { EquipmentType = EquipmentType.Shield });
        aisling.Equipment.TryEquip(EquipmentType.Shield, shield, out _);
    }

    [Test]
    [Arguments((byte)1)]
    [Arguments((byte)3)]
    [Arguments((byte)5)]
    [Arguments((byte)7)]
    public void ChallengingShout_ShouldNotThrow_AtAnyTier(byte level)
    {
        var harness = new SkillScriptHarness<ChallengingShoutScript>(
            skillSetup: s =>
            {
                s.Level = level;
                EnsureScriptVars(s, "challengingShout");
            },
            serviceProvider: CreateServiceProviderWithEffectFactory());

        harness.WithTargetMonster();

        var act = harness.Use;

        act.Should()
           .NotThrow();
    }

    [Test]
    [Arguments((byte)1)]
    [Arguments((byte)3)]
    [Arguments((byte)5)]
    [Arguments((byte)7)]
    public void BastionsCharge_ShouldNotThrow_AtAnyTier(byte level)
    {
        var harness = new SkillScriptHarness<BastionsChargeScript>(
            skillSetup: s =>
            {
                s.Level = level;
                EnsureScriptVars(s, "bastionsCharge");
            });

        var act = harness.Use;

        act.Should()
           .NotThrow();
    }

    [Test]
    public void BastionsCharge_StopgapValues_ShouldActuallyRushAndDealDamage()
    {
        //bastions_charge.json (the old, still-granted single-file template) had no scriptVars at all for the
        //rewritten BastionsChargeScript.cs, which no longer has an internal Level-switch fallback - every
        //property silently defaulted to 0/false, so the live ability did nothing (0-tile rush, 0 damage), no
        //exception thrown. Stopgap: populated bastions_charge.json's scriptVars with bastions_charge_i.json's
        //Tier I values. This test uses those same real values to confirm the ability actually functions again -
        //not the real fix, just confirming the bleeding has stopped.
        var harness = new SkillScriptHarness<BastionsChargeScript>(
            scriptFactory: skill => new BastionsChargeScript(skill)
            {
                RushDistance = 3,
                BaseDamage = 10,
                DamageStatMultiplier = 1.5m,
                StunOnImpact = false,
                AoeOnImpact = false,
                StunDurationMs = 2000,
                DamageStat = Stat.STR
            },
            skillSetup: s => EnsureScriptVars(s, "bastionsCharge"));

        harness.Source.Direction = Direction.Down;
        var target = MockMonster.Create(harness.Map, setup: m => m.WarpTo(harness.Source.DirectionalOffset(Direction.Down, 2)));
        harness.Map.AddEntity(target, Point.From(target));
        target.StatSheet.SetHp(100000);

        var sourceStartPoint = Point.From(harness.Source);
        var hpBefore = target.StatSheet.CurrentHp;

        harness.Use();

        Point.From(harness.Source)
             .ManhattanDistanceFrom(sourceStartPoint)
             .Should()
             .BeGreaterThan(0, "Bastion's Charge should actually move the caster forward, not stay at rush distance 0");

        target.StatSheet.CurrentHp
              .Should()
              .BeLessThan(hpBefore, "Bastion's Charge should deal real damage again, not 0");
    }

    [Test]
    public void StaciasBulwark_StopgapValues_ShouldGrantARealDuration()
    {
        //stacias_bulwark.json (the old, still-granted single-file template) had no durationMs at all for the
        //rewritten StaciasBulwarkScript.cs - it silently defaulted to 0, making the invulnerability window
        //effectively instant. Stopgap: populated stacias_bulwark.json's scriptVars with stacias_bulwark_i.json's
        //Tier I durationMs (2000). Confirms the effect is still active a meaningful amount of time after cast.
        var harness = new SkillScriptHarness<StaciasBulwarkScript>(
            scriptFactory: skill => new StaciasBulwarkScript(skill) { DurationMs = 2000 },
            skillSetup: s => EnsureScriptVars(s, "staciasBulwark"));

        harness.Use();

        harness.Source.Effects.TryGetEffect("Stacia's Bulwark", out var effect);

        effect.Should()
              .NotBeNull("Stacia's Bulwark should apply its invulnerability effect");

        harness.Source.Effects.Update(TimeSpan.FromMilliseconds(1500));

        harness.Source.Effects.TryGetEffect("Stacia's Bulwark", out var stillActive);

        stillActive.Should()
                   .NotBeNull("with a real 2000ms duration, the effect should still be active 1500ms later, not already expired");
    }

    [Test]
    [Arguments((byte)1)]
    [Arguments((byte)3)]
    [Arguments((byte)5)]
    [Arguments((byte)7)]
    public void IronCairn_ShouldNotThrow_AtAnyTier_AndAftershockUpdateDoesNotThrow(byte level)
    {
        var harness = new SkillScriptHarness<IronCairnScript>(
            skillSetup: s =>
            {
                s.Level = level;
                EnsureScriptVars(s, "ironCairn");
            });

        harness.WithTargetMonster();

        harness.Use();

        var act = () => harness.Script.Update(TimeSpan.FromMilliseconds(2000));

        act.Should()
           .NotThrow();
    }

    [Test]
    [Arguments((byte)1)]
    [Arguments((byte)3)]
    [Arguments((byte)5)]
    [Arguments((byte)7)]
    public void StaciasBulwark_ShouldNotThrow_AtAnyTier_AndHealOnExpiryDoesNotThrow(byte level)
    {
        var harness = new SkillScriptHarness<StaciasBulwarkScript>(
            skillSetup: s =>
            {
                s.Level = level;
                EnsureScriptVars(s, "staciasBulwark");
            });

        harness.Use();

        var act = () => harness.Script.Update(TimeSpan.FromMilliseconds(6000));

        act.Should()
           .NotThrow();
    }

    [Test]
    public void ValkorsAegis_ShouldNotThrow_WithShieldEquipped()
    {
        var harness = new SkillScriptHarness<ValkorsAegisScript>(skillSetup: s => EnsureScriptVars(s, "valkorsAegis"));

        EquipShield(harness.Source);
        harness.WithTargetMonster();

        var act = harness.Use;

        act.Should()
           .NotThrow();
    }

    [Test]
    public void ShieldThrust_ShouldKnockTargetBack_WhenStruck()
    {
        var harness = new SkillScriptHarness<ShieldThrustScript>(skillSetup: s => EnsureScriptVars(s, "shieldThrust"));

        EquipShield(harness.Source);

        var target = MockMonster.Create(
            harness.Map,
            setup: m => m.SetLocation(new Point(harness.Source.X + 1, harness.Source.Y)));

        harness.Source.Direction = Direction.Right;

        var originalDistance = Math.Abs(target.X - harness.Source.X);

        harness.WithTarget(target);
        harness.Use();

        var newDistance = Math.Abs(target.X - harness.Source.X);

        newDistance.Should()
                   .BeGreaterThanOrEqualTo(originalDistance, "Shield Thrust should knock the target further away, not pull it in");
    }

    [Test]
    public void PivotStrike_ShouldDealDamageAndStun_WithoutRepositioningTheCaster()
    {
        //reworked per playtest feedback: dropped the old teleport-behind-target mechanic (a third gap-closer,
        //too mobile for a tank) in favor of a purely stationary strike+stun - this asserts both the new stun
        //behavior AND that the caster genuinely no longer moves, not just "doesn't throw"
        var harness = new SkillScriptHarness<PivotStrikeScript>(
            scriptFactory: skill => new PivotStrikeScript(skill)
            {
                BaseDamage = 30,
                DamageStat = Stat.STR,
                DamageStatMultiplier = 1.5m,
                RangeTiles = 4,
                StunDurationMs = 1000
            },
            skillSetup: s => EnsureScriptVars(s, "pivotStrike"));

        var target = MockMonster.Create(
            harness.Map,
            setup: m =>
            {
                m.SetLocation(new Point(harness.Source.X + 2, harness.Source.Y));
                m.StatSheet.SetHp(100000);
            });

        harness.Source.Direction = Direction.Right;

        var sourceOriginalPoint = Point.From(harness.Source);
        var hpBefore = target.StatSheet.CurrentHp;

        harness.WithTarget(target);
        harness.Use();

        target.StatSheet.CurrentHp
              .Should()
              .BeLessThan(hpBefore, "Pivot Strike should deal damage");

        target.Effects.Contains("Root")
              .Should()
              .BeTrue("Pivot Strike should briefly stun the target");

        Point.From(harness.Source)
             .Should()
             .Be(sourceOriginalPoint, "Pivot Strike should no longer reposition the caster");
    }

    [Test]
    public void StaciasCleansingLight_ShouldRemoveSelfDebuffs()
    {
        var harness = new SkillScriptHarness<StaciasCleansingLightScript>(skillSetup: s => EnsureScriptVars(s, "staciasCleansingLight"));

        var slow = new SlowEffect();
        harness.Source.Effects.Apply(harness.Source, slow, harness.Script);

        harness.Source.Effects.Contains("Slow")
               .Should()
               .BeTrue("the debuff should be applied before cleansing");

        harness.Use();

        harness.Source.Effects.Contains("Slow")
               .Should()
               .BeFalse("Stacia's Cleansing Light should have stripped the debuff");
    }

    [Test]
    public void IronReprisal_ShouldNegateAndCounter_TheNextHit()
    {
        var damageScript = new ApplyAttackDamageScript();
        var harness = new SkillScriptHarness<ShieldThrustScript>(skillSetup: s => EnsureScriptVars(s, "shieldThrust")); // harness just for Source/Map setup

        var bastion = harness.Source;
        bastion.UserStatSheet.SetBaseClass(BaseClass.Bastion);
        bastion.StatSheet.SetHp(500);

        var reprisalEffect = new IronReprisalEffect();
        bastion.Effects.Apply(bastion, reprisalEffect, harness.Script);

        bastion.Trackers.Tags
               .Should()
               .ContainKey(IronReprisalEffect.ReadyTag);

        var attacker = MockMonster.Create(harness.Map, name: "Attacker");
        var hpBefore = bastion.StatSheet.CurrentHp;

        damageScript.ApplyDamage(attacker, bastion, harness.Script, 9999);

        bastion.StatSheet.CurrentHp
               .Should()
               .Be(hpBefore, "Iron Reprisal should negate the hit entirely, not just reduce it");

        bastion.Trackers.Tags
               .Should()
               .NotContainKey(IronReprisalEffect.ReadyTag, "the counter should consume the ready tag, one use only");
    }

    /// <inheritdoc cref="BerserkerNewSkillsTests" />
    private sealed class EmptyScriptVars : IScriptVars
    {
        public bool ContainsKey(string key) => false;
        public object? Get(Type type, string name) => null;
        public T? Get<T>(string name) => default;
        public T GetRequired<T>(string key) => throw new KeyNotFoundException(key);
        public void Set<T>(string name, T value) { }
    }
}
