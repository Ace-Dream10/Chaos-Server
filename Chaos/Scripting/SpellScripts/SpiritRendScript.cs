#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     A direct build - "Fas"/Spirit Rend had no existing precedent to reuse. One of Mystic's 5 evolving abilities,
///     part of the core trio (alongside Stacia's Shrine and Stacia's Judgment) that spans the whole game, checking
///     in every 3 floors. See <see cref="Chaos.Scripting.EffectScripts.SpiritRendEffect" />'s doc comment for how
///     the tier's BEHAVIOR (not just its numbers) changes at Tier I. Tiers per Mystic's own floor arc (Floor1
///     obtain, Floor4, Floor7, Floor10 max/solo finale). All placeholder values, not balance-tested.
/// </summary>
public class SpiritRendScript : ConfigurableSpellScriptBase
{
    /// <inheritdoc />
    public SpiritRendScript(Spell subject)
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
        var tier = GetTierValues();

        if (!source.StatSheet.TrySubtractMp(ManaCost))
        {
            context.SourceAisling?.SendOrangeBarMessage("Not enough focus.");

            return;
        }

        context.SourceAisling?.Client.SendAttributes(StatUpdateType.Vitality);

        source.AnimateBody(BodyAnimation);

        var rendEffect = new SpiritRendEffect
        {
            MagicDamageTakenBonusPct = tier.BonusPct,
            ConsumeOnHit = tier.ConsumeOnHit
        };

        rendEffect.SetDuration(TimeSpan.FromMilliseconds(tier.DurationMs));
        target.Effects.Apply(source, rendEffect, this);

        if (Animation != null)
            target.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.TargetPoint);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor1(Level&lt;=2)=I(obtain,Early,single-consumption mark,
    ///     the next magical hit only), Floor4(&lt;=8)=II(Mid,5s window,whole party can capitalize),
    ///     Floor7(&lt;=14)=III(Late,8-10s window,+DoT amplification via the same bonus), Floor10+(&gt;14)=IV(max,
    ///     solo finale,strongest version of Late).
    /// </summary>
    private (int BonusPct, int DurationMs, bool ConsumeOnHit) GetTierValues() =>
        Subject.Level switch
        {
            <= 2  => (25, 500, true),
            <= 8  => (20, 5000, false),
            <= 14 => (25, 9000, false),
            _     => (35, 9000, false)
        };

    #region ScriptVars
    /// <summary>
    ///     The animation played on the marked target
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
