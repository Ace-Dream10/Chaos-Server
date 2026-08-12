#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     One of Ironscale's 7 specialization actives - see
///     <see cref="Chaos.Scripting.EffectScripts.PerfectCounterEffect" />'s doc comment. Not one of Ironscale's 5
///     evolving abilities - flat.
/// </summary>
public class PerfectCounterScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public PerfectCounterScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        source.Effects.Apply(source, new PerfectCounterEffect(), this);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     Sound played on activation
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
