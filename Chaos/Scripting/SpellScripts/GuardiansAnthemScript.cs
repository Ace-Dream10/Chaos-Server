#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     A direct build - nothing existing matched the iconic raid-saving cooldown. One of Bard's 5 evolving
///     abilities, with a fully detailed locked-design tier progression: "Lv.1: protect one ally, 3 seconds, 100%
///     damage immunity. Mid: one ally, longer duration. Mid+: small area around the target, full immunity. Late:
///     larger area. Max: entire party, 5-6 seconds, 100% damage immunity." 5 named stages squeeze onto the floor
///     schedule's 4 checkpoints (Floor6 intro, Floor7, Floor8, Floor9 max) by folding "Late" (larger area) into
///     "Max" (the whole party is definitionally the largest possible area) - see
///     <see cref="Chaos.Scripting.EffectScripts.GuardiansAnthemEffect" /> for the invulnerability mechanic. All
///     placeholder values, not balance-tested.
/// </summary>
public class GuardiansAnthemScript : ConfigurableSpellScriptBase
{
    /// <inheritdoc />
    public GuardiansAnthemScript(Spell subject)
        : base(subject) { }

    /// <inheritdoc />
    public override bool CanUse(SpellContext context)
    {
        if (!context.Source.IsAlive)
            return false;

        if ((context.TargetCreature is not { IsAlive: true } target) || !Filter.IsValidTarget(context.Source, target))
        {
            context.SourceAisling?.SendOrangeBarMessage("You must select a valid ally.");

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
        var tier = GetTierValues();

        if (!source.StatSheet.TrySubtractMp(ManaCost))
        {
            context.SourceAisling?.SendOrangeBarMessage("Not enough mana.");

            return;
        }

        context.SourceAisling?.Client.SendAttributes(StatUpdateType.Vitality);

        source.AnimateBody(BodyAnimation);

        IEnumerable<Creature> protectedAllies;

        if (tier.PartyWide && (target is Aisling { Group: not null } groupedAisling))
            protectedAllies = groupedAisling.Group.Cast<Creature>()
                                             .Append(groupedAisling)
                                             .Distinct();
        else if (tier.AoeRadius <= 0)
            protectedAllies = new List<Creature> { target };
        else
            protectedAllies = map.GetEntitiesWithinRange<Creature>(target, tier.AoeRadius)
                                 .Where(creature => Filter.IsValidTarget(source, creature));

        foreach (var ally in protectedAllies)
        {
            var anthemEffect = new GuardiansAnthemEffect();
            anthemEffect.SetDuration(TimeSpan.FromMilliseconds(tier.DurationMs));
            ally.Effects.Apply(source, anthemEffect, this);

            if (Animation != null)
                ally.Animate(Animation, source.Id);
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.TargetPoint);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor6(Level&lt;=12)=I(intro,single ally,3s),
    ///     Floor7(&lt;=14)=II(single,4s,"longer duration"), Floor8(&lt;=16)=III(small AoE radius1,4s,"Mid+"),
    ///     Floor9+(&gt;16)=IV(max,entire party,6s,folds in "Late"'s larger area).
    /// </summary>
    private (int DurationMs, int AoeRadius, bool PartyWide) GetTierValues() =>
        Subject.Level switch
        {
            <= 12 => (3000, 0, false),
            <= 14 => (4000, 0, false),
            <= 16 => (4000, 1, false),
            _     => (6000, 0, true)
        };

    #region ScriptVars
    /// <summary>
    ///     The animation played on each protected ally
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine whether the selected ally target is valid
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
