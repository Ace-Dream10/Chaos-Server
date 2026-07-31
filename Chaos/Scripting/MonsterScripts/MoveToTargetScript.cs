#region
using Chaos.Extensions.Geometry;
using Chaos.Models.World;
using Chaos.Scripting.MonsterScripts.Abstractions;
#endregion

namespace Chaos.Scripting.MonsterScripts;

// ReSharper disable once ClassCanBeSealed.Global
public class MoveToTargetScript : MonsterScriptBase
{
    /// <inheritdoc />
    public MoveToTargetScript(Monster subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        base.Update(delta);

        //frozen by Stasis - don't move
        if (Subject.Trackers.Tags.ContainsKey("stasis"))
            return;

        //rooted by Lancer's Leash - don't move (but can still attack if already in range)
        if (Subject.Trackers.Tags.ContainsKey("rooted"))
            return;

        //asleep from Stacia's Lullaby - completely idle until woken by damage
        if (Subject.Trackers.Tags.ContainsKey("asleep"))
            return;

        if ((Target == null) || !ShouldMove)
            return;

        if (!Map.HasAislings)
            return;

        var distance = Subject.ManhattanDistanceFrom(Target);

        if (distance != 1)
            Subject.Pathfind(Target);
        else
        {
            var direction = Target.DirectionalRelationTo(Subject);
            Subject.Turn(direction);
        }

        Subject.WanderTimer.Reset();
        Subject.SkillTimer.Reset();
    }
}