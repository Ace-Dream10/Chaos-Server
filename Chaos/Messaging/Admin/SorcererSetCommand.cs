#region
using Chaos.Collections.Common;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Messaging.Abstractions;
using Chaos.Models.World;
using Chaos.Services.Factories.Abstractions;
using Chaos.Utilities;
#endregion

namespace Chaos.Messaging.Admin;

/// <summary>
///     TEST-ONLY: directly sets a Sorcerer's element picks and immediately grants the resulting full kit (Tier I,
///     Tier II for the first element, Tier III for the second, and Tier IV for the resolved specialization),
///     bypassing the real level/floor gates and the real dialog flow entirely. Lets you set up a test character as
///     any of the 5 pure or 6 hybrid paths in one command instead of manually leveling through Floor 3/5/7 and
///     clicking through dialogs for every combination you want to check.
/// </summary>
/// <remarks>
///     Purely additive - does NOT modify, replace, or share any code with
///     <see cref="Chaos.Scripting.DialogScripts.SorcererElementStatusScript" />,
///     <see cref="Chaos.Scripting.DialogScripts.SorcererChooseFirstElementScript" />,
///     <see cref="Chaos.Scripting.DialogScripts.SorcererChooseSecondElementScript" />, or the dialog JSON tree real
///     players use. It reuses the same underlying <see cref="SorcererProgressionHelper" /> and
///     <see cref="ISorcererModuleProvider" /> those scripts call, so a character configured through this command
///     ends up in an identical state to one who walked the real dialog flow - same persisted counters, same
///     AdvClass resolution, same templateKeys granted via the same
///     <see cref="Chaos.Utilities.ComplexActionHelper.LearnSpell" /> path. It just skips the level checks and the
///     two separate dialog interactions.
///     <para>
///         Wipes the character's entire spellbook first, unlike the real dialog flow (which only ever adds). This
///         is deliberate: the real flow only ever grants incrementally as floors are reached and never needs to
///         "undo" a prior configuration, but this command is meant to be re-run repeatedly on the same test
///         character to check different combinations - without wiping first, testing Ignis and then Magma would
///         leave both kits mixed together, which no real player could ever end up with.
///     </para>
/// </remarks>
[Command(
    "sorcererset",
    helpText: "<element1> <element2> - TEST-ONLY: sets a Sorcerer's element picks and grants the full resulting kit immediately, bypassing level gates and the real dialog flow")]
public class SorcererSetCommand(ISorcererModuleProvider moduleProvider, ISpellFactory spellFactory) : ICommand<Aisling>
{
    private readonly ISorcererModuleProvider ModuleProvider = moduleProvider;
    private readonly ISpellFactory SpellFactory = spellFactory;

    /// <inheritdoc />
    public ValueTask ExecuteAsync(Aisling source, ArgumentCollection args)
    {
        if (!args.TryGetNext<SorcererElement>(out var element1) || !args.TryGetNext<SorcererElement>(out var element2))
        {
            source.SendOrangeBarMessage("Usage: /sorcererset <element1> <element2> (Fire, Earth, Water, Wind, Arcane)");

            return default;
        }

        if (source.UserStatSheet.BaseClass != BaseClass.Sorcerer)
        {
            source.SendOrangeBarMessage("[TEST] Source is not a Sorcerer - /setclass Sorcerer first.");

            return default;
        }

        //Wipe first - see the class doc comment for why this differs from the real (additive-only) dialog flow
        foreach (var spell in source.SpellBook.ToArray())
            source.SpellBook.Remove(spell.Slot);

        SorcererProgressionHelper.SetElement1(source, element1);
        SorcererProgressionHelper.SetElement2(source, element2);

        var specialization = SorcererProgressionHelper.ResolveSpecialization(element1, element2);
        source.UserStatSheet.SetAdvClass(specialization);

        //Mark Tier I/IV as already granted so SorcererProgressionScript's own poll doesn't attempt to grant them
        //again (harmless either way - LearnSpell on an already-known spell just fails gracefully - but keeps the
        //persisted state honest rather than looking "not yet granted" when it actually is)
        source.Trackers.Counters.Set("sorcererTierGranted", 4);

        SorcererProgressionHelper.GrantSpells(source, SpellFactory, ModuleProvider.GetTierISpellKeys());
        SorcererProgressionHelper.GrantSpells(source, SpellFactory, ModuleProvider.GetTierIISpellKeys(element1));
        SorcererProgressionHelper.GrantSpells(source, SpellFactory, ModuleProvider.GetTierIIISpellKeys(element2));
        SorcererProgressionHelper.GrantSpells(source, SpellFactory, ModuleProvider.GetTierIVSpellKeys(specialization));

        source.Client.SendAttributes(StatUpdateType.Full);
        source.SendOrangeBarMessage($"[TEST] Set to {element1} -> {element2} ({specialization}). Full kit granted.");

        return default;
    }
}
