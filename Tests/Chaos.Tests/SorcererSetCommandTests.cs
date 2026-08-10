#region
using Chaos.Collections.Common;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Messaging.Admin;
using Chaos.Scripting.FunctionalScripts;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Services.Factories;
using Chaos.Services.Factories.Abstractions;
using Chaos.Testing.Infrastructure.Mocks;
using Chaos.Utilities;
using FluentAssertions;
using Moq;
#endregion

namespace Chaos.Tests;

/// <summary>
///     Confirms /sorcererset actually reuses the real granting mechanism (SorcererProgressionHelper +
///     ISorcererModuleProvider) rather than a parallel/duplicate path - the point of the command is that a
///     character configured through it is representative of one who walked the real dialog flow.
/// </summary>
public sealed class SorcererSetCommandTests
{
    static SorcererSetCommandTests()
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

    [Test]
    public async Task ExecuteAsync_ShouldGrantExactlyTheSameKit_TheRealModuleProviderWouldProduce()
    {
        var moduleProvider = new SorcererModuleProvider();
        var spellFactory = CreateSpellFactory();
        var command = new SorcererSetCommand(moduleProvider, spellFactory);

        var aisling = MockAisling.Create();
        aisling.UserStatSheet.SetBaseClass(BaseClass.Sorcerer);

        await command.ExecuteAsync(aisling, new ArgumentCollection("Fire Earth"));

        SorcererProgressionHelper.TryGetElement1(aisling, out var element1)
                                  .Should()
                                  .BeTrue();

        element1.Should()
                .Be(SorcererElement.Fire);

        SorcererProgressionHelper.TryGetElement2(aisling, out var element2)
                                  .Should()
                                  .BeTrue();

        element2.Should()
                .Be(SorcererElement.Earth);

        aisling.UserStatSheet.AdvClass
               .Should()
               .Be(AdvClass.Magma);

        //the exact same keys the real dialog flow's own module-provider calls would produce - not hardcoded here,
        //so this test would fail if the command's grant logic ever drifted from the real provider calls
        var expectedKeys = moduleProvider.GetTierISpellKeys()
                                         .Concat(moduleProvider.GetTierIISpellKeys(SorcererElement.Fire))
                                         .Concat(moduleProvider.GetTierIIISpellKeys(SorcererElement.Earth))
                                         .Concat(moduleProvider.GetTierIVSpellKeys(AdvClass.Magma))
                                         .ToList();

        foreach (var key in expectedKeys)
            aisling.SpellBook.ContainsByTemplateKey(key)
                   .Should()
                   .BeTrue($"{key} should have been granted via the real module provider");

        aisling.Trackers.Counters.TryGetValue("sorcererTierGranted", out var grantedTier)
               .Should()
               .BeTrue();

        grantedTier.Should()
                   .Be(4);
    }

    [Test]
    public async Task ExecuteAsync_ShouldNotRequireAnyLevel_UnlikeTheRealDialogFlow()
    {
        var moduleProvider = new SorcererModuleProvider();
        var spellFactory = CreateSpellFactory();
        var command = new SorcererSetCommand(moduleProvider, spellFactory);

        var aisling = MockAisling.Create();
        aisling.UserStatSheet.SetBaseClass(BaseClass.Sorcerer);
        aisling.StatSheet.SetLevel(1); //well below FirstChoiceLevel/SecondChoiceLevel

        await command.ExecuteAsync(aisling, new ArgumentCollection("Water Water"));

        aisling.UserStatSheet.AdvClass
               .Should()
               .Be(AdvClass.Hydrosage, "the shortcut should bypass level gates entirely");
    }

    [Test]
    public async Task ExecuteAsync_ShouldWipeThePreviousConfiguration_WhenRunAgainWithDifferentElements()
    {
        var moduleProvider = new SorcererModuleProvider();
        var spellFactory = CreateSpellFactory();
        var command = new SorcererSetCommand(moduleProvider, spellFactory);

        var aisling = MockAisling.Create();
        aisling.UserStatSheet.SetBaseClass(BaseClass.Sorcerer);

        await command.ExecuteAsync(aisling, new ArgumentCollection("Fire Fire"));

        aisling.UserStatSheet.AdvClass
               .Should()
               .Be(AdvClass.Ignis);

        foreach (var key in moduleProvider.GetTierIVSpellKeys(AdvClass.Ignis))
            aisling.SpellBook.ContainsByTemplateKey(key)
                   .Should()
                   .BeTrue();

        //re-run with a completely different combination
        await command.ExecuteAsync(aisling, new ArgumentCollection("Water Wind"));

        aisling.UserStatSheet.AdvClass
               .Should()
               .Be(AdvClass.Blizzard);

        //Ignis's Tier IV content should be gone, not left mixed in alongside Blizzard's
        foreach (var key in moduleProvider.GetTierIVSpellKeys(AdvClass.Ignis))
            aisling.SpellBook.ContainsByTemplateKey(key)
                   .Should()
                   .BeFalse($"{key} is Ignis-only and should have been wiped by the reconfiguration");
    }

    [Test]
    public async Task ExecuteAsync_ShouldReject_WhenSourceIsNotASorcerer()
    {
        var moduleProvider = new SorcererModuleProvider();
        var spellFactory = CreateSpellFactory();
        var command = new SorcererSetCommand(moduleProvider, spellFactory);

        var aisling = MockAisling.Create();
        aisling.UserStatSheet.SetBaseClass(BaseClass.Berserker);

        await command.ExecuteAsync(aisling, new ArgumentCollection("Fire Fire"));

        SorcererProgressionHelper.TryGetElement1(aisling, out _)
                                  .Should()
                                  .BeFalse();
    }
}
