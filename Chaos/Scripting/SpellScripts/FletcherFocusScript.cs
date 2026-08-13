#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     One of Fletcher's 5 evolving abilities. Switched from the generic <see cref="ApplyEffectScript" /> to a
///     dedicated script so each tier can construct <see cref="FocusEffect" /> with its own Dmg/Hit bonus, the same
///     "properties set by the skill script before applying" convention <see cref="BloodlustScript" /> established
///     for <see cref="BloodlustEffect" />. Tiers per Fletcher's own floor arc (Floor2 intro, Floor3, Floor4,
///     Floor5 max) via the same "Level ≈ 2×Floor" ratio used throughout tonight - see <see cref="GetTierValues" />.
///     Keeps the explicit Fletcher "focus" MP cost/message this skill already had. All placeholder values, not
///     balance-tested.
/// </summary>
public class FletcherFocusScript : ConfigurableSpellScriptBase
{
    /// <inheritdoc />
    public FletcherFocusScript(Spell subject)
        : base(subject) { }

    /// <summary>
    ///     The MP cost to use this skill
    /// </summary>
    public int ManaCost { get; init; }

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var tier = GetTierValues();

        if (!source.StatSheet.TrySubtractMp(ManaCost))
        {
            if (source is Aisling manaAisling)
                manaAisling.SendOrangeBarMessage("Not enough mana.");

            return;
        }

        if (source is Aisling attackerAisling)
            attackerAisling.Client.SendAttributes(StatUpdateType.Vitality);

        source.AnimateBody(BodyAnimation);

        var focusEffect = new FocusEffect
        {
            DmgBonus = tier.DmgBonus,
            HitBonus = tier.HitBonus
        };
        focusEffect.SetDuration(TimeSpan.FromMilliseconds(tier.DurationMs));
        source.Effects.Apply(source, focusEffect, this);

        if (Animation != null)
            source.Animate(Animation, source.Id);

        if (Sound.HasValue)
            context.TargetMap.PlaySound(Sound.Value, context.SourcePoint);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor2(Level&lt;=4)=I(intro,+25/+25,8s),
    ///     Floor3(&lt;=6)=II(+35/+35,8s), Floor4(&lt;=8)=III(+45/+45,10s), Floor5+(&gt;8)=IV(max,+60/+60,10s).
    /// </summary>
    private (int DmgBonus, int HitBonus, int DurationMs) GetTierValues() =>
        Subject.Level switch
        {
            <= 4 => (25, 25, 8000),
            <= 6 => (35, 35, 8000),
            <= 8 => (45, 45, 10000),
            _    => (60, 60, 10000)
        };

    #region ScriptVars
    /// <summary>
    ///     The animation played on the caster on activation
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster when the skill is used
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     Sound played on activation
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
