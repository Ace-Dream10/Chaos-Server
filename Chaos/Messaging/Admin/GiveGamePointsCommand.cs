using Chaos.Collections.Common;
using Chaos.Extensions.Common;
using Chaos.Messaging.Abstractions;
using Chaos.Models.World;
using Chaos.Networking.Abstractions;

namespace Chaos.Messaging.Admin;

[Command("giveGamePoints", helpText: "<amount|targetName>")]
public class GiveGamePointsCommand(IClientRegistry<IChaosWorldClient> clientRegistry) : ICommand<Aisling>
{
    private readonly IClientRegistry<IChaosWorldClient> ClientRegistry = clientRegistry;

    /// <inheritdoc />
    public ValueTask ExecuteAsync(Aisling source, ArgumentCollection args)
    {
        if (args.TryGetNext<int>(out var amount))
        {
            if (source.TryGiveGamePoints(amount))
                source.SendOrangeBarMessage($"You gave yourself {amount} game points");

            return default;
        }

        if (args.TryGetNext<string>(out var targetName) && args.TryGetNext(out amount))
        {
            var target = ClientRegistry.Select(client => client.Aisling)
                                       .FirstOrDefault(aisling => aisling.Name.EqualsI(targetName));

            if (target == null)
            {
                source.SendOrangeBarMessage($"{targetName} is not online");

                return default;
            }

            if (target.TryGiveGamePoints(amount))
                source.SendOrangeBarMessage($"You gave {target.Name} {amount} game points");
        }

        return default;
    }
}
