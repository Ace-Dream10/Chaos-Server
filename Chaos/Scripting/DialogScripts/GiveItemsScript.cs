#region
using Chaos.Models.Menu;
using Chaos.Models.World;
using Chaos.Scripting.DialogScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.DialogScripts;

/// <summary>
///     Grants a fixed list of items directly to the Aisling's inventory, one of each templateKey listed. Does not
///     touch equipment or clear anything first - purely additive. Any templateKey that doesn't exist is skipped
///     silently rather than throwing.
/// </summary>
public class GiveItemsScript : ConfigurableDialogScriptBase
{
    private readonly IItemFactory ItemFactory;

    /// <inheritdoc />
    public GiveItemsScript(Dialog subject, IItemFactory itemFactory)
        : base(subject)
        => ItemFactory = itemFactory;

    /// <inheritdoc />
    public override void OnDisplaying(Aisling source)
    {
        foreach (var templateKey in ItemTemplateKeys)
        {
            try
            {
                var item = ItemFactory.Create(templateKey);
                source.TryGiveItems(item);
            } catch
            {
                //item template doesn't exist - skip gracefully
            }
        }
    }

    #region ScriptVars
    /// <summary>
    ///     The templateKeys of items to grant, one of each. Repeat a key to grant more than one (e.g. two rings).
    /// </summary>
    public ICollection<string> ItemTemplateKeys { get; init; } = [];
    #endregion
}
