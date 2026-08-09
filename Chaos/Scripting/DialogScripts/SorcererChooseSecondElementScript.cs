#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Models.Menu;
using Chaos.Models.World;
using Chaos.Scripting.DialogScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
using Chaos.Utilities;
#endregion

namespace Chaos.Scripting.DialogScripts;

/// <summary>
///     Records a Sorcerer's SECOND elemental pick (unlocks at
///     <see cref="SorcererProgressionHelper.SecondChoiceLevel" />), resolves and locks in the final specialization
///     onto <see cref="Chaos.Models.Data.UserStatSheet.AdvClass" />, and immediately grants that element's Tier III
///     module. One instance per element option (5 total) - picking the SAME element as the first pick is a
///     completely valid, first-class option here, not a special case: per
///     <see cref="SorcererProgressionHelper.ResolveSpecialization" />, picking your first element again IS how a
///     player stays on the pure path (e.g. Fire-then-Fire resolves to Ignis) - there's no separate "remain pure"
///     branch to build or a different code path to worry about diverging from the hybrid case.
/// </summary>
public class SorcererChooseSecondElementScript : ConfigurableDialogScriptBase
{
    private readonly ISorcererModuleProvider ModuleProvider;
    private readonly ISpellFactory SpellFactory;

    /// <inheritdoc />
    public SorcererChooseSecondElementScript(Dialog subject, ISorcererModuleProvider moduleProvider, ISpellFactory spellFactory)
        : base(subject)
    {
        ModuleProvider = moduleProvider;
        SpellFactory = spellFactory;
    }

    /// <inheritdoc />
    public override void OnDisplaying(Aisling source)
    {
        if (source.UserStatSheet.BaseClass != BaseClass.Sorcerer)
        {
            source.SendOrangeBarMessage("You are not a Sorcerer.");

            return;
        }

        if (!SorcererProgressionHelper.TryGetElement1(source, out var element1))
        {
            source.SendOrangeBarMessage("You must choose your first element before your second.");

            return;
        }

        if (source.StatSheet.Level < SorcererProgressionHelper.SecondChoiceLevel)
        {
            source.SendOrangeBarMessage($"You must reach level {SorcererProgressionHelper.SecondChoiceLevel} before continuing your path.");

            return;
        }

        if (SorcererProgressionHelper.TryGetElement2(source, out _))
        {
            source.SendOrangeBarMessage("Your path is already set.");

            return;
        }

        SorcererProgressionHelper.SetElement2(source, Element);

        var specialization = SorcererProgressionHelper.ResolveSpecialization(element1, Element);
        source.UserStatSheet.SetAdvClass(specialization);
        source.Client.SendAttributes(StatUpdateType.Full);

        SorcererProgressionHelper.GrantSpells(source, SpellFactory, ModuleProvider.GetTierIIISpellKeys(Element));

        var pathDescription = element1 == Element ? $"remained pure {specialization}" : $"become {specialization}";
        source.SendOrangeBarMessage($"You have {pathDescription}. Return at level {SorcererProgressionHelper.TierIVLevel} to claim your mastery.");
    }

    #region ScriptVars
    /// <summary>
    ///     The element this specific dialog option represents
    /// </summary>
    public SorcererElement Element { get; init; }
    #endregion
}
