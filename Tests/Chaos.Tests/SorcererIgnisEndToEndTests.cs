#region
using Chaos.Common.Abstractions;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Models.Menu;
using Chaos.Models.Templates;
using Chaos.Models.World;
using Chaos.Scripting.Abstractions;
using Chaos.Scripting.AislingScripts;
using Chaos.Scripting.DialogScripts;
using Chaos.Scripting.DialogScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Services.Factories;
using Chaos.Services.Factories.Abstractions;
using Chaos.Testing.Infrastructure.Harnesses;
using Chaos.Testing.Infrastructure.Mocks;
using Chaos.Utilities;
using FluentAssertions;
using Moq;
#endregion

namespace Chaos.Tests;

/// <summary>
///     Full end-to-end walk-through of Sorcerer's complete progression for one path (pure Ignis, Fire chosen
///     twice) - the first real test of Phase 1's infrastructure against Phase 2's real content. Uses the REAL
///     <see cref="SorcererModuleProvider" /> (not a fake, unlike <see cref="SorcererProgressionScriptTests" />) so
///     this proves the actual wiring between real templateKeys and the granting pipeline, not just the mechanism
///     shape. <see cref="ISpellFactory" /> is still mocked (returns <see cref="MockSpell.Create" /> instances) since
///     a real one needs the full config-loading pipeline running, which a unit test process doesn't have - same
///     reasoning <c>BerserkerNewSkillsTests</c>/<c>SlayerNewSkillsTests</c> already established.
/// </summary>
/// <remarks>
///     The dialog scripts (<see cref="SorcererChooseFirstElementScript" />/
///     <see cref="SorcererChooseSecondElementScript" />) are exercised directly here via their real constructors
///     (no DialogScriptHarness exists in this codebase yet, unlike Skill/Spell/Aisling/Monster/Item scripts) -
///     this still runs their exact production OnDisplaying logic, just without a network-driven Dialog object
///     behind it.
/// </remarks>
public sealed class SorcererIgnisEndToEndTests
{
    static SorcererIgnisEndToEndTests()
    {
        var registry = new FunctionalScriptRegistry(MockServiceProvider.CreateBuilder()
                                                                        .Build()
                                                                        .Object);

        registry.Register(ApplyAttackDamageScript.Key, typeof(ApplyAttackDamageScript));
    }

    private static ISpellFactory CreateSpellFactory()
    {
        var mock = new Mock<ISpellFactory>();

        mock.Setup(f => f.Create(It.IsAny<string>(), null))
            .Returns((string templateKey, ICollection<string>? _) => MockSpell.Create(templateKey));

        return mock.Object;
    }

    /// <summary>
    ///     Builds a real Dialog (not the trivial 4-arg stub constructor) with a real DialogTemplate carrying an
    ///     empty ScriptVars entry for the given scriptKey - ConfigurableDialogScriptBase (per builder.md's own
    ///     #1 pitfall, same as ConfigurableSkillScriptBase) requires a matching ScriptVars entry to exist or
    ///     construction throws, mirroring BerserkerNewSkillsTests/SlayerNewSkillsTests's EnsureScriptVars pattern
    ///     for skill scripts.
    /// </summary>
    private static Dialog CreateStubDialog(Aisling dialogSource, string scriptKey)
    {
        var template = new DialogTemplate
        {
            Contextual = false,
            NextDialogKey = null,
            Options = [],
            PrevDialogKey = null,
            ScriptKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { scriptKey },
            ScriptVars = new Dictionary<string, IScriptVars>(StringComparer.OrdinalIgnoreCase) { [scriptKey] = new EmptyScriptVars() },
            TemplateKey = "stub",
            Text = "stub",
            TextBoxLength = null,
            TextBoxPrompt = null,
            Type = ChaosDialogType.Normal
        };

        var scriptProviderMock = new Mock<IScriptProvider>();

        scriptProviderMock.Setup(sp => sp.CreateScript<IDialogScript, Dialog>(It.IsAny<ICollection<string>>(), It.IsAny<Dialog>()))
                          .Returns(() => new Mock<IDialogScript>().Object);

        return new Dialog(template, dialogSource, scriptProviderMock.Object, new Mock<IDialogFactory>().Object);
    }

    private sealed class EmptyScriptVars : IScriptVars
    {
        public bool ContainsKey(string key) => false;
        public object? Get(Type type, string name) => null;
        public T? Get<T>(string name) => default;
        public T GetRequired<T>(string key) => throw new KeyNotFoundException(key);
        public void Set<T>(string name, T value) { }
    }

    [Test]
    public void FullIgnisProgression_ShouldGrantEveryTier_AtTheRightPoints()
    {
        var moduleProvider = new SorcererModuleProvider();
        var spellFactory = CreateSpellFactory();

        var serviceProvider = MockServiceProvider.CreateBuilder()
                                                 .SetupService<ISorcererModuleProvider>(moduleProvider)
                                                 .SetupService(spellFactory)
                                                 .Build()
                                                 .Object;

        var harness = new AislingScriptHarness<SorcererProgressionScript>(serviceProvider: serviceProvider);
        var source = harness.Source;
        source.UserStatSheet.SetBaseClass(BaseClass.Sorcerer);

        // --- Floor 1: Tier I should grant immediately (Level 1 is already eligible) ---
        source.StatSheet.SetLevel(1);
        harness.Update(TimeSpan.FromSeconds(6));

        foreach (var tierIKey in moduleProvider.GetTierISpellKeys())
            source.SpellBook.ContainsByTemplateKey(tierIKey)
                  .Should()
                  .BeTrue($"Tier I's {tierIKey} should be granted as soon as the Sorcerer exists");

        // --- Too early: the first-choice dialog should refuse below FirstChoiceLevel ---
        var earlyDialog = CreateStubDialog(source, "sorcererChooseFirstElement");

        var earlyFirstChoice = new SorcererChooseFirstElementScript(earlyDialog, moduleProvider, spellFactory) { Element = SorcererElement.Fire };
        earlyFirstChoice.OnDisplaying(source);

        SorcererProgressionHelper.TryGetElement1(source, out _)
                                  .Should()
                                  .BeFalse("the first choice shouldn't be recordable before FirstChoiceLevel");

        // --- Floor 3: choose Fire as the first element (via the real dialog script) ---
        source.StatSheet.SetLevel(SorcererProgressionHelper.FirstChoiceLevel);

        var firstChoiceDialog = CreateStubDialog(source, "sorcererChooseFirstElement");
        var firstChoiceScript = new SorcererChooseFirstElementScript(firstChoiceDialog, moduleProvider, spellFactory) { Element = SorcererElement.Fire };
        firstChoiceScript.OnDisplaying(source);

        SorcererProgressionHelper.TryGetElement1(source, out var recordedElement1)
                                  .Should()
                                  .BeTrue();

        recordedElement1.Should()
                        .Be(SorcererElement.Fire);

        foreach (var fireTierIIKey in moduleProvider.GetTierIISpellKeys(SorcererElement.Fire))
            source.SpellBook.ContainsByTemplateKey(fireTierIIKey)
                  .Should()
                  .BeTrue($"Fire Tier II's {fireTierIIKey} should be granted immediately by the first-choice dialog");

        // --- Too early: Tier IV shouldn't auto-grant yet even though Element1 is set ---
        harness.Update(TimeSpan.FromSeconds(6));

        foreach (var ignisTierIVKey in moduleProvider.GetTierIVSpellKeys(AdvClass.Ignis))
            source.SpellBook.ContainsByTemplateKey(ignisTierIVKey)
                  .Should()
                  .BeFalse("Tier IV shouldn't grant before both elements are resolved, even at a high level");

        // --- Floor 5: choose Fire again as the second element (pure Ignis) ---
        source.StatSheet.SetLevel(SorcererProgressionHelper.SecondChoiceLevel);

        var secondChoiceDialog = CreateStubDialog(source, "sorcererChooseSecondElement");
        var secondChoiceScript = new SorcererChooseSecondElementScript(secondChoiceDialog, moduleProvider, spellFactory) { Element = SorcererElement.Fire };
        secondChoiceScript.OnDisplaying(source);

        SorcererProgressionHelper.TryGetElement2(source, out var recordedElement2)
                                  .Should()
                                  .BeTrue();

        recordedElement2.Should()
                        .Be(SorcererElement.Fire);

        source.UserStatSheet.AdvClass
              .Should()
              .Be(AdvClass.Ignis, "picking Fire twice should resolve to pure Ignis");

        foreach (var fireTierIIIKey in moduleProvider.GetTierIIISpellKeys(SorcererElement.Fire))
            source.SpellBook.ContainsByTemplateKey(fireTierIIIKey)
                  .Should()
                  .BeTrue($"Fire Tier III's {fireTierIIIKey} should be granted immediately by the second-choice dialog");

        // --- Floor 7: Tier IV should now auto-grant via the poller ---
        source.StatSheet.SetLevel(SorcererProgressionHelper.TierIVLevel);
        harness.Update(TimeSpan.FromSeconds(6));

        foreach (var ignisTierIVKey in moduleProvider.GetTierIVSpellKeys(AdvClass.Ignis))
            source.SpellBook.ContainsByTemplateKey(ignisTierIVKey)
                  .Should()
                  .BeTrue($"Ignis Tier IV's {ignisTierIVKey} should be auto-granted once level-eligible and fully resolved");

        // --- Full kit sanity check: exactly the 14 real Ignis abilities (3 Tier I + 4 + 4 + 3, since Shadow
        // Bolt's removal from Shared Tier I dropped the total from 15), nothing missing, nothing stray ---
        var expectedKeys = moduleProvider.GetTierISpellKeys()
                                         .Concat(moduleProvider.GetTierIISpellKeys(SorcererElement.Fire))
                                         .Concat(moduleProvider.GetTierIIISpellKeys(SorcererElement.Fire))
                                         .Concat(moduleProvider.GetTierIVSpellKeys(AdvClass.Ignis))
                                         .ToList();

        expectedKeys.Should()
                    .HaveCount(14);

        foreach (var key in expectedKeys)
            source.SpellBook.ContainsByTemplateKey(key)
                  .Should()
                  .BeTrue();
    }
}
