#region
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.World;
using Chaos.Scripting.MonsterScripts.Abstractions;
#endregion

namespace Chaos.Scripting.MonsterScripts;

/// <summary>
///     Targets nearby hostile MONSTERS instead of Aislings - the inverse of the normal
///     <see cref="AggroTargetingScript" />. Used by Shadow Clone so the decoy fights alongside the Assassin instead
///     of attacking players. Reuses the monster's own <c>AggroRange</c> (from its template) as the search radius.
/// </summary>
// ReSharper disable once ClassCanBeSealed.Global
public class ShadowCloneAggroScript : MonsterScriptBase
{
    /// <inheritdoc />
    public ShadowCloneAggroScript(Monster subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if ((Target is { IsAlive: true } target) && target.OnSameMapAs(Subject))
            return;

        Target = Map.GetEntitiesWithinRange<Monster>(Subject, AggroRange)
                    .Where(monster => monster.IsAlive && !monster.Equals(Subject))
                    .ClosestOrDefault(Subject);
    }
}
