#region
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.AislingScripts;
using Chaos.Scripting.SpellScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     Smoke and Mirrors (Trickster passive) special-cases this ability with a direct hook (see
///     <see cref="TricksterIllusionHelper" />, called at the top of OnUse below, before the teleport/animation)
///     instead of TricksterIllusionScript's normal one-tick-later observer pattern - per playtest feedback, the
///     decoy needs to appear BEFORE the jump, and the deferred pattern can only ever detect an ability after it's
///     already fully executed. Every other Smoke and Mirrors trigger keeps using the deferred pattern; this is
///     Shadow Step-only.
/// </summary>
public class ShadowStepScript : ConfigurableSpellScriptBase
{
    private readonly IMonsterFactory MonsterFactory;

    /// <inheritdoc />
    public ShadowStepScript(Spell subject, IMonsterFactory monsterFactory)
        : base(subject)
        => MonsterFactory = monsterFactory;

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

        //Smoke and Mirrors' decoy must appear BEFORE the jump, so this fires here - at the very top of OnUse,
        //using the caster's still-current pre-teleport position - rather than through TricksterIllusionScript's
        //usual deferred (one-tick-later, necessarily post-facto) detection
        if (source is Aisling sourceAisling)
            TricksterIllusionHelper.SpawnIllusionIfLearned(sourceAisling, Point.From(source), MonsterFactory);

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
