#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     Modeled directly on <see cref="RedRequiemScript" /> (Bard's own flat, non-evolving revive) - same core
///     revive mechanic, but rebuilt as a dedicated, tier-aware script since Communion Rite is one of Mystic's 5
///     evolving abilities and Red Requiem stays flat for Bard. Mystic's "protect the individual" identity: its own
///     personal revive, alongside Bard's Red Requiem and Valkyrie's Stacia's Reprieve. Tiers per the locked floor
///     schedule (Floor6 intro, Floor7, Floor8, Floor9 max) - same bracket shape as Bard's Guardian's Anthem. All
///     placeholder values, not balance-tested.
/// </summary>
public class CommunionRiteScript : ConfigurableSpellScriptBase
{
    /// <inheritdoc />
    public CommunionRiteScript(Spell subject)
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
        var tier = GetTierValues();

        if (!source.StatSheet.TrySubtractMp(ManaCost))
        {
            context.SourceAisling?.SendOrangeBarMessage("Not enough mana.");

            return;
        }

        context.SourceAisling?.Client.SendAttributes(StatUpdateType.Vitality);

        source.AnimateBody(BodyAnimation);

        if (target is Aisling aislingTarget)
        {
            aislingTarget.IsDead = false;
            aislingTarget.StatSheet.SetHealthPct(tier.ReviveHpPct);
            aislingTarget.Refresh(true);

            if (tier.ShieldMs > 0)
            {
                var shieldEffect = new CommunionRiteShieldEffect();
                shieldEffect.SetDuration(TimeSpan.FromMilliseconds(tier.ShieldMs));
                aislingTarget.Effects.Apply(source, shieldEffect, this);
            }
        }

        if (Animation != null)
            target.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.TargetPoint);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor6(Level&lt;=12)=I(intro,revive at 25% HP),
    ///     Floor7(&lt;=14)=II(35% HP), Floor8(&lt;=16)=III(50% HP,+brief invulnerability so the revive can't be
    ///     instantly undone), Floor9+(&gt;16)=IV(max,65% HP,+longer invulnerability).
    /// </summary>
    private (int ReviveHpPct, int ShieldMs) GetTierValues() =>
        Subject.Level switch
        {
            <= 12 => (25, 0),
            <= 14 => (35, 0),
            <= 16 => (50, 2000),
            _     => (65, 4000)
        };

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
