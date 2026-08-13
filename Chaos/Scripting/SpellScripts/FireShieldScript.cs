#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     Surround yourself with a protective flame barrier. See <see cref="FireShieldEffect" /> for the absorb +
///     eruption mechanics.
/// </summary>
public class FireShieldScript : ConfigurableSpellScriptBase
{
    /// <inheritdoc />
    public FireShieldScript(Spell subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;

        if (!source.StatSheet.TrySubtractMp(ManaCost))
        {
            context.SourceAisling?.SendOrangeBarMessage("Not enough mana.");

            return;
        }

        context.SourceAisling?.Client.SendAttributes(StatUpdateType.Vitality);

        source.AnimateBody(BodyAnimation);

        if (Sound.HasValue)
            context.TargetMap.PlaySound(Sound.Value, source);

        var effect = new FireShieldEffect
        {
            ShieldAmount = ShieldAmount,
            EruptDamage = EruptDamage,
            EruptBurnStacks = EruptBurnStacks
        };

        effect.SetDuration(TimeSpan.FromMilliseconds(DurationMs));
        source.Effects.Apply(source, effect, this);
    }

    #region ScriptVars
    /// <summary>
    ///     The body animation played by the caster when the spell is used
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     How long, in milliseconds, the shield lasts before naturally expiring (and erupting)
    /// </summary>
    public int DurationMs { get; init; } = 15000;

    /// <summary>
    ///     The Burn stacks applied to each nearby enemy on eruption
    /// </summary>
    public int EruptBurnStacks { get; init; } = 2;

    /// <summary>
    ///     The damage dealt to each nearby enemy on eruption (break or expiry)
    /// </summary>
    public int EruptDamage { get; init; } = 60;

    /// <summary>
    ///     The MP cost to use this spell
    /// </summary>
    public int ManaCost { get; init; }

    /// <summary>
    ///     The amount of damage the shield can absorb
    /// </summary>
    public int ShieldAmount { get; init; } = 150;

    /// <summary>
    ///     Sound played on use
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
