#region
using Chaos.Common.Abstractions;
using Chaos.DarkAges.Definitions;
using Chaos.Models.Panel;
using Chaos.Models.World;
#endregion

namespace Chaos.Utilities;

/// <summary>
///     Shared helpers for the Martial Artist Beast Form skill-visibility system. Form-specific skills (Fenrir
///     Claw, Celestial Bolt, Basilisk Bite) are tagged via a "beastform" scriptVars entry
///     (<c>{ "beastFormOnly": true, "requiredForm": "Fenrir" }</c>) rather than a dedicated attached script, since
///     the tag is pure metadata read by <see cref="Chaos.Scripting.EffectScripts.BeastFormEffect" /> and
///     <see cref="Chaos.Scripting.DialogScripts.BecomeClassScript" /> - it doesn't need any behavior of its own.
///     These skills stay in the SkillBook at all times once granted; only their client-side action bar visibility
///     toggles, via <see cref="IChaosWorldClient.SendAddSkillToPane" />/<see cref="IChaosWorldClient.SendRemoveSkillFromPane" />.
/// </summary>
public static class BeastFormSkillHelper
{
    private const string BeastFormOnlyKey = "beastFormOnly";
    private const string BeastFormTag = "beastform";
    private const string RequiredFormKey = "requiredForm";

    /// <summary>
    ///     Whether the given skill is tagged as a Beast Form skill, regardless of which form it belongs to
    /// </summary>
    public static bool IsBeastFormSkill(Skill skill)
        => skill.Template.ScriptVars.TryGetValue(BeastFormTag, out var vars) && vars.Get<bool>(BeastFormOnlyKey);

    /// <summary>
    ///     Whether the given skill is tagged as belonging specifically to <paramref name="form" />
    /// </summary>
    public static bool MatchesForm(Skill skill, BeastFormType form)
        => skill.Template.ScriptVars.TryGetValue(BeastFormTag, out var vars)
           && vars.Get<bool>(BeastFormOnlyKey)
           && string.Equals(vars.Get<string>(RequiredFormKey), form.ToString(), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///     Hides every Beast Form skill currently in the Aisling's SkillBook from their action bar, without removing
    ///     them from the SkillBook itself. Meant to be called right after granting these skills (e.g. from
    ///     BecomeClassScript), since learning a skill normally reveals it immediately.
    /// </summary>
    public static void HideAllFormSkills(Aisling source)
    {
        foreach (var skill in source.SkillBook)
            if (IsBeastFormSkill(skill))
                source.Client.SendRemoveSkillFromPane(skill.Slot);
    }
}
