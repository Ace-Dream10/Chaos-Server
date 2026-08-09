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
///     Records a Sorcerer's FIRST elemental pick (unlocks at
///     <see cref="SorcererProgressionHelper.FirstChoiceLevel" />) and immediately grants that element's Tier II
///     module. One instance of this script is attached per element option in the choice menu (5 total, one per
///     <see cref="SorcererElement" /> value) - mirrors <see cref="ChooseBeastFormScript" />'s shape (a permanent,
///     one-time choice recorded directly by the confirming dialog), not the old, now-deleted
///     SorcererSpecializeScript test scaffolding's one-shot full-kit dump.
/// </summary>
public class SorcererChooseFirstElementScript : ConfigurableDialogScriptBase
{
    private readonly ISorcererModuleProvider ModuleProvider;
    private readonly ISpellFactory SpellFactory;

    /// <inheritdoc />
    public SorcererChooseFirstElementScript(Dialog subject, ISorcererModuleProvider moduleProvider, ISpellFactory spellFactory)
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

        if (source.StatSheet.Level < SorcererProgressionHelper.FirstChoiceLevel)
        {
            source.SendOrangeBarMessage($"You must reach level {SorcererProgressionHelper.FirstChoiceLevel} before choosing your path.");

            return;
        }

        if (SorcererProgressionHelper.TryGetElement1(source, out _))
        {
            source.SendOrangeBarMessage("You have already chosen your first element.");

            return;
        }

        SorcererProgressionHelper.SetElement1(source, Element);
        SorcererProgressionHelper.GrantSpells(source, SpellFactory, ModuleProvider.GetTierIISpellKeys(Element));

        source.SendOrangeBarMessage($"You have embraced {Element}. Return at level {SorcererProgressionHelper.SecondChoiceLevel} to continue your path.");
    }

    #region ScriptVars
    /// <summary>
    ///     The element this specific dialog option represents
    /// </summary>
    public SorcererElement Element { get; init; }
    #endregion
}
