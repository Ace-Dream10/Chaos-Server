#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Enter a cold calculated state - maximizes Severance potential (instantly fills MP to max and holds it there)
///     and grants lifesteal for the duration.
/// </summary>
/// <remarks>
///     Replaces the generic "applyEffect" shim this skill previously used - a dedicated script is needed so
///     <see cref="ColdBloodEffect" />'s duration and lifesteal can be tier-scaled per the locked design's
///     evolution note ("longer duration... increased lifesteal while active"). Same level-bracket convention
///     <see cref="BastionsChargeScript" /> established.
/// </remarks>
public class ColdBloodScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public ColdBloodScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var tier = GetTierValues();

        source.AnimateBody(BodyAnimation);

        if (Sound.HasValue)
            context.TargetMap.PlaySound(Sound.Value, source);

        var effect = new ColdBloodEffect { LifestealPct = tier.LifestealPct };
        effect.SetDuration(TimeSpan.FromMilliseconds(tier.DurationMs));
        source.Effects.Apply(source, effect, this);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested.
    /// </summary>
    private (int DurationMs, int LifestealPct) GetTierValues() =>
        Subject.Level switch
        {
            <= 2 => (15000, 5),
            <= 4 => (18000, 10),
            <= 6 => (21000, 15),
            _    => (25000, 20)
        };

    #region ScriptVars
    /// <summary>
    ///     The body animation played by the caster when the skill is used
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     Sound played on use
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
