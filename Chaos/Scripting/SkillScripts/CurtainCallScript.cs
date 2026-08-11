#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     A direct build - nothing existing matched "cloak your entire party in illusion magic, causing all allies to
///     vanish from enemy sight and immediately drop aggro." Reuses <see cref="VanishEffect" /> directly (already
///     does exactly "clear aggro map-wide + tag as vanished, breaks on the wearer's own next action") applied to
///     every member of the caster's <see cref="Aisling.Group" /> plus the caster themself - same
///     Group-iteration pattern <see cref="DivineInterventionScript" />'s Tier IV established for Valkyrie. Flat,
///     non-evolving per the locked design's updated floor schedule (unlocks Floor 10 as part of the finale).
/// </summary>
public class CurtainCallScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public CurtainCallScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        Cloak(source, source);

        if (source is Aisling { Group: not null } aisling)
            foreach (var member in aisling.Group)
                Cloak(source, member);

        if (Animation != null)
            map.ShowAnimation(Animation.GetPointAnimation(context.SourcePoint, source.Id));

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    private void Cloak(Creature caster, Creature target)
    {
        var vanishEffect = new VanishEffect();
        vanishEffect.SetDuration(TimeSpan.FromMilliseconds(VanishDurationMs));
        target.Effects.Apply(caster, vanishEffect, this);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played at the caster's position on cast
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     Sound played at the caster's position on cast
    /// </summary>
    public byte? Sound { get; init; }

    /// <summary>
    ///     How long, in milliseconds, the party stays vanished
    /// </summary>
    public int VanishDurationMs { get; init; } = 4000;
    #endregion
}
