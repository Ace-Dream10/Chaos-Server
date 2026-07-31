#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

public class RedRequiemScript : ConfigurableSpellScriptBase
{
    /// <inheritdoc />
    public RedRequiemScript(Spell subject)
        : base(subject) { }

    /// <inheritdoc />
    public override bool CanUse(SpellContext context)
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
    public override void OnUse(SpellContext context)
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
    ///     Whether this spell only ever affects a single target
    /// </summary>
    public bool SingleTarget { get; init; }

    /// <summary>
    ///     The sound played on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
