#region
using Chaos.Collections;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     One of Ironscale's 7 specialization actives - "force nearby enemies to attack you" per the locked design. A
///     scoped-down version of Bastion's own Challenging Shout (whole-map mass aggro, evolving) - Taunt is a
///     simpler, radius-based, non-evolving pull, same AggroList.AddAggro pattern.
/// </summary>
public class TauntScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public TauntScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        foreach (var monster in map.GetEntitiesWithinRange<Monster>(source, Radius))
            if (monster.IsAlive && Filter.IsValidTarget(source, monster))
            {
                monster.AggroList.AddAggro(source, AggroAmount);

                if (Animation != null)
                    monster.Animate(Animation, source.Id);
            }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The amount of threat added to every hostile in range
    /// </summary>
    public int AggroAmount { get; init; } = 2000;

    /// <summary>
    ///     The animation played on each taunted monster
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine which nearby monsters are valid taunt targets
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The radius around the caster within which monsters are taunted
    /// </summary>
    public int Radius { get; init; } = 5;

    /// <summary>
    ///     Sound played on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
