#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyHealing;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     A direct build - nothing existing matched an evolving emergency-healing miracle closely enough (Stacia's
///     Vitae is the closest single-target-burst-heal precedent, reused as the shape but not the file). One of
///     Bard's 5 evolving abilities - unlike the "Trio" (Stacia's Blessing/Battle Hymn/Bard's Malediction), Salvation
///     follows the "universal first evolution on Floor 3" schedule instead: Floor2 intro, Floor3 first evolution,
///     Floor4, Floor5 max. Tiers per the locked design's evolution track ("Large heal -&gt; Stronger heal -&gt;
///     Splash healing -&gt; Better cooldown -&gt; End-game emergency miracle") - "Better cooldown" isn't a separate
///     tier's whole identity so much as a property of the max tier, folded into Tier IV alongside the emergency-
///     miracle bonus (heals for more, uncapped, the lower the target's HP is) rather than treated as its own
///     checkpoint. All placeholder values, not balance-tested.
/// </summary>
public class SalvationScript : ConfigurableSpellScriptBase
{
    /// <inheritdoc />
    public SalvationScript(Spell subject)
        : base(subject)
        => ApplyHealScript = FunctionalScripts.ApplyHealing.ApplyHealScript.Create();

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
            context.SourceAisling?.SendOrangeBarMessage("Not enough focus.");

            return;
        }

        context.SourceAisling?.Client.SendAttributes(StatUpdateType.Vitality);

        source.AnimateBody(BodyAnimation);

        Heal(source, target, tier);

        if (tier.SplashRadius > 0)
            foreach (var nearbyAlly in map.GetEntitiesWithinRange<Creature>(target, tier.SplashRadius))
            {
                if (nearbyAlly.Equals(target) || !Filter.IsValidTarget(source, nearbyAlly))
                    continue;

                Heal(source, nearbyAlly, tier);
            }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.TargetPoint);
    }

    private void Heal(Creature source, Creature target, (int BaseHeal, decimal HealStatMultiplier, int SplashRadius, bool EmergencyMiracle) tier)
    {
        var healing = tier.BaseHeal + Convert.ToInt32(source.StatSheet.GetEffectiveStat(Stat.WIS) * tier.HealStatMultiplier);

        if (tier.EmergencyMiracle)
        {
            var missingHpPct = 1m - (target.StatSheet.CurrentHp / (decimal)Math.Max(1, target.StatSheet.EffectiveMaximumHp));
            healing += Convert.ToInt32(healing * missingHpPct);
        }

        if (healing <= 0)
            return;

        ApplyHealScript.ApplyHeal(source, target, this, healing);

        if (Animation != null)
            target.Animate(Animation, source.Id);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor2(Level&lt;=4)=I(intro,large heal),
    ///     Floor3(&lt;=6)=II(first evolution,stronger heal), Floor4(&lt;=8)=III(+splash healing),
    ///     Floor5+(&gt;8)=IV(max,better cooldown handled via lower CooldownMs in the JSON itself,+emergency
    ///     miracle scaling for critically low targets).
    /// </summary>
    private (int BaseHeal, decimal HealStatMultiplier, int SplashRadius, bool EmergencyMiracle) GetTierValues() =>
        Subject.Level switch
        {
            <= 4 => (300, 5m, 0, false),
            <= 6 => (450, 6m, 0, false),
            <= 8 => (450, 6m, 2, false),
            _    => (600, 7m, 2, true)
        };

    #region ScriptVars
    /// <summary>
    ///     The animation played on each healed target
    /// </summary>
    public Animation? Animation { get; init; }

    public IApplyHealScript ApplyHealScript { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine whether a given creature is a valid heal target
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
