#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Models.Panel;
using Chaos.Scripting.AislingScripts;
using Chaos.Services.Factories.Abstractions;
using Chaos.Testing.Infrastructure.Harnesses;
using Chaos.Testing.Infrastructure.Mocks;
using Chaos.Utilities;
using FluentAssertions;
using Moq;
#endregion

namespace Chaos.Tests;

/// <summary>
///     Confirms SorcererProgressionScript's floor-triggered auto-granting (Tier I and Tier IV - the two tiers that
///     don't require a player choice) fires at the right points and only once, using a fake
///     <see cref="ISorcererModuleProvider" /> so this doesn't depend on any real Phase 2 content existing yet.
/// </summary>
public sealed class SorcererProgressionScriptTests
{
    private static readonly TimeSpan PastCheckInterval = TimeSpan.FromSeconds(6);

    private static IServiceProvider CreateServiceProvider(Mock<ISorcererModuleProvider> moduleProviderMock)
    {
        var spellFactoryMock = new Mock<ISpellFactory>();

        spellFactoryMock.Setup(f => f.Create(It.IsAny<string>(), null))
                        .Returns((string templateKey, ICollection<string>? _) => MockSpell.Create(templateKey));

        return MockServiceProvider.CreateBuilder()
                                  .SetupService(moduleProviderMock.Object)
                                  .SetupService(spellFactoryMock.Object)
                                  .Build()
                                  .Object;
    }

    private static Mock<ISorcererModuleProvider> CreateModuleProviderMock()
    {
        var mock = new Mock<ISorcererModuleProvider>();
        mock.Setup(m => m.GetTierISpellKeys()).Returns(["tier1_test_spell"]);
        mock.Setup(m => m.GetTierIVSpellKeys(It.IsAny<AdvClass>())).Returns(["tier4_test_spell"]);

        return mock;
    }

    [Test]
    public void ShouldNotGrantAnything_ForNonSorcerers()
    {
        var moduleProviderMock = CreateModuleProviderMock();
        var serviceProvider = CreateServiceProvider(moduleProviderMock);

        var harness = new AislingScriptHarness<SorcererProgressionScript>(serviceProvider: serviceProvider);
        harness.Source.UserStatSheet.SetBaseClass(BaseClass.Berserker);
        harness.Source.StatSheet.SetLevel(10);

        harness.Update(PastCheckInterval);

        harness.Source.SpellBook.ContainsByTemplateKey("tier1_test_spell")
               .Should()
               .BeFalse();
    }

    [Test]
    public void ShouldGrantTierI_AssoonAsEligible_ButOnlyOnce()
    {
        var moduleProviderMock = CreateModuleProviderMock();
        var serviceProvider = CreateServiceProvider(moduleProviderMock);

        var harness = new AislingScriptHarness<SorcererProgressionScript>(serviceProvider: serviceProvider);
        harness.Source.UserStatSheet.SetBaseClass(BaseClass.Sorcerer);
        harness.Source.StatSheet.SetLevel(1);

        harness.Update(PastCheckInterval);

        harness.Source.SpellBook.ContainsByTemplateKey("tier1_test_spell")
               .Should()
               .BeTrue();

        //remove it and tick again - a second grant would re-add it; it shouldn't happen since Tier I is
        //already recorded as granted
        harness.Source.SpellBook.RemoveByTemplateKey("tier1_test_spell");
        harness.Update(PastCheckInterval);

        harness.Source.SpellBook.ContainsByTemplateKey("tier1_test_spell")
               .Should()
               .BeFalse();

        moduleProviderMock.Verify(m => m.GetTierISpellKeys(), Times.Once);
    }

    [Test]
    public void ShouldNotGrantTierIV_BeforeLevelThreshold_EvenIfBothElementsChosen()
    {
        var moduleProviderMock = CreateModuleProviderMock();
        var serviceProvider = CreateServiceProvider(moduleProviderMock);

        var harness = new AislingScriptHarness<SorcererProgressionScript>(serviceProvider: serviceProvider);
        harness.Source.UserStatSheet.SetBaseClass(BaseClass.Sorcerer);
        harness.Source.StatSheet.SetLevel(SorcererProgressionHelper.TierIVLevel - 1);

        SorcererProgressionHelper.SetElement1(harness.Source, SorcererElement.Fire);
        SorcererProgressionHelper.SetElement2(harness.Source, SorcererElement.Fire);

        harness.Update(PastCheckInterval);

        harness.Source.SpellBook.ContainsByTemplateKey("tier4_test_spell")
               .Should()
               .BeFalse();
    }

    [Test]
    public void ShouldNotGrantTierIV_AtLevelThreshold_IfElementsNotYetChosen()
    {
        var moduleProviderMock = CreateModuleProviderMock();
        var serviceProvider = CreateServiceProvider(moduleProviderMock);

        var harness = new AislingScriptHarness<SorcererProgressionScript>(serviceProvider: serviceProvider);
        harness.Source.UserStatSheet.SetBaseClass(BaseClass.Sorcerer);
        harness.Source.StatSheet.SetLevel(SorcererProgressionHelper.TierIVLevel);

        harness.Update(PastCheckInterval);

        harness.Source.SpellBook.ContainsByTemplateKey("tier4_test_spell")
               .Should()
               .BeFalse();
    }

    [Test]
    public void ShouldGrantTierIV_OnceLevelEligible_AndBothElementsChosen()
    {
        var moduleProviderMock = CreateModuleProviderMock();
        var serviceProvider = CreateServiceProvider(moduleProviderMock);

        var harness = new AislingScriptHarness<SorcererProgressionScript>(serviceProvider: serviceProvider);
        harness.Source.UserStatSheet.SetBaseClass(BaseClass.Sorcerer);
        harness.Source.StatSheet.SetLevel(SorcererProgressionHelper.TierIVLevel);

        SorcererProgressionHelper.SetElement1(harness.Source, SorcererElement.Fire);
        SorcererProgressionHelper.SetElement2(harness.Source, SorcererElement.Earth);

        harness.Update(PastCheckInterval);

        harness.Source.SpellBook.ContainsByTemplateKey("tier4_test_spell")
               .Should()
               .BeTrue();

        moduleProviderMock.Verify(m => m.GetTierIVSpellKeys(AdvClass.Magma), Times.Once);
    }

    [Test]
    public void ShouldNotRecheckEligibility_BeforeCheckIntervalElapses()
    {
        var moduleProviderMock = CreateModuleProviderMock();
        var serviceProvider = CreateServiceProvider(moduleProviderMock);

        var harness = new AislingScriptHarness<SorcererProgressionScript>(serviceProvider: serviceProvider);
        harness.Source.UserStatSheet.SetBaseClass(BaseClass.Sorcerer);
        harness.Source.StatSheet.SetLevel(1);

        harness.Update(TimeSpan.FromSeconds(1));

        harness.Source.SpellBook.ContainsByTemplateKey("tier1_test_spell")
               .Should()
               .BeFalse();
    }
}
