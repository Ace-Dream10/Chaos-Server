#region
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
///     Revives a single skulled ally (per Elysium's skull/revive mechanic - a character drops to 0 HP, "skulls",
///     and can be "redded"/revived). Renamed from "Rally of the Fallen" - the original name implied a group
///     effect, but this is single-target. Ties to Stacia's domain (grace, life, protection), distinct from Bard's
///     Red Requiem. Not one of Valkyrie's 6 evolving abilities - flat, per the locked design.
/// </summary>
/// <remarks>
///     Near-direct port of <see cref="Chaos.Scripting.SpellScripts.RedRequiemScript" /> to Skill form (Valkyrie
///     has no unique spells - all her abilities are Skills, unlike Bard) - same revive shape
///     (<c>IsDead = false</c>, <c>SetHealthPct</c>, <c>Refresh</c>), same range-check pattern.
/// </remarks>
public class StaciasReprieveScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public StaciasReprieveScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override bool CanUse(ActivationContext context)
    {
        if (!context.Source.IsAlive)
            return false;

        if ((context.TargetCreature is not { } target) || !Filter.IsValidTarget(context.Source, target))
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

        if (target is Aisling aislingTarget)
        {
            aislingTarget.IsDead = false;
            aislingTarget.StatSheet.SetHealthPct(ReviveHpPct);
            aislingTarget.Refresh(true);
        }

        if (Animation != null)
            target.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.TargetPoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the revived target
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
    ///     The maximum distance, in tiles, a target can be selected from
    /// </summary>
    public int Range { get; init; }

    /// <summary>
    ///     The percentage of max HP the target is revived with
    /// </summary>
    public int ReviveHpPct { get; init; } = 25;

    /// <summary>
    ///     Sound played on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
