#region
using Chaos.Extensions;
using Chaos.Models.Menu;
using Chaos.Models.World;
using Chaos.Scripting.DialogScripts.Abstractions;
using Chaos.Scripting.ItemScripts;
#endregion

namespace Chaos.Scripting.DialogScripts;

/// <summary>
///     Charges Stacia's essence and enhances an item by one tier. Spends via TryTakeGamePoints/TryGiveGamePoints
///     rather than manipulating the Stacia's Pouch inventory item directly - GamePoints is the real source of
///     truth and the pouch item is just a display mirror of it (see Aisling.SyncColItem), so spending against the
///     pouch item directly would desync from GamePoints and get silently overwritten the next time anything else
///     touches game points.
/// </summary>
public class EnhanceItemScript : ConfigurableDialogScriptBase
{
    /// <inheritdoc />
    public EnhanceItemScript(Dialog subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnDisplaying(Aisling source)
    {
        if (!source.Inventory.TryGetObjectByTemplateKey(ItemTemplateKey, out var item)
            || !item.Script.Is<EnhancementScript>(out var enhancement))
        {
            Subject.InjectTextParameters("You don't have that item.");

            return;
        }

        if (!enhancement.CanEnhance)
        {
            Subject.InjectTextParameters($"Your {item.DisplayName} is already at maximum enhancement (+{enhancement.EnhancementLevel}).");

            return;
        }

        var nextLevel = enhancement.EnhancementLevel + 1;
        var cost = CostPerLevel * nextLevel;

        if (!source.TryTakeGamePoints(cost))
        {
            Subject.InjectTextParameters($"You need {cost} Stacia's essence to enhance your {item.DisplayName}.");

            return;
        }

        if (!enhancement.TryEnhance())
        {
            //shouldn't happen given the CanEnhance check above, but don't eat the player's essence if it does
            source.TryGiveGamePoints(cost);
            Subject.InjectTextParameters("Something went wrong. Your Stacia's essence has been refunded.");

            return;
        }

        source.Inventory.Update(item.Slot, localItem => localItem.Suffix = $"+{enhancement.EnhancementLevel}");

        Subject.InjectTextParameters($"Your {item.DisplayName} is now +{enhancement.EnhancementLevel}!");
    }

    #region ScriptVars
    /// <summary>
    ///     The Stacia's essence cost per enhancement tier (tier N costs CostPerLevel * N)
    /// </summary>
    public int CostPerLevel { get; init; } = 100;

    /// <summary>
    ///     The templateKey of the item in the player's inventory to enhance
    /// </summary>
    public string ItemTemplateKey { get; init; } = string.Empty;
    #endregion
}
