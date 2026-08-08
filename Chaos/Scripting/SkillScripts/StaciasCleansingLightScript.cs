#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Bastion's "Stacia's Cleansing Light" (NEW per the locked design, replacing "Weakening Shout" as a
///     standalone ability - Floor 7). Removes negative effects from the caster. Near-trivial clone of Bard's
///     <see cref="Chaos.Scripting.SpellScripts.StaciasCleanseScript" /> (same fixed debuff-name list, same
///     terminate-matching-effects approach) reworked as a self-only Skill rather than a targeted Spell, since
///     Bastion has no unique spells (see <c>Spells/Lancer/</c>, intentionally empty per builder.md).
/// </summary>
public class StaciasCleansingLightScript : ConfigurableSkillScriptBase
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
        "Weakening Shout"
    };

    /// <inheritdoc />
    public StaciasCleansingLightScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        foreach (var effect in source.Effects.ToArray())
            if (DebuffNames.Contains(effect.Name))
                source.Effects.Terminate(effect.Name);

        if (Animation != null)
            source.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, source);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the caster
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used - should stay selfOnly
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     Sound played on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
