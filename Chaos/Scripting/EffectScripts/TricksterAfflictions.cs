#region
using Chaos.DarkAges.Definitions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.Abstractions;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Shared definition of Trickster's 4 "mental afflictions" (Blackout, Delirium, Puppeteer, Root) - used by
///     Deceiver's Cache (applies a random one), Grand Finale (consumes every one affecting nearby enemies),
///     Psychological Warfare (bonus damage against an afflicted target), and Chain Reaction (spread/retrigger on
///     expiry, called from each of the 4 effects' own OnTerminated below). Centralizing the list here means all 4
///     Trickster mechanics that care about "is this a mental affliction" stay in sync with a single set, rather
///     than four independent hardcoded name checks drifting apart over time.
/// </summary>
public static class TricksterAfflictions
{
    /// <summary>
    ///     Placeholder, not balance-tested: the odds Chain Reaction fires at all when a mental affliction expires,
    ///     and (given it fires) the odds it spreads to a nearby enemy vs. retriggers a different affliction on the
    ///     same target.
    /// </summary>
    private const double ChainReactionProcChance = 0.35;

    private const int ChainReactionSpreadRange = 3;
    private const double ChainReactionSpreadVsRetriggerChance = 0.5;

    public static readonly IReadOnlyDictionary<string, Func<EffectBase>> Factories = new Dictionary<string, Func<EffectBase>>(StringComparer.OrdinalIgnoreCase)
    {
        ["Blackout"] = () => new BlackoutEffect(),
        ["Delirium"] = () => new DeliriumEffect(),
        ["Puppeteer"] = () => new PuppeteerEffect(),
        ["Root"] = () => new RootEffect()
    };

    /// <summary>
    ///     Picks a random affliction, optionally excluding one by name (used by Chain Reaction's "retrigger a
    ///     DIFFERENT random affliction" wording)
    /// </summary>
    public static EffectBase CreateRandom(string? excludeName = null)
    {
        var candidates = Factories.Where(kvp => !string.Equals(kvp.Key, excludeName, StringComparison.OrdinalIgnoreCase))
                                  .ToList();

        var (_, factory) = candidates[Random.Shared.Next(candidates.Count)];

        return factory();
    }

    /// <summary>
    ///     Counts how many of Trickster's 4 mental afflictions (and how many distinct kinds) are currently active
    ///     on a target - used by Grand Finale's damage scaling
    /// </summary>
    public static (int Count, int Variety) CountActive(Creature target)
    {
        var count = 0;
        var variety = 0;

        foreach (var name in Factories.Keys)
        {
            if (!target.Effects.TryGetEffect(name, out _))
                continue;

            count++;
            variety++;
        }

        return (count, variety);
    }

    /// <summary>
    ///     Terminates every active mental affliction on a target - used by Grand Finale
    /// </summary>
    public static void ConsumeAll(Creature target)
    {
        foreach (var name in Factories.Keys)
            target.Effects.Terminate(name);
    }

    /// <summary>
    ///     Chain Reaction (Trickster passive) - called from each of Blackout/Delirium/Puppeteer/Root's own
    ///     OnTerminated. Only fires if the affliction was applied by an Aisling Trickster who has actually learned
    ///     Chain Reaction (gated the same way Scorch/Kindling gate on "has learned this passive" in
    ///     ApplyAttackDamageScript). Placeholder odds throughout, not balance-tested.
    /// </summary>
    public static void TryChainReact(Creature subject, Creature source, string expiringAfflictionName, IScript? sourceScript)
    {
        if ((source is not Aisling aisling)
            || (aisling.UserStatSheet.BaseClass != BaseClass.Trickster)
            || !aisling.SkillBook.TryGetObjectByTemplateKey("chain_reaction", out _))
            return;

        if (Random.Shared.NextDouble() >= ChainReactionProcChance)
            return;

        if (Random.Shared.NextDouble() < ChainReactionSpreadVsRetriggerChance)
        {
            //spread: apply the SAME affliction that just expired to a nearby enemy
            if (!Factories.TryGetValue(expiringAfflictionName, out var factory))
                return;

            var nearby = subject.MapInstance
                                .GetEntitiesWithinRange<Monster>(Point.From(subject), ChainReactionSpreadRange)
                                .FirstOrDefault(monster => !monster.Equals(subject) && monster.IsAlive && aisling.IsHostileTo(monster));

            if (nearby != null)
                nearby.Effects.Apply(source, factory(), sourceScript);
        } else
        {
            //retrigger: apply a DIFFERENT random affliction to the same target
            if (subject.IsAlive)
                subject.Effects.Apply(source, CreateRandom(expiringAfflictionName), sourceScript);
        }
    }
}
