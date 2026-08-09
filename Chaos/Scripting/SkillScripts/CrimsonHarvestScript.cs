#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     For the duration, heal for a percentage of all damage you deal. Renamed from "Bloodlust" to resolve the
///     naming collision with Assassin's resource and Beast's passive. Not one of Slayer's 5 evolving abilities -
///     flat numbers, per the locked design.
/// </summary>
public class CrimsonHarvestScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public CrimsonHarvestScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;

        source.AnimateBody(BodyAnimation);

        if (Sound.HasValue)
            context.TargetMap.PlaySound(Sound.Value, source);

        var effect = new CrimsonHarvestEffect { LifestealPct = LifestealPct };
        effect.SetDuration(TimeSpan.FromMilliseconds(DurationMs));
        source.Effects.Apply(source, effect, this);
    }

    #region ScriptVars
    /// <summary>
    ///     The body animation played by the caster when the skill is used
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     How long, in milliseconds, the lifesteal buff lasts
    /// </summary>
    public int DurationMs { get; init; } = 8000;

    /// <summary>
    ///     The percentage of damage dealt that's returned as healing while active
    /// </summary>
    public int LifestealPct { get; init; } = 20;

    /// <summary>
    ///     Sound played on use
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
