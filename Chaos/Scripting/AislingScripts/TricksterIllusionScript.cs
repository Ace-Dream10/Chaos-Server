#region
using Chaos.DarkAges.Definitions;
using Chaos.Geometry;
using Chaos.Models.World;
using Chaos.Scripting.AislingScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.AislingScripts;

/// <summary>
///     Smoke and Mirrors (Trickster passive) - a true always-on passive: "whenever you use a mobility or deception
///     ability, leave behind an illusion that briefly distracts nearby enemies." Rather than hooking into every
///     individual mobility/deception skill/spell script (Vanishing Act, Switcheroo, Curtain Call, Puppeteer, Hall
///     of Mirrors, Deceiver's Cache), this watches
///     <see cref="Chaos.Collections.Trackers.LastUsedSkill" />/<see cref="Chaos.Collections.Trackers.LastUsedSpell" />
///     for a template-key match against <see cref="TriggeringTemplateKeys" /> - same kill-time-diff detection
///     pattern <see cref="AssassinFrenzyScript" /> established for observing an event this script doesn't own.
///     Spawns a short-lived <c>mirror_image_decoy</c> (the same passive-lure monster Hall of Mirrors' base tier
///     uses) to pull nearby aggro, via the shared <see cref="TricksterIllusionHelper" />. Placeholder duration/
///     range, not balance-tested.
/// </summary>
/// <remarks>
///     Shadow Step is deliberately NOT in <see cref="TriggeringTemplateKeys" /> - per playtest feedback, its
///     decoy needs to appear BEFORE the teleport/animation, which this one-tick-later observer pattern can never
///     achieve by construction (it can only detect an ability after it's already fully executed). Shadow Step
///     calls <see cref="TricksterIllusionHelper.SpawnIllusionIfLearned" /> directly from inside its own OnUse
///     instead - see <see cref="Chaos.Scripting.SpellScripts.ShadowStepScript" />. Every other trigger keeps using
///     this deferred pattern; only Shadow Step gets the direct hook.
/// </remarks>
public class TricksterIllusionScript : AislingScriptBase
{
    /// <summary>
    ///     Per playtest feedback, Blackout and Delirium are pure mental-affliction/CC abilities (blind, confuse-
    ///     and-redirect) - neither mobility nor deception per the locked design's own "Signature Afflictions"
    ///     categorization (Blind/Delirium/Control grouped separately from the mobility/deception cluster). They
    ///     were erroneously included here, causing a decoy to spawn on every use even though neither ability
    ///     actually involves movement or trickery - removed. Puppeteer wasn't flagged in the same report and is
    ///     left as-is rather than second-guessed.
    /// </summary>
    private static readonly HashSet<string> TriggeringTemplateKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "vanishing_act",
        "switcheroo",
        "curtain_call",
        "puppeteer",
        "mirror_image",
        "deceivers_cache"
    };

    private readonly IMonsterFactory MonsterFactory;
    private DateTime? LastObservedSkillUse;
    private DateTime? LastObservedSpellUse;

    /// <summary>
    ///     The caster's position as of the END of the previous Update tick - captured every tick regardless of
    ///     whether a trigger fires. Detection here is necessarily deferred by one tick (this script observes
    ///     Trackers.LastSkillUse/LastSpellUse changing rather than hooking each ability directly), and by the time
    ///     that change is observed, a mobility ability like Vanishing Act has already moved the caster to its
    ///     landing point. Using this stored pre-tick position instead of the caster's live current position is
    ///     what makes the illusion spawn where the caster STARTED, not where they landed - confirmed root cause
    ///     of the "spawns at the landing point" report.
    /// </summary>
    private Point? LastKnownPosition;

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
            LastKnownPosition = null;

            return;
        }

        if (!Subject.SpellBook.TryGetObjectByTemplateKey("smoke_and_mirrors", out _))
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
            TricksterIllusionHelper.SpawnIllusionIfLearned(Subject, LastKnownPosition ?? Point.From(Subject), MonsterFactory);

        LastKnownPosition = Point.From(Subject);
    }
}
