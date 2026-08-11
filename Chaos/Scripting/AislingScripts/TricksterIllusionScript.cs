#region
using Chaos.DarkAges.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Models.World;
using Chaos.Scripting.AislingScripts.Abstractions;
using Chaos.Scripting.MonsterScripts;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.AislingScripts;

/// <summary>
///     Smoke and Mirrors (Trickster passive) - a true always-on passive: "whenever you use a mobility or deception
///     ability, leave behind an illusion that briefly distracts nearby enemies." Rather than hooking into every
///     individual mobility/deception skill/spell script (Vanishing Act, Shadow Step, Switcheroo, Curtain Call,
///     Blackout, Delirium, Puppeteer, Hall of Mirrors, Deceiver's Cache), this watches
///     <see cref="Chaos.Collections.Trackers.LastUsedSkill" />/<see cref="Chaos.Collections.Trackers.LastUsedSpell" />
///     for a template-key match against <see cref="TriggeringTemplateKeys" /> - same kill-time-diff detection
///     pattern <see cref="AssassinFrenzyScript" /> established for observing an event this script doesn't own.
///     Spawns a short-lived <c>mirror_image_decoy</c> (the same passive-lure monster Hall of Mirrors' base tier
///     uses) to pull nearby aggro. Placeholder duration/range, not balance-tested.
/// </summary>
public class TricksterIllusionScript : AislingScriptBase
{
    private const int AggroRange = 5;
    private const int IllusionDurationMs = 1500;

    private static readonly HashSet<string> TriggeringTemplateKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "vanishing_act",
        "shadow_step",
        "switcheroo",
        "curtain_call",
        "blackout",
        "delirium",
        "puppeteer",
        "mirror_image",
        "deceivers_cache"
    };

    private readonly IMonsterFactory MonsterFactory;
    private DateTime? LastObservedSkillUse;
    private DateTime? LastObservedSpellUse;

    /// <inheritdoc />
    public TricksterIllusionScript(Aisling subject, IMonsterFactory monsterFactory)
        : base(subject)
        => MonsterFactory = monsterFactory;

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (Subject.UserStatSheet.BaseClass != BaseClass.Trickster)
        {
            LastObservedSkillUse = null;
            LastObservedSpellUse = null;

            return;
        }

        if (!Subject.SkillBook.TryGetObjectByTemplateKey("smoke_and_mirrors", out _))
            return;

        var lastSkillUse = Subject.Trackers.LastSkillUse;
        var lastSpellUse = Subject.Trackers.LastSpellUse;

        var skillTriggered = lastSkillUse.HasValue
                              && (lastSkillUse != LastObservedSkillUse)
                              && (Subject.Trackers.LastUsedSkill != null)
                              && TriggeringTemplateKeys.Contains(Subject.Trackers.LastUsedSkill.Template.TemplateKey);

        var spellTriggered = lastSpellUse.HasValue
                              && (lastSpellUse != LastObservedSpellUse)
                              && (Subject.Trackers.LastUsedSpell != null)
                              && TriggeringTemplateKeys.Contains(Subject.Trackers.LastUsedSpell.Template.TemplateKey);

        LastObservedSkillUse = lastSkillUse;
        LastObservedSpellUse = lastSpellUse;

        if (skillTriggered || spellTriggered)
            SpawnIllusion();
    }

    private void SpawnIllusion()
    {
        var map = Subject.MapInstance;
        var selfPoint = Point.From(Subject);

        foreach (var point in selfPoint.SpiralSearch())
        {
            if (point == selfPoint)
                continue;

            if (!map.IsWalkable(point, Subject, false))
                continue;

            var illusion = MonsterFactory.Create("mirror_image_decoy", map, point);
            map.AddEntity(illusion, point);

            if (illusion.Script.As<DecoyExpirationScript>() is { } expirationScript)
                expirationScript.DurationMs = IllusionDurationMs;

            foreach (var monster in map.GetEntitiesWithinRange<Monster>(Subject, AggroRange))
                monster.AggroList.AddAggro(illusion, 99999);

            break;
        }
    }
}
