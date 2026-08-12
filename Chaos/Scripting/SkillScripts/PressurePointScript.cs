#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     One of Ironscale's 7 specialization actives, and one of its 3 evolving abilities - see
///     <see cref="Chaos.Scripting.EffectScripts.PressurePointEffect" />'s doc comment. All placeholder values, not
///     balance-tested.
/// </summary>
public class PressurePointScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public PressurePointScript(Skill subject)
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

        var tier = GetTierValues();

        source.AnimateBody(BodyAnimation);

        Mark(source, target, tier);

        //full raid-support debuff at max tier - spreads to nearby enemies too, not just the primary target
        if (tier.SpreadRadius > 0)
            foreach (var nearby in map.GetEntitiesWithinRange<Creature>(target, tier.SpreadRadius))
            {
                if (nearby.Equals(target) || !Filter.IsValidTarget(source, nearby))
                    continue;

                Mark(source, nearby, tier);
            }

        if (Animation != null)
            target.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.TargetPoint);
    }

    private void Mark(Creature source, Creature target, (int BonusDamageTakenPct, int SpreadRadius) tier)
    {
        var pressurePointEffect = new PressurePointEffect { BonusDamageTakenPct = tier.BonusDamageTakenPct };
        target.Effects.Apply(source, pressurePointEffect, this);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Per the locked Floor Schedule, Pressure Point intros and
    ///     jumps straight to II together at Floor 5 (4-named-stages-onto-3-checkpoints squeeze, same as Perfect
    ///     Form): Floor5(Level&lt;=10)=intro+II combined(+25% damage taken), Floor6(&lt;=12)=III(+35%),
    ///     Floor7+(&gt;12)=IV(max,+45%,+spreads to nearby enemies - "full raid-support debuff").
    /// </summary>
    private (int BonusDamageTakenPct, int SpreadRadius) GetTierValues() =>
        Subject.Level switch
        {
            <= 10 => (25, 0),
            <= 12 => (35, 0),
            _     => (45, 2)
        };

    #region ScriptVars
    /// <summary>
    ///     The animation played on the primary target
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
    public int Range { get; init; } = 5;

    /// <summary>
    ///     Sound played on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
