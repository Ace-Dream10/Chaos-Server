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
///     A direct build - "Cradh"/Bard's Malediction had no existing precedent to reuse. One of Bard's 5 evolving
///     abilities - see <see cref="Chaos.Scripting.EffectScripts.BardsMaledictionEffect" />'s doc comment for the
///     tier interpolation. Tiers per Bard's own floor arc (Floor1 obtain, Floor4, Floor7, Floor10 max/solo finale).
///     All placeholder values, not balance-tested.
/// </summary>
public class BardsMaledictionScript : ConfigurableSpellScriptBase
{
    /// <inheritdoc />
    public BardsMaledictionScript(Spell subject)
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
            context.SourceAisling?.SendOrangeBarMessage("Not enough mana.");

            return;
        }

        context.SourceAisling?.Client.SendAttributes(StatUpdateType.Vitality);

        source.AnimateBody(BodyAnimation);

        var maledictionEffect = new BardsMaledictionEffect
        {
            AcPenalty = tier.AcPenalty,
            DamageDealtReductionPct = tier.DamageDealtReductionPct,
            CritVulnerabilityPct = tier.CritVulnerabilityPct
        };
        target.Effects.Apply(source, maledictionEffect, this);

        if (Animation != null)
            target.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.TargetPoint);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor1(Level&lt;=2)=I(obtain,Early,-10% def approx),
    ///     Floor4(&lt;=8)=II(interpolated,~-15% def), Floor7(&lt;=14)=III(Mid,-20% def+dmg reduction),
    ///     Floor10+(&gt;14)=IV(max,Late,-30% def+dmg reduction+crit vulnerability).
    /// </summary>
    private (int AcPenalty, int DamageDealtReductionPct, int CritVulnerabilityPct) GetTierValues() =>
        Subject.Level switch
        {
            <= 2  => (10, 0, 0),
            <= 8  => (15, 0, 0),
            <= 14 => (20, 10, 0),
            _     => (30, 15, 20)
        };

    #region ScriptVars
    /// <summary>
    ///     The animation played on the target on mark
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
    ///     Sound played on mark
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
