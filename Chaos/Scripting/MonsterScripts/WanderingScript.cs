#region
using Chaos.Models.World;
using Chaos.Scripting.MonsterScripts.Abstractions;
#endregion

namespace Chaos.Scripting.MonsterScripts;

// ReSharper disable once ClassCanBeSealed.Global
public class WanderingScript : MonsterScriptBase
{
    /// <inheritdoc />
    public WanderingScript(Monster subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        base.Update(delta);

        //frozen by Stasis - don't wander
        if (Subject.Trackers.Tags.ContainsKey("stasis"))
            return;

        //rooted by Lancer's Leash - don't wander
        if (Subject.Trackers.Tags.ContainsKey("rooted"))
            return;

        if ((Target != null) || !ShouldWander)
            return;

        if (!Map.HasAislings)
            return;

        Subject.Wander();
        Subject.MoveTimer.Reset();
    }
}