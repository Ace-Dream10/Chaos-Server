#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.World;
using Chaos.Scripting.AislingScripts.Abstractions;
using Chaos.Scripting.EffectScripts;
#endregion

namespace Chaos.Scripting.AislingScripts;

/// <summary>
///     Flowing Chi (Tempest passive) - a true always-on passive: "using different abilities in succession
///     empowers the next ability." Watches <see cref="Chaos.Collections.Trackers.LastSkillUse" /> for a change
///     (same event-diff-detection pattern CrescendoScript/SpiritualAttunementScript/BedrockScript already
///     established for observing an event this script doesn't own). Unlike Crescendo (which builds stacks over 5
///     casts of ANYTHING), Flowing Chi's own wording is about VARIETY - it only grants/refreshes
///     <see cref="FlowingChiEffect" /> when the just-used skill's templateKey differs from the one immediately
///     before it; repeating the same skill twice in a row doesn't refresh the buff. Martial Artist's entire kit is
///     Skill-typed (no Spells), so this only needs to watch skill usage, not spell usage.
/// </summary>
public class FlowingChiScript : AislingScriptBase
{
    private string? LastObservedSkillKey;
    private DateTime? LastObservedSkillUse;

    /// <inheritdoc />
    public FlowingChiScript(Aisling subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (Subject.UserStatSheet.BaseClass != BaseClass.MartialArtist)
        {
            LastObservedSkillUse = null;
            LastObservedSkillKey = null;

            return;
        }

        if (!Subject.SkillBook.TryGetObjectByTemplateKey("flowing_chi", out _))
            return;

        var lastSkillUse = Subject.Trackers.LastSkillUse;
        var castDetected = lastSkillUse.HasValue && (lastSkillUse != LastObservedSkillUse);

        if (!castDetected)
            return;

        LastObservedSkillUse = lastSkillUse;

        var usedSkillKey = Subject.Trackers.LastUsedSkill?.Template.TemplateKey;
        var wasDifferentAbility = (usedSkillKey != null) && (usedSkillKey != LastObservedSkillKey);

        LastObservedSkillKey = usedSkillKey;

        if (wasDifferentAbility)
        {
            Subject.Effects.Terminate("Flowing Chi");
            Subject.Effects.Apply(Subject, new FlowingChiEffect(), this);
        }
    }
}
