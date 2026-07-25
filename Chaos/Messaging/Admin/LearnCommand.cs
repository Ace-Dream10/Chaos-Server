using Chaos.Collections.Common;
using Chaos.Extensions.Common;
using Chaos.Messaging.Abstractions;
using Chaos.Models.World;
using Chaos.Networking.Abstractions;
using Chaos.Services.Factories.Abstractions;

namespace Chaos.Messaging.Admin;

[Command("learn", helpText: "<spell|skill> <templateKey> <name?>")]
public class LearnCommand(
    ISpellFactory spellFactory,
    ISkillFactory skillFactory,
    IClientRegistry<IChaosWorldClient> clientRegistry) : ICommand<Aisling>
{
    private readonly IClientRegistry<IChaosWorldClient> ClientRegistry = clientRegistry;
    private readonly ISkillFactory SkillFactory = skillFactory;
    private readonly ISpellFactory SpellFactory = spellFactory;

    /// <inheritdoc />
    public ValueTask ExecuteAsync(Aisling source, ArgumentCollection args)
    {
        if (!args.TryGetNext<string>(out var type))
            return default;

        if (!args.TryGetNext<string>(out var templateKey))
            return default;

        var target = source;

        if (args.TryGetNext<string>(out var name))
        {
            var targetClient = ClientRegistry.FirstOrDefault(cli => cli.Aisling.Name.EqualsI(name));

            if (targetClient == null)
            {
                source.SendOrangeBarMessage($"{name} can not be found");

                return default;
            }

            target = targetClient.Aisling;
        }

        switch (type.ToLower())
        {
            case "spell":
                var spell = SpellFactory.Create(templateKey);

                target.SpellBook.TryAddToNextSlot(spell);

                break;
            case "skill":
                var skill = SkillFactory.Create(templateKey);

                target.SkillBook.TryAddToNextSlot(skill);

                break;
        }

        return default;
    }
}