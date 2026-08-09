#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Menu;
using Chaos.Models.World;
using Chaos.Scripting.DialogScripts.Abstractions;
using Chaos.Utilities;
#endregion

namespace Chaos.Scripting.DialogScripts;

/// <summary>
///     Entry point for a Sorcerer's element-choice flow. Replaces the old flat
///     <c>sorcerer_specialize_menu</c> test scaffolding (a single static menu offering every outcome at once) with
///     a dialog that reflects the player's REAL current progression state - checks what's already been chosen and
///     what level they are, then redirects (<see cref="Dialog.Reply" />) to whichever of the four real states
///     applies: not eligible yet, choose first element, choose second element, or already fully specialized.
///     Nothing here is guessed content - every branch maps directly to
///     <see cref="SorcererProgressionHelper" />'s persisted state.
/// </summary>
public class SorcererElementStatusScript : ConfigurableDialogScriptBase
{
    /// <inheritdoc />
    public SorcererElementStatusScript(Dialog subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnDisplaying(Aisling source)
    {
        if (source.UserStatSheet.BaseClass != BaseClass.Sorcerer)
        {
            Subject.Reply(source, "You are not a Sorcerer.");

            return;
        }

        if (!SorcererProgressionHelper.TryGetElement1(source, out var element1))
        {
            if (source.StatSheet.Level < SorcererProgressionHelper.FirstChoiceLevel)
            {
                Subject.Reply(
                    source,
                    $"Your Arcane fundamentals aren't ready yet. Return at level {SorcererProgressionHelper.FirstChoiceLevel} to choose your first element.");

                return;
            }

            Subject.Reply(source, "Choose your first element. This determines your path's foundation.", "sorcerer_choose_first_menu");

            return;
        }

        if (!SorcererProgressionHelper.TryGetElement2(source, out _))
        {
            if (source.StatSheet.Level < SorcererProgressionHelper.SecondChoiceLevel)
            {
                Subject.Reply(
                    source,
                    $"You've embraced {element1}. Return at level {SorcererProgressionHelper.SecondChoiceLevel} to continue your path.");

                return;
            }

            Subject.Reply(
                source,
                $"You've embraced {element1}. Choose your second element now - picking {element1} again keeps you pure, picking a different element forges a hybrid path.",
                "sorcerer_choose_second_menu");

            return;
        }

        Subject.Reply(source, $"Your path is set: {source.UserStatSheet.AdvClass}. There is nothing more to choose.");
    }
}
