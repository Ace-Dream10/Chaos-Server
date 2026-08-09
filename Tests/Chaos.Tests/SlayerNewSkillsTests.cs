#region
using Chaos.Collections;
using Chaos.Common.Abstractions;
using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;
using Chaos.Models.Panel;
using Chaos.Scripting.FunctionalScripts;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts;
using Chaos.Testing.Infrastructure.Harnesses;
using Chaos.Testing.Infrastructure.Mocks;
using FluentAssertions;
#endregion

namespace Chaos.Tests;

/// <summary>
///     Confirms Cruel Thrust correctly drives <see cref="SeveranceTargetSync" /> - the MP-bar-as-stack-visual
///     feedback restored after severance_strike/deep_cut (its only previous callers) were retired as pre-redesign
///     orphans. Uses the same SkillScriptHarness infrastructure <c>BerserkerNewSkillsTests</c> established.
/// </summary>
public sealed class SlayerNewSkillsTests
{
    /// <summary>
    ///     Same test-only static registration <c>BerserkerNewSkillsTests</c> established - CruelThrustScript calls
    ///     the static <c>ApplyAttackDamageScript.Create()</c>, which resolves through
    ///     <see cref="FunctionalScriptRegistry.Instance" />, a process-wide singleton normally populated by real app
    ///     startup's reflection-based registration (which never runs in this test process).
    /// </summary>
    static SlayerNewSkillsTests()
    {
        var registry = new FunctionalScriptRegistry(MockServiceProvider.CreateBuilder()
                                                                        .Build()
                                                                        .Object);

        registry.Register(ApplyAttackDamageScript.Key, typeof(ApplyAttackDamageScript));
    }

    private static void EnsureScriptVars(Skill skill, string scriptKey) => skill.Template.ScriptVars[scriptKey] = new EmptyScriptVars();

    [Test]
    public void CruelThrust_ShouldSyncMpToSeveranceStacks_OnHit()
    {
        var harness = new SkillScriptHarness<CruelThrustScript>(skillSetup: s =>
        {
            s.Level = 1;
            EnsureScriptVars(s, "cruelThrust");
        });

        //CruelThrustScript looks for a target exactly Range (1) tiles ahead in the caster's facing direction
        harness.Source.Direction = Direction.Down;

        harness.WithTargetMonster(m => m.SetLocation(new Point(harness.Source.X, harness.Source.Y + 1)));

        harness.Source.StatSheet.CurrentMp
               .Should()
               .Be(0);

        harness.Use();

        //Tier I (level <= 2) applies 1 Severance stack; SeveranceTargetSync syncs MP to stacks * 20 per stack
        harness.Source.StatSheet.CurrentMp
               .Should()
               .Be(20);
    }

    [Test]
    public void CruelThrust_ShouldResetMp_WhenSwitchingSeveranceTargets()
    {
        var harness = new SkillScriptHarness<CruelThrustScript>(skillSetup: s =>
        {
            s.Level = 1;
            EnsureScriptVars(s, "cruelThrust");
        });

        harness.Source.Direction = Direction.Down;

        var firstTargetPoint = new Point(harness.Source.X, harness.Source.Y + 1);
        harness.WithTargetMonster(m => m.SetLocation(firstTargetPoint));
        harness.Use();

        harness.Source.StatSheet.CurrentMp
               .Should()
               .Be(20);

        //A second, different monster at the same point (a fresh Severance target) should reset MP to 0 before the
        //new stack is applied and synced, not just keep accumulating
        harness.WithTargetMonster(m => m.SetLocation(firstTargetPoint));
        harness.Use();

        harness.Source.StatSheet.CurrentMp
               .Should()
               .Be(20);
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
