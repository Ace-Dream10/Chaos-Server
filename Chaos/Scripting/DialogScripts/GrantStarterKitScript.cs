#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Menu;
using Chaos.Models.World;
using Chaos.Scripting.DialogScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.DialogScripts;

/// <summary>
///     Sets the Aisling's class and grants a starting kit of items/gold. Meant for a "new character" guide NPC -
///     the dialog's own "text" field doubles as the instructions shown alongside the grant.
/// </summary>
public class GrantStarterKitScript : ConfigurableDialogScriptBase
{
    private readonly IItemFactory ItemFactory;

    /// <inheritdoc />
    public GrantStarterKitScript(Dialog subject, IItemFactory itemFactory)
        : base(subject)
        => ItemFactory = itemFactory;

    /// <inheritdoc />
    public override void OnDisplaying(Aisling source)
    {
        if (BaseClass.HasValue)
            source.UserStatSheet.SetBaseClass(BaseClass.Value);

        source.UserStatSheet.SetAdvClass(AdvClass ?? Chaos.DarkAges.Definitions.AdvClass.None);

        foreach (var itemTemplateKey in StarterItemTemplateKeys)
        {
            var item = ItemFactory.Create(itemTemplateKey);
            source.TryGiveItems(item);
        }

        if (StarterGold > 0)
            source.TryGiveGold(StarterGold);

        source.Hardcore = Hardcore;

        source.Client.SendAttributes(StatUpdateType.Full);
        source.Client.SendUserId();
    }

    #region ScriptVars
    /// <summary>
    ///     The base class to set on the Aisling
    /// </summary>
    public BaseClass? BaseClass { get; init; }

    /// <summary>
    ///     The advanced class to set on the Aisling, if any
    /// </summary>
    public AdvClass? AdvClass { get; init; }

    /// <summary>
    ///     Whether this character is playing in hardcore mode
    /// </summary>
    public bool Hardcore { get; init; }

    /// <summary>
    ///     The templateKeys of items to grant as a starter kit
    /// </summary>
    public ICollection<string> StarterItemTemplateKeys { get; init; } = [];

    /// <summary>
    ///     The amount of gold to grant alongside the starter kit
    /// </summary>
    public int StarterGold { get; init; }
    #endregion
}
