#region
using Chaos.Models.Menu;
using Chaos.Models.World;
using Chaos.Scripting.DialogScripts.Abstractions;
#endregion

namespace Chaos.Scripting.DialogScripts;

/// <summary>
///     Deposits every bankable item in the caller's inventory into their bank in one action. Skips anything with
///     PreventBanking set (mirroring the single-item deposit flow's own check), plus the essence currency items by
///     templateKey specifically, since those are meant to always stay in the player's pack.
/// </summary>
public class BankDepositAllScript : DialogScriptBase
{
    private static readonly HashSet<string> AlwaysSkipTemplateKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "stacias_pouch",
        "stacias_tear"
    };

    /// <inheritdoc />
    public BankDepositAllScript(Dialog subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnDisplaying(Aisling source)
    {
        var depositedCount = 0;

        foreach (var item in source.Inventory.ToArray())
        {
            if (item.PreventBanking || AlwaysSkipTemplateKeys.Contains(item.Template.TemplateKey))
                continue;

            source.Inventory.Remove(item.Slot);
            source.Bank.Deposit(item);
            depositedCount++;
        }

        source.SendOrangeBarMessage(
            depositedCount > 0
                ? "All items deposited to Stacia's Vault."
                : "You have nothing bankable to deposit.");
    }
}
