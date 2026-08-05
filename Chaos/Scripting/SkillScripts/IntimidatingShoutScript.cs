#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

public class IntimidatingShoutScript : ConfigurableSkillScriptBase
{
    private static readonly Direction[] CardinalDirections = [Direction.Up, Direction.Right, Direction.Down, Direction.Left];

    /// <inheritdoc />
    public IntimidatingShoutScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        //only the 4 tiles directly adjacent to the caster (north/south/east/west), not a full radius
        foreach (var direction in CardinalDirections)
        {
            var point = context.SourcePoint.DirectionalOffset(direction);

            foreach (var monster in map.GetEntitiesAtPoints<Monster>(point))
            {
                if (!Filter.IsValidTarget(source, monster))
                    continue;

                //the direction the monster would need to face to be looking at the caster, reversed to face away
                var directionToFaceCaster = context.SourcePoint.DirectionalRelationTo(Point.From(monster));
                monster.Turn(directionToFaceCaster.Reverse(), forced: true);

                var fearedEffect = new FearedEffect();
                fearedEffect.SetDuration(TimeSpan.FromMilliseconds(FearDurationMs));
                monster.Effects.Apply(source, fearedEffect, this);

                if (Animation != null)
                    monster.Animate(Animation, source.Id);
            }
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, source);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each affected monster
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     How long, in milliseconds, affected monsters are feared for
    /// </summary>
    public int FearDurationMs { get; init; } = 2000;

    /// <summary>
    ///     The filter used to determine which monsters within range are affected
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The sound played at the caster's position on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
