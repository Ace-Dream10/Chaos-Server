#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.World;
using Chaos.Scripting.AislingScripts.Abstractions;
using Chaos.Scripting.EffectScripts;
#endregion

namespace Chaos.Scripting.AislingScripts;

/// <summary>
///     One with the Wind (Tempest passive) - a true always-on passive: "mobility and chi techniques briefly
///     increase movement speed and evasion." Same LastSkillUse-diff-detection pattern as Flowing Chi/Perfect
///     Rhythm, triggering specifically off <see cref="TriggerTemplateKeys" /> - the "mobility" active (Flickering
///     Step) plus the "chi technique" ranged actives (Void Palm, Chi Bullet, Star Cross) - a curated, finite
///     trigger list rather than an open-ended "any ability" catch-all, same scoping approach as every other
///     curated-list mechanic built this session.
/// </summary>
public class OneWithTheWindScript : AislingScriptBase
{
    private static readonly HashSet<string> TriggerTemplateKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "flickering_step",
        "void_palm",
        "chi_bullet",
        "star_cross"
    };

    private DateTime? LastObservedSkillUse;

    /// <inheritdoc />
    public OneWithTheWindScript(Aisling subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (Subject.UserStatSheet.BaseClass != BaseClass.MartialArtist)
        {
            LastObservedSkillUse = null;

            return;
        }

        if (!Subject.SkillBook.TryGetObjectByTemplateKey("one_with_the_wind", out _))
            return;

        var lastSkillUse = Subject.Trackers.LastSkillUse;
        var castDetected = lastSkillUse.HasValue && (lastSkillUse != LastObservedSkillUse);

        if (!castDetected)
            return;

        LastObservedSkillUse = lastSkillUse;

        var usedSkillKey = Subject.Trackers.LastUsedSkill?.Template.TemplateKey;

        if ((usedSkillKey == null) || !TriggerTemplateKeys.Contains(usedSkillKey))
            return;

        Subject.Effects.Terminate("One with the Wind");
        Subject.Effects.Apply(Subject, new OneWithTheWindEffect(), this);
    }
}
