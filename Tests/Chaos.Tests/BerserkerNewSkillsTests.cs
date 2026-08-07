#region
using Chaos.Collections;
using Chaos.Common.Abstractions;
using Chaos.Models.Panel;
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
///     Smoke tests for Berserker's newly-built evolving skill scripts (Massacre, Berserker Gate, Titanic Fury,
///     Seismic Leap) - confirms each constructs and runs without throwing across the level-bracket tiers, using
///     the SkillScriptHarness/MockServiceProvider infrastructure already established this session for the housing
///     work (previously unused for skill scripts specifically, same as it was for item scripts before).
/// </summary>
public sealed class BerserkerNewSkillsTests
{
    /// <summary>
    ///     Massacre/SeismicLeap call the static <c>ApplyAttackDamageScript.Create()</c> (same pattern
    ///     <c>CycloneScript</c> already uses) which resolves through <see cref="FunctionalScriptRegistry.Instance" />
    ///     - a process-wide static singleton normally populated by real app startup's reflection-based script
    ///     registration, which never runs in this test process. Registering it here once is a test-only fix (idempotent
    ///     - <see cref="FunctionalScriptRegistry.Register" /> uses TryAdd) and doesn't touch production code; this
    ///     wasn't previously needed because no skill using ApplyAttackDamageScript.Create() had a test at all.
    /// </summary>
    static BerserkerNewSkillsTests()
    {
        var registry = new FunctionalScriptRegistry(MockServiceProvider.CreateBuilder()
                                                                        .Build()
                                                                        .Object);

        registry.Register(ApplyAttackDamageScript.Key, typeof(ApplyAttackDamageScript));
    }

    /// <summary>
    ///     Each of these scripts is a ConfigurableSkillScriptBase (per builder.md's #1 pitfall), so its template
    ///     needs a matching (even if empty) scriptVars entry under its own script key or construction throws.
    ///     MockSkill.Create's own default template ships an empty ScriptVars dictionary, so this has to be injected
    ///     via skillSetup, which runs before the harness constructs the script.
    /// </summary>
    private static void EnsureScriptVars(Skill skill, string scriptKey) => skill.Template.ScriptVars[scriptKey] = new EmptyScriptVars();

    private static IServiceProvider CreateServiceProviderWithEffectFactory()
    {
        var effectFactoryMock = new Mock<IEffectFactory>();

        effectFactoryMock.Setup(f => f.Create("BerserkerGate"))
                         .Returns(() => new BerserkerGateEffect());

        effectFactoryMock.Setup(f => f.Create("TitanicFury"))
                         .Returns(() => new TitanicFuryEffect());

        effectFactoryMock.Setup(f => f.Create("Slow"))
                         .Returns(() => new SlowEffect());

        effectFactoryMock.Setup(f => f.Create("Root"))
                         .Returns(() => new RootEffect());

        return MockServiceProvider.CreateBuilder()
                                  .SetupService(effectFactoryMock.Object)
                                  .Build()
                                  .Object;
    }

    [Test]
    [Arguments((byte)1)]
    [Arguments((byte)3)]
    [Arguments((byte)5)]
    [Arguments((byte)7)]
    public void Massacre_ShouldNotThrow_AtAnyTier(byte level)
    {
        var harness = new SkillScriptHarness<MassacreScript>(
            skillSetup: s =>
            {
                s.Level = level;
                EnsureScriptVars(s, "Massacre");
            });

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
    public void BerserkerGate_ShouldNotThrow_AtAnyTier(byte level)
    {
        var serviceProvider = CreateServiceProviderWithEffectFactory();

        var harness = new SkillScriptHarness<BerserkerGateScript>(
            skillSetup: s =>
            {
                s.Level = level;
                EnsureScriptVars(s, "BerserkerGate");
            },
            serviceProvider: serviceProvider);

        var act = harness.Use;

        act.Should()
           .NotThrow();
    }

    [Test]
    public void BerserkerGate_ShouldGrantDamageBonus_ToCaster()
    {
        var serviceProvider = CreateServiceProviderWithEffectFactory();

        var harness = new SkillScriptHarness<BerserkerGateScript>(
            skillSetup: s =>
            {
                s.Level = 1;
                EnsureScriptVars(s, "BerserkerGate");
            },
            serviceProvider: serviceProvider);

        var before = harness.Source.StatSheet.EffectiveDmg;

        harness.Use();

        harness.Source.StatSheet.EffectiveDmg
               .Should()
               .BeGreaterThan(before);
    }

    [Test]
    [Arguments((byte)1)]
    [Arguments((byte)3)]
    [Arguments((byte)5)]
    [Arguments((byte)7)]
    public void TitanicFury_ShouldNotThrow_AtAnyTier_WithEnoughRage(byte level)
    {
        var serviceProvider = CreateServiceProviderWithEffectFactory();

        var harness = new SkillScriptHarness<TitanicFuryScript>(
            skillSetup: s =>
            {
                s.Level = level;
                EnsureScriptVars(s, "TitanicFury");
            },
            serviceProvider: serviceProvider);

        harness.Source.StatSheet.SetMp(100);

        var act = harness.Use;

        act.Should()
           .NotThrow();
    }

    [Test]
    public void TitanicFury_ShouldSendMessage_WhenNotEnoughRage()
    {
        var serviceProvider = CreateServiceProviderWithEffectFactory();

        var harness = new SkillScriptHarness<TitanicFuryScript>(
            skillSetup: s =>
            {
                s.Level = 1;
                EnsureScriptVars(s, "TitanicFury");
            },
            serviceProvider: serviceProvider);

        harness.Source.StatSheet.SetMp(0);

        harness.Use();

        harness.SourceClient.Verify(
            c => c.SendServerMessage(It.IsAny<Chaos.DarkAges.Definitions.ServerMessageType>(), It.Is<string>(msg => msg.Contains("rage"))),
            Times.Once);
    }

    [Test]
    [Arguments((byte)1)]
    [Arguments((byte)3)]
    [Arguments((byte)5)]
    [Arguments((byte)7)]
    public void SeismicLeap_ShouldNotThrow_AtAnyTier(byte level)
    {
        var serviceProvider = CreateServiceProviderWithEffectFactory();

        var harness = new SkillScriptHarness<SeismicLeapScript>(
            skillSetup: s =>
            {
                s.Level = level;
                EnsureScriptVars(s, "SeismicLeap");
            },
            serviceProvider: serviceProvider);

        harness.WithTargetMonster();

        var act = harness.Use;

        act.Should()
           .NotThrow();
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
