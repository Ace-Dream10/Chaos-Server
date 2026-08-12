#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.World;
using Chaos.Scripting.AislingScripts.Abstractions;
#endregion

namespace Chaos.Scripting.AislingScripts;

/// <summary>
///     Perfect Rhythm (Tempest passive) - a true always-on passive: "every few abilities restore Mana while
///     transformed." Same LastSkillUse-diff-detection pattern as FlowingChiScript, but counting EVERY ability use
///     (not just varied ones) while Martial Form is active, restoring a flat chunk of Mana every
///     <see cref="AbilitiesPerRestore" /> uses. The counter itself keeps accumulating across Martial Form
///     activations rather than resetting when the form drops - only the RESTORE is gated on currently being
///     transformed, so dropping form mid-streak and re-transforming later doesn't lose progress. Placeholder
///     values, not balance-tested.
/// </summary>
public class PerfectRhythmScript : AislingScriptBase
{
    private const int AbilitiesPerRestore = 4;
    private const int MpRestoreAmount = 40;

    private int AbilitiesUsedSinceRestore;
    private DateTime? LastObservedSkillUse;

    /// <inheritdoc />
    public PerfectRhythmScript(Aisling subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (Subject.UserStatSheet.BaseClass != BaseClass.MartialArtist)
        {
            LastObservedSkillUse = null;
            AbilitiesUsedSinceRestore = 0;

            return;
        }

        if (!Subject.SkillBook.TryGetObjectByTemplateKey("perfect_rhythm", out _))
            return;

        var lastSkillUse = Subject.Trackers.LastSkillUse;
        var castDetected = lastSkillUse.HasValue && (lastSkillUse != LastObservedSkillUse);

        if (!castDetected)
            return;

        LastObservedSkillUse = lastSkillUse;

        if (!Subject.Effects.Contains("Martial Form"))
            return;

        AbilitiesUsedSinceRestore++;

        if (AbilitiesUsedSinceRestore < AbilitiesPerRestore)
            return;

        AbilitiesUsedSinceRestore = 0;
        Subject.StatSheet.AddMp(MpRestoreAmount);
        Subject.Client.SendAttributes(StatUpdateType.Vitality);
    }
}
