#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     One of Beast's 7 specialization actives - see <see cref="Chaos.Scripting.EffectScripts.BreakEffect" />'s doc
///     comment. Not one of Beast's 3 evolving abilities - flat.
/// </summary>
public class BreakScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public BreakScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        var target = context.TargetCreature;

        if ((target is not { IsAlive: true }) || !Filter.IsValidTarget(source, target))
        {
            context.SourceAisling?.SendOrangeBarMessage("You must select a valid target.");

            return;
        }

        if (context.SourcePoint.ManhattanDistanceFrom(context.TargetPoint) > Range)
        {
            context.SourceAisling?.SendOrangeBarMessage("Your target is too far away.");

            return;
        }

        source.AnimateBody(BodyAnimation);

        var breakEffect = new BreakEffect { BonusDamageTakenPct = BonusDamageTakenPct };
        target.Effects.Apply(source, breakEffect, this);

        if (Animation != null)
            target.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.TargetPoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the target
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The percentage bonus damage the target takes while Break is active
    /// </summary>
    public int BonusDamageTakenPct { get; init; } = 25;

    /// <summary>
    ///     The filter used to determine whether the selected target is valid
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The maximum distance, in tiles, a target can be selected from
    /// </summary>
    public int Range { get; init; } = 5;

    /// <summary>
    ///     Sound played on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
