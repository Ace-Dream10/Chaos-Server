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
///     One of Tempest's 7 specialization actives - "encase a target in swirling feathers, preventing movement for
///     a short time" per the locked design. Directly reuses RootEffect (same as Anchor Hold/every other CC
///     application this session) rather than inventing a parallel root mechanic. Not one of Tempest's 5 evolving
///     abilities - flat.
/// </summary>
public class FeatherPrisonScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public FeatherPrisonScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override bool CanUse(ActivationContext context)
    {
        if (!context.Source.IsAlive)
            return false;

        if ((context.TargetCreature is not { IsAlive: true } target) || !Filter.IsValidTarget(context.Source, target))
        {
            context.SourceAisling?.SendOrangeBarMessage("You must select a valid target.");

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
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var target = context.TargetCreature!;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        var rootEffect = new RootEffect();
        rootEffect.SetDuration(TimeSpan.FromMilliseconds(DurationMs));
        target.Effects.Apply(source, rootEffect, this);

        if (Animation != null)
            target.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.TargetPoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the imprisoned target
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     How long, in milliseconds, the target is rooted for
    /// </summary>
    public int DurationMs { get; init; } = 3000;

    /// <summary>
    ///     The filter used to determine whether the selected target is valid
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The maximum distance, in tiles, a target can be selected from
    /// </summary>
    public int Range { get; init; } = 8;

    /// <summary>
    ///     Sound played on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
