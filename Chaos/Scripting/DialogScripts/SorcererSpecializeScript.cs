#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Menu;
using Chaos.Models.World;
using Chaos.Scripting.DialogScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
using Chaos.Utilities;
#endregion

namespace Chaos.Scripting.DialogScripts;

/// <summary>
///     Locks in a Sorcerer's elemental specialization for testing purposes: records the chosen element(s) on
///     <see cref="Chaos.Collections.Trackers.SorcererElement1" />/<see cref="Chaos.Collections.Trackers.SorcererElement2" />,
///     wipes the current spellbook, and grants the configured spell list for that specialization. Unlike
///     <see cref="BecomeClassScript" />, this only touches spells - skills, gear, and class are left alone. Meant for
///     the Sorcerer specialization-testing NPC, not real gameplay; nothing yet enforces the recorded elements as an
///     actual restriction.
/// </summary>
public class SorcererSpecializeScript : ConfigurableDialogScriptBase
{
    private readonly ISpellFactory SpellFactory;

    /// <inheritdoc />
    public SorcererSpecializeScript(Dialog subject, ISpellFactory spellFactory)
        : base(subject)
        => SpellFactory = spellFactory;

    /// <inheritdoc />
    public override void OnDisplaying(Aisling source)
    {
        source.Trackers.SorcererElement1 = Element1;
        source.Trackers.SorcererElement2 = Element2;

        foreach (var spell in source.SpellBook.ToArray())
            source.SpellBook.Remove(spell.Slot);

        foreach (var templateKey in SpellTemplateKeys)
            TryGrantSpell(source, templateKey);

        source.SendOrangeBarMessage($"You have specialized in {SpecializationName}.");
        source.Client.SendAttributes(StatUpdateType.Full);
    }

    private void TryGrantSpell(Aisling source, string templateKey)
    {
        try
        {
            var spell = SpellFactory.Create(templateKey);
            ComplexActionHelper.LearnSpell(source, spell);
        } catch
        {
            //spell template doesn't exist yet - skip gracefully
        }
    }

    #region ScriptVars
    /// <summary>
    ///     The first elemental specialization chosen (e.g. "Fire")
    /// </summary>
    public string? Element1 { get; init; }

    /// <summary>
    ///     The second elemental specialization chosen, if any (e.g. "Earth" for a Magma specialist)
    /// </summary>
    public string? Element2 { get; init; }

    /// <summary>
    ///     The display name of the specialization, used in the completion message
    /// </summary>
    public string SpecializationName { get; init; } = string.Empty;

    /// <summary>
    ///     The templateKeys of spells to grant
    /// </summary>
    public ICollection<string> SpellTemplateKeys { get; init; } = [];
    #endregion
}
