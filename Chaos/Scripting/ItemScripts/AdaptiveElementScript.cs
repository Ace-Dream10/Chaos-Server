using Chaos.DarkAges.Definitions;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.ItemScripts.Abstractions;

namespace Chaos.Scripting.ItemScripts;

/// <summary>
///     While equipped, the wielder's offense element is recalculated on every attack to whatever element performs
///     best against the current target's defense element, instead of a fixed element.
/// </summary>
public class AdaptiveElementScript : ConfigurableItemScriptBase
{
    /// <inheritdoc />
    public AdaptiveElementScript(Item subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnEquipped(Aisling aisling)
    {
        aisling.StatSheet.SetAdaptiveOffenseElement(true);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    /// <inheritdoc />
    public override void OnUnEquipped(Aisling aisling)
    {
        aisling.StatSheet.SetAdaptiveOffenseElement(false);
        aisling.StatSheet.SetOffenseElement(Element.None);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }
}
