#region
using Chaos.Extensions;
using Chaos.Models.Menu;
using Chaos.Models.World;
using Chaos.Scripting.DialogScripts.Abstractions;
using Chaos.Scripting.ItemScripts;
#endregion

namespace Chaos.Scripting.DialogScripts;

/// <summary>
///     Displays an item's current enhancement level and the Stacia's essence cost to enhance it further
/// </summary>
public class EnhanceItemInfoScript : ConfigurableDialogScriptBase
{
    /// <inheritdoc />
    public EnhanceItemInfoScript(Dialog subject)
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

        Subject.InjectTextParameters(
            $"Your {item.DisplayName} is currently +{enhancement.EnhancementLevel}. "
            + $"Enhancing to +{nextLevel} will cost {cost} Stacia's essence.");
    }

    #region ScriptVars
    /// <summary>
    ///     The Stacia's essence cost per enhancement tier (tier N costs CostPerLevel * N)
    /// </summary>
    public int CostPerLevel { get; init; } = 100;

    /// <summary>
    ///     The templateKey of the item in the player's inventory to show info for
    /// </summary>
    public string ItemTemplateKey { get; init; } = string.Empty;
    #endregion
}
