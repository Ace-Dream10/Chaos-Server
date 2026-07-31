#region
using Chaos.Models.Menu;
using Chaos.Models.World;
using Chaos.Scripting.DialogScripts.Abstractions;
#endregion

namespace Chaos.Scripting.DialogScripts;

/// <summary>
///     General Bank NPC interactions. Deposit/withdraw of individual items reuses the existing generic bank
///     dialogs (generic_deposititem_initial, generic_withdrawitem_initial) - this script only backs the
///     bank-specific leaves, like checking your balance.
/// </summary>
public class BankScript : DialogScriptBase
{
    /// <inheritdoc />
    public BankScript(Dialog subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnDisplaying(Aisling source)
    {
        switch (Subject.Template.TemplateKey.ToLower())
        {
            case "bank_balance":
                OnDisplayingBalance(source);

                break;
        }
    }

    private void OnDisplayingBalance(Aisling source)
    {
        var itemCount = source.Bank.Count();

        Subject.InjectTextParameters(source.Bank.Gold, itemCount);
    }
}
