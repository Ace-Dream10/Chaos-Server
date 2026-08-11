#region
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Applies <see cref="GhostStepEffect" /> to the caster - see that effect's doc comment for the full mechanic.
/// </summary>
public class GhostStepScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public GhostStepScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;

        source.Effects.Apply(source, new GhostStepEffect(), this);
    }
}
