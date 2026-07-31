#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts.Abstractions;
using Chaos.Time;
using Chaos.Time.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Redirects a portion of damage taken by nearby group members to the Lancer. The redirected percentage decays
///     over the effect's duration: 100% for the first 2 seconds, 75% for the next 2, 50% for the next 2, and 25% for
///     the final 2. Implemented via a periodic scan (rather than a true pre-damage intercept) since effects are not
///     notified when their subject's allies are attacked.
/// </summary>
public sealed class StaciasWillEffect : IntervalEffectBase
{
    private const int RangeTiles = 5;
    private const TargetFilter AllyFilter = TargetFilter.GroupOnly | TargetFilter.AliveOnly | TargetFilter.OthersOnly;
    private readonly Dictionary<uint, int> LastKnownHp = new();

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(8000);

    /// <inheritdoc />
    protected override IIntervalTimer Interval { get; } = new IntervalTimer(TimeSpan.FromMilliseconds(200), false);

    /// <inheritdoc />
    public override byte Icon => 52;

    /// <inheritdoc />
    public override string Name => "Stacia's Will";

    private decimal CurrentRedirectPct
        => Elapsed.TotalMilliseconds switch
        {
            < 2000 => 1.00m,
            < 4000 => 0.75m,
            < 6000 => 0.50m,
            _      => 0.25m
        };

    /// <inheritdoc />
    public override void OnApplied() => LastKnownHp.Clear();

    /// <inheritdoc />
    protected override void OnIntervalElapsed()
    {
        if (!Subject.IsAlive)
            return;

        var allies = Subject.MapInstance
                            .GetEntitiesWithinRange<Creature>(Subject, RangeTiles)
                            .Where(creature => (creature.Id != Subject.Id) && AllyFilter.IsValidTarget(Source, creature))
                            .ToArray();

        var seenIds = new HashSet<uint>();

        foreach (var ally in allies)
        {
            seenIds.Add(ally.Id);

            if (!LastKnownHp.TryGetValue(ally.Id, out var lastHp))
            {
                LastKnownHp[ally.Id] = ally.StatSheet.CurrentHp;

                continue;
            }

            var currentHp = ally.StatSheet.CurrentHp;
            var damageTaken = lastHp - currentHp;

            if (damageTaken > 0)
            {
                var redirectAmount = Convert.ToInt32(damageTaken * CurrentRedirectPct);

                if (redirectAmount > 0)
                {
                    ally.StatSheet.AddHp(redirectAmount);
                    (ally as Aisling)?.Client.SendAttributes(StatUpdateType.Vitality);

                    Subject.StatSheet.SubtractHp(redirectAmount);
                    AislingSubject?.Client.SendAttributes(StatUpdateType.Vitality);
                    AislingSubject?.ShowHealth();

                    if (!Subject.IsAlive)
                        AislingSubject?.Script.OnDeath();
                }
            }

            LastKnownHp[ally.Id] = ally.StatSheet.CurrentHp;
        }

        //stop tracking anyone who left range
        foreach (var staleId in LastKnownHp.Keys.Where(id => !seenIds.Contains(id)).ToArray())
            LastKnownHp.Remove(staleId);
    }

    /// <inheritdoc />
    public override void OnTerminated() => LastKnownHp.Clear();
}
