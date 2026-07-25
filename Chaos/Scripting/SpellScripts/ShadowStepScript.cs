#region
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

public class ShadowStepScript : ConfigurableSpellScriptBase
{
    /// <inheritdoc />
    public ShadowStepScript(Spell subject)
        : base(subject) { }

    /// <inheritdoc />
    public override bool CanUse(SpellContext context)
    {
        if (!context.Source.IsAlive)
            return false;

        if (context.TargetCreature is not { IsAlive: true } target || target.Equals(context.Source))
        {
            context.SourceAisling?.SendOrangeBarMessage("You must select a target.");

            return false;
        }

        if (context.SourcePoint.ManhattanDistanceFrom(context.TargetPoint) > Range)
        {
            context.SourceAisling?.SendOrangeBarMessage("Your target is too far away.");

            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var target = context.TargetCreature!;
        var map = context.TargetMap;

        //get the direction that vectors behind the target relative to the source
        var behindTargetDirection = target.DirectionalRelationTo(context.SourcePoint);

        //for each direction around the target, starting with the direction behind the target
        foreach (var direction in behindTargetDirection.AsEnumerable())
        {
            //get the point in that direction
            var destinationPoint = target.DirectionalOffset(direction);

            //if that point is not walkable or is a reactor, continue
            if (!map.IsWalkable(destinationPoint, source, false))
                continue;

            //if it is walkable, warp to that point and turn to face the target
            source.WarpTo(destinationPoint);
            var newDirection = target.DirectionalRelationTo(source);
            source.Turn(newDirection);

            if (Sound.HasValue)
                map.PlaySound(Sound.Value, destinationPoint);

            if (Animation != null)
                source.Animate(Animation, source.Id);

            return;
        }

        context.SourceAisling?.SendOrangeBarMessage("There is nowhere to land near your target.");
    }

    #region ScriptVars
    /// <summary>
    ///     The maximum distance, in tiles, a target can be selected from
    /// </summary>
    public int Range { get; init; }

    /// <summary>
    ///     The sound played when the teleport lands
    /// </summary>
    public byte? Sound { get; init; }

    /// <summary>
    ///     The animation played on the source when the teleport lands
    /// </summary>
    public Animation? Animation { get; init; }
    #endregion
}
