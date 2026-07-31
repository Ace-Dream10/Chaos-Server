using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.ItemScripts.Abstractions;

namespace Chaos.Scripting.ItemScripts;

/// <summary>
///     Prevents this item from being dropped on the ground or given to another creature. Combine with NoTrade in
///     the item's template to fully lock an item to its owner (NoTrade alone still allows dropping).
/// </summary>
public class UndroppableScript : ConfigurableItemScriptBase
{
    /// <inheritdoc />
    public UndroppableScript(Item subject)
        : base(subject) { }

    /// <inheritdoc />
    public override bool CanBeDropped(Aisling source, Point targetPoint)
    {
        source.SendOrangeBarMessage("This item cannot be dropped.");

        return false;
    }

    /// <inheritdoc />
    public override bool CanBeDroppedOn(Aisling source, Creature creature)
    {
        source.SendOrangeBarMessage("This item cannot be given away.");

        return false;
    }
}
