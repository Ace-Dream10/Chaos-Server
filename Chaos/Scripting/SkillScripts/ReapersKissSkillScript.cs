#region
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Reaper's Kiss is a passive - the actual heal-on-kill logic lives in
///     <see cref="Chaos.Scripting.AislingScripts.ReapersKissScript" />, which runs automatically for any Assassin
///     who has learned this skill. This is just the placeholder activation for the skill pane entry itself.
/// </summary>
public class ReapersKissSkillScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public ReapersKissSkillScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
        => context.SourceAisling?.SendOrangeBarMessage("Reaper's Kiss triggers automatically on kills.");
}
