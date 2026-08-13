#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     A direct build - no existing "strip buffs from an enemy" precedent in the codebase. Mirrors
///     <see cref="StaciasCleanseScript" />'s own approach (there's no "IsBuff" flag on effects, so this matches
///     against a fixed set of known beneficial effect names), just aimed at a hostile target instead of a friendly
///     one, and terminating BUFFS instead of debuffs. The name list is intentionally scoped to this session's own
///     built evolving/notable buffs rather than attempting to enumerate every buff in the game - a scoping
///     decision, flagged rather than silently guessed, same as every other curated-list mechanic built tonight.
/// </summary>
public class UnravelScript : ConfigurableSpellScriptBase
{
    /// <summary>
    ///     The set of known beneficial effect names Unravel can strip from a target. Scoped to this session's own
    ///     built evolving/notable buffs rather than an exhaustive list of every buff in the game.
    /// </summary>
    private static readonly HashSet<string> BuffNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Stacia's Blessing",
        "Battle Hymn",
        "Guardian's Anthem",
        "Stacia's Grace",
        "Crescendo",
        "Blooming Life",
        "Spirit Rend",
        "Regeneration",
        "Bloodlust",
        "Solar Flare"
    };

    /// <inheritdoc />
    public UnravelScript(Spell subject)
        : base(subject) { }

    /// <inheritdoc />
    public override bool CanUse(SpellContext context)
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
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var target = context.TargetCreature!;
        var map = context.TargetMap;

        if (!source.StatSheet.TrySubtractMp(ManaCost))
        {
            context.SourceAisling?.SendOrangeBarMessage("Not enough mana.");

            return;
        }

        context.SourceAisling?.Client.SendAttributes(StatUpdateType.Vitality);

        source.AnimateBody(BodyAnimation);

        foreach (var effect in target.Effects.ToArray())
            if (BuffNames.Contains(effect.Name))
                target.Effects.Terminate(effect.Name);

        if (Animation != null)
            target.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.TargetPoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the unraveled target
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine whether the selected target is valid
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The MP cost to use this spell
    /// </summary>
    public int ManaCost { get; init; }

    /// <summary>
    ///     The maximum distance, in tiles, a target can be selected from
    /// </summary>
    public int Range { get; init; }

    /// <summary>
    ///     Sound played on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
