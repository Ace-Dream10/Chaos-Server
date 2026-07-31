#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Menu;
using Chaos.Models.World;
using Chaos.NLog.Logging.Definitions;
using Chaos.NLog.Logging.Extensions;
using Chaos.Scripting.DialogScripts.Abstractions;
#endregion

namespace Chaos.Scripting.DialogScripts;

public class SetItemColorScript : ConfigurableDialogScriptBase
{
    private readonly ILogger<SetItemColorScript> Logger;

    /// <inheritdoc />
    public SetItemColorScript(Dialog subject, ILogger<SetItemColorScript> logger)
        : base(subject)
        => Logger = logger;

    /// <inheritdoc />
    public override void OnDisplaying(Aisling source)
    {
        if (!source.Inventory.TryGetObjectByTemplateKey(ItemTemplateKey, out var item))
        {
            Logger.WithTopics(Topics.Entities.Aisling, Topics.Entities.Item)
                  .WithProperty(source)
                  .LogInformation(
                      "SetItemColorScript: {@AislingName} does not have an item with templateKey {@ItemTemplateKey}",
                      source.Name,
                      ItemTemplateKey);

            source.SendOrangeBarMessage("You don't have that item.");

            return;
        }

        Logger.WithTopics(Topics.Entities.Aisling, Topics.Entities.Item)
              .WithProperty(source)
              .WithProperty(item)
              .LogInformation(
                  "SetItemColorScript: found {@ItemTemplateKey} in slot {@Slot} for {@AislingName}, current color {@CurrentColor}, setting to {@NewColor}",
                  ItemTemplateKey,
                  item.Slot,
                  source.Name,
                  item.Color,
                  Color);

        source.Inventory.Update(item.Slot, localItem => localItem.Color = Color);

        Logger.WithTopics(Topics.Entities.Aisling, Topics.Entities.Item)
              .WithProperty(source)
              .WithProperty(item)
              .LogInformation("SetItemColorScript: after update, item.Color is now {@Color}", item.Color);
    }

    #region ScriptVars
    /// <summary>
    ///     The color to dye the item
    /// </summary>
    public DisplayColor Color { get; init; }

    /// <summary>
    ///     The templateKey of the item in the player's inventory to dye
    /// </summary>
    public string ItemTemplateKey { get; init; } = string.Empty;
    #endregion
}
