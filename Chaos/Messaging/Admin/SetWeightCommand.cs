using Chaos.Collections.Common;
using Chaos.DarkAges.Definitions;
using Chaos.Messaging.Abstractions;
using Chaos.Models.World;

namespace Chaos.Messaging.Admin;

[Command("setweight", helpText: "<maxWeight>")]
public class SetWeightCommand : ICommand<Aisling>
{
    /// <inheritdoc />
    public ValueTask ExecuteAsync(Aisling source, ArgumentCollection args)
    {
        if (!args.TryGetNext(out int maxWeight))
            return default;

        source.UserStatSheet.SetMaxWeight(maxWeight);

        source.Client.SendAttributes(StatUpdateType.Full);

        return default;
    }
}
