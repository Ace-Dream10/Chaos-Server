#region
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.MonsterScripts.Abstractions;
using Chaos.Time;
using Chaos.Time.Abstractions;
#endregion

namespace Chaos.Scripting.MonsterScripts;

// ReSharper disable once ClassCanBeSealed.Global
public class AggroTargetingScript : MonsterScriptBase
{
    private readonly IIntervalTimer TargetUpdateTimer;
    private int InitialAggro = 10;

    /// <inheritdoc />
    public AggroTargetingScript(Monster subject)
        : base(subject)
        => TargetUpdateTimer = new IntervalTimer(TimeSpan.FromMilliseconds(Math.Min(250, Subject.Template.SkillIntervalMs)));

    /// <inheritdoc />
    public override void OnAttacked(Creature source, int damage, int? aggroOverride)
    {
        if (source.Equals(Subject))
            return;

        var aggro = aggroOverride ?? damage;

        if (aggro == 0)
            return;

        AggroList.AddAggro(source, aggro);
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        base.Update(delta);

        //frozen by Stasis - don't acquire or hold a target
        if (Subject.Trackers.Tags.ContainsKey("stasis"))
            return;

        //feared by Intimidating Shout - lose target and don't re-acquire one
        if (Subject.Trackers.Tags.ContainsKey("feared"))
        {
            Target = null;

            return;
        }

        TargetUpdateTimer.Update(delta);

        if ((Target != null) && (!Target.IsAlive || !Target.OnSameMapAs(Subject)))
        {
            AggroList.Clear(Target);
            Target = null;
        }

        if (!TargetUpdateTimer.IntervalElapsed)
            return;

        Target = null;

        if (!Map.HasAislings)
            return;

        //confused by Delirium - attack a random nearby creature instead of the usual aggro/aisling logic
        if (Subject.Trackers.Tags.ContainsKey("delirium"))
        {
            Subject.Effects.TryGetEffect("Delirium", out var deliriumEffect);
            var caster = deliriumEffect?.Source;

            var candidates = Map.GetEntitiesWithinRange<Creature>(Subject, AggroRange)
                                .Where(creature => creature.IsAlive
                                                    && !creature.Equals(Subject)
                                                    && ((caster == null) || !creature.Equals(caster))
                                                    && Subject.CanSee(creature))
                                .ToArray();

            Target = candidates.Length > 0 ? candidates[Random.Shared.Next(candidates.Length)] : null;

            return;
        }

        //puppeteered by Trickster's Puppeteer - attack other monsters instead of Aislings
        if (Subject.Trackers.Tags.ContainsKey("puppeteered"))
        {
            Target = Map.GetEntitiesWithinRange<Monster>(Subject, AggroRange)
                        .Where(monster => monster.IsAlive && !monster.Equals(Subject))
                        .ClosestOrDefault(Subject);

            return;
        }

        var isBlind = Subject.IsBlind;

        //first try to get target via aggro list
        //if something is already aggro, ignore aggro range
        foreach (var kvp in AggroList.OrderByDescending(kvp => kvp.Value))
        {
            if (!Map.TryGetEntity<Creature>(kvp.Key, out var possibleTarget))
                continue;

            if (!possibleTarget.IsAlive || !Subject.CanSee(possibleTarget) || !possibleTarget.WithinRange(Subject))
                continue;

            //vanished via Trickster's Vanishing Act - can't be (re)targeted while hidden
            if (possibleTarget.Trackers.Tags.ContainsKey("vanished"))
                continue;

            //if we're blind, we can only target things within 1 tile
            if (isBlind && !possibleTarget.WithinRange(Subject, 1))
                continue;

            Target = possibleTarget;

            break;
        }

        if (Target != null)
            return;

        //if blind, we can only target things within 1 space
        var range = isBlind ? 1 : AggroRange;

        //if we failed to get a target via aggroList, grab the closest aisling within aggro range
        Target ??= Map.GetEntitiesWithinRange<Aisling>(Subject, range)
                      .ThatAreVisibleTo(Subject)
                      .Where(obj => !obj.Equals(Subject)
                                    && obj.IsAlive
                                    && !obj.Trackers.Tags.ContainsKey("vanished")
                                    && Subject.ApproachTime.TryGetValue(obj, out var time)
                                    && ((DateTime.UtcNow - time).TotalSeconds >= 1.5))
                      .ClosestOrDefault(Subject);

        //since we grabbed a new target, give them some initial aggro so we stick to them
        if (Target != null)
            AggroList.AddAggro(Target, InitialAggro++);
    }
}