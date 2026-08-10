#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     Empower yourself with solar fire, temporarily increasing Fire damage and Burn application. See
///     <see cref="SolarFlareEffect" /> for the buff itself.
/// </summary>
public class SolarFlareScript : ConfigurableSpellScriptBase
{
    /// <inheritdoc />
    public SolarFlareScript(Spell subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;

        source.AnimateBody(BodyAnimation);

        if (Sound.HasValue)
            context.TargetMap.PlaySound(Sound.Value, source);

        var effect = new SolarFlareEffect
        {
            FireDamageBonusPct = FireDamageBonusPct,
            BonusKindlingChance = BonusKindlingChance
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
    ///     The additional Burn proc chance (0-1) added to Kindling's own roll while active
    /// </summary>
    public double BonusKindlingChance { get; init; } = 0.2;

    /// <summary>
    ///     How long, in milliseconds, the buff lasts
    /// </summary>
    public int DurationMs { get; init; } = 12000;

    /// <summary>
    ///     The bonus Fire-element damage percentage granted while active
    /// </summary>
    public int FireDamageBonusPct { get; init; } = 25;

    /// <summary>
    ///     Sound played on use
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
