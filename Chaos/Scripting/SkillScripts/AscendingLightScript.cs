#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Removes negative effects from nearby allies. Not one of Valkyrie's 6 evolving abilities - flat, per the
///     locked design. Same fixed debuff-name list/terminate-matching-effects approach as
///     <see cref="StaciasCleansingLightScript" />/<see cref="Chaos.Scripting.SpellScripts.StaciasCleanseScript" />,
///     but AoE across nearby allies rather than self-only.
/// </summary>
public class AscendingLightScript : ConfigurableSkillScriptBase
{
    private static readonly HashSet<string> DebuffNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Poison",
        "Poison Bomb",
        "Slow",
        "Root",
        "Stasis",
        "Blackout",
        "Delirium",
        "Not So Bad Curse",
        "Lullaby",
        "Weakening Shout",
        "Burn"
    };

    /// <inheritdoc />
    public AscendingLightScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        var allies = map.GetEntitiesWithinRange<Creature>(source, Range)
                        .Where(creature => source.IsFriendlyTo(creature) || (creature == source));

        foreach (var ally in allies)
        {
            var cleansed = false;

            foreach (var effect in ally.Effects.ToArray())
                if (DebuffNames.Contains(effect.Name))
                {
                    ally.Effects.Terminate(effect.Name);
                    cleansed = true;
                }

            if (cleansed && (Animation != null))
                ally.Animate(Animation, source.Id);
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, source);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each ally actually cleansed of at least one debuff
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The range, in tiles, within which allies are cleansed
    /// </summary>
    public int Range { get; init; } = 3;

    /// <summary>
    ///     Sound played on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
