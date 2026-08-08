#region
using Chaos.Common.Abstractions;
using Chaos.DarkAges.Definitions;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.AislingScripts;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts;
using Chaos.Testing.Infrastructure.Harnesses;
using Chaos.Testing.Infrastructure.Mocks;
using FluentAssertions;
#endregion

namespace Chaos.Tests;

/// <summary>
///     Functional tests for Bastion's 3 passives, simulating real gameplay (an actual attack through
///     ApplyAttackDamageScript, not just constructing scripts and asserting on internals) per the same discipline
///     established for <see cref="BerserkerPassivesTests" />. Bastion's Retribution and Hold the Line are
///     stateless (computed directly in the damage pipeline, no AislingScript involved - see their doc comments in
///     <see cref="ApplyAttackDamageScript" /> for why), so those are tested purely through the damage script;
///     Guardian's Resolve does need the AislingScript harness since it's the one that actually ticks.
/// </summary>
public sealed class BastionPassivesTests
{
    private static readonly ApplyAttackDamageScript DamageScript = new();

    /// <inheritdoc cref="BastionNewSkillsTests" />
    private static void EnsureScriptVars(Skill skill, string scriptKey) => skill.Template.ScriptVars[scriptKey] = new EmptyScriptVars();

    private sealed class EmptyScriptVars : IScriptVars
    {
        public bool ContainsKey(string key) => false;
        public object? Get(Type type, string name) => null;
        public T? Get<T>(string name) => default;
        public T GetRequired<T>(string key) => throw new KeyNotFoundException(key);
        public void Set<T>(string name, T value) { }
    }

    [Test]
    public void BastionsRetribution_ShouldReflectDamage_ScaledByAc_ForBastion()
    {
        var harness = new SkillScriptHarness<ShieldThrustScript>(skillSetup: s => EnsureScriptVars(s, "shieldThrust")); //just for Source/Map
        var bastion = harness.Source;
        bastion.UserStatSheet.SetBaseClass(BaseClass.Bastion);
        bastion.StatSheet.SetHp(1000);
        //-30, not something more extreme: EffectiveAc also feeds the incoming-damage AC mitigation formula
        //(1 + defenderAc/100), and a bonus at or beyond -100 would zero the incoming hit entirely before
        //Retribution's own reflect code ever runs, given the -100 test-environment AC floor
        bastion.StatSheet.AddBonus(new Attributes { Ac = -30 }); //stronger defense = harder counter

        var attacker = MockMonster.Create(harness.Map, name: "Attacker", setup: m => m.StatSheet.SetHp(500));
        var attackerHpBefore = attacker.StatSheet.CurrentHp;

        DamageScript.ApplyDamage(attacker, bastion, harness.Script, 200);

        attacker.StatSheet.CurrentHp
                .Should()
                .BeLessThan(attackerHpBefore, "Bastion's Retribution should reflect some damage back at the attacker");
    }

    [Test]
    public void BastionsRetribution_ShouldNotReflect_ForNonBastion()
    {
        var harness = new SkillScriptHarness<ShieldThrustScript>(skillSetup: s => EnsureScriptVars(s, "shieldThrust"));
        var nonBastion = harness.Source;
        nonBastion.UserStatSheet.SetBaseClass(BaseClass.Sorcerer);
        nonBastion.StatSheet.SetHp(1000);
        nonBastion.StatSheet.AddBonus(new Attributes { Ac = -100 });

        var attacker = MockMonster.Create(harness.Map, name: "Attacker", setup: m => m.StatSheet.SetHp(500));
        var attackerHpBefore = attacker.StatSheet.CurrentHp;

        DamageScript.ApplyDamage(attacker, nonBastion, harness.Script, 200);

        attacker.StatSheet.CurrentHp
                .Should()
                .Be(attackerHpBefore, "Retribution is Bastion-only");
    }

    [Test]
    public void HoldTheLine_ShouldRedirectBonusAggro_ToNearbyBastion_WhenAllyIsHit()
    {
        var harness = new SkillScriptHarness<ShieldThrustScript>(skillSetup: s => EnsureScriptVars(s, "shieldThrust"));
        var map = harness.Map;

        var ally = MockAisling.Create(map, setup: a => a.StatSheet.SetHp(1000));
        var bastion = MockAisling.Create(map, setup: a => a.StatSheet.SetHp(1000));
        bastion.UserStatSheet.SetBaseClass(BaseClass.Bastion);

        //MockAisling.Create doesn't register the aisling on the map's spatial index (unlike MockMonster.Create) -
        //GetEntitiesWithinRange needs it explicitly added to find it
        map.AddAislingDirect(ally, ally);
        map.AddAislingDirect(bastion, bastion);

        var attacker = MockMonster.Create(map, name: "Attacker", setup: m => m.StatSheet.SetHp(500));

        DamageScript.ApplyDamage(attacker, ally, harness.Script, 50);

        attacker.AggroList.GetAggro(bastion)
                .Should()
                .BeGreaterThan(0, "a nearby Bastion should have gained bonus threat when their ally was hit");
    }

    [Test]
    public void StaciasBulwark_ShouldNegateDamageEntirely_WhileActive()
    {
        var harness = new SkillScriptHarness<ShieldThrustScript>(skillSetup: s => EnsureScriptVars(s, "shieldThrust"));
        var bastion = harness.Source;
        bastion.UserStatSheet.SetBaseClass(BaseClass.Bastion);
        bastion.StatSheet.SetHp(1000);

        var bulwarkEffect = new StaciasBulwarkEffect();
        bastion.Effects.Apply(bastion, bulwarkEffect, harness.Script);

        var attacker = MockMonster.Create(harness.Map, name: "Attacker");
        var hpBefore = bastion.StatSheet.CurrentHp;

        DamageScript.ApplyDamage(attacker, bastion, harness.Script, 9999);

        bastion.StatSheet.CurrentHp
               .Should()
               .Be(hpBefore, "Stacia's Bulwark should negate the hit entirely - true invulnerability, not mitigation");
    }

    [Test]
    public void GuardiansResolve_ShouldGrantAcBonus_ScaledToNearbyEnemyCount()
    {
        var harness = new AislingScriptHarness<GuardiansResolveScript>();
        var bastion = harness.Source;
        bastion.UserStatSheet.SetBaseClass(BaseClass.Bastion);

        var acBefore = bastion.StatSheet.EffectiveAc;

        MockMonster.Create(harness.Map, setup: m => m.SetLocation(new Point(bastion.X + 1, bastion.Y)));
        MockMonster.Create(harness.Map, setup: m => m.SetLocation(new Point(bastion.X - 1, bastion.Y)));

        harness.Update(TimeSpan.FromMilliseconds(600)); //past the 500ms scan interval

        bastion.StatSheet.EffectiveAc
               .Should()
               .BeLessThan(acBefore, "nearby enemies should grant a defensive AC bonus (lower AC = stronger defense)");
    }

    [Test]
    public void GuardiansResolve_ShouldNotApply_ForNonBastion()
    {
        var harness = new AislingScriptHarness<GuardiansResolveScript>();
        var nonBastion = harness.Source;
        nonBastion.UserStatSheet.SetBaseClass(BaseClass.Sorcerer);

        var acBefore = nonBastion.StatSheet.EffectiveAc;

        MockMonster.Create(harness.Map, setup: m => m.SetLocation(new Point(nonBastion.X + 1, nonBastion.Y)));

        harness.Update(TimeSpan.FromMilliseconds(600));

        nonBastion.StatSheet.EffectiveAc
                  .Should()
                  .Be(acBefore, "Guardian's Resolve is Bastion-only");
    }
}
