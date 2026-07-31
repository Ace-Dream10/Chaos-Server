#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.World;
using Chaos.Scripting.AislingScripts.Abstractions;
#endregion

namespace Chaos.Scripting.AislingScripts;

/// <summary>
///     For Valkyries, tracks fury (stored as MP): every skill use grants fury, and fury drains away if the Valkyrie
///     goes too long without using a skill. Standing still lets fury bleed out - pure aggression is rewarded.
///     Hard-capped at <see cref="HardCap" /> regardless of the character's real max mp (set directly via SetMp,
///     bypassing the normal AddMp/EffectiveMaximumMp clamp entirely). Ragnarok itself is excluded from granting
///     fury - it's the fury dump, not a builder, and since using it counts as a skill use like any other, it would
///     otherwise immediately regenerate a chunk of the fury it just spent.
/// </summary>
public class ValkyrieFuryScript : AislingScriptBase
{
    private const int FuryPerSkillUse = 10;
    private const int IdleDrainAmount = 5;
    private const int HardCap = 100;
    private static readonly TimeSpan IdleDrainInterval = TimeSpan.FromSeconds(2);

    private DateTime? LastObservedSkillUse;
    private TimeSpan SinceLastSkillUse = TimeSpan.Zero;

    /// <inheritdoc />
    public ValkyrieFuryScript(Aisling subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (Subject.UserStatSheet.AdvClass != AdvClass.Valkyrie)
            return;

        var lastSkillUse = Subject.Trackers.LastSkillUse;

        if (lastSkillUse.HasValue && (lastSkillUse != LastObservedSkillUse))
        {
            LastObservedSkillUse = lastSkillUse;
            SinceLastSkillUse = TimeSpan.Zero;

            var isRagnarok = string.Equals(
                Subject.Trackers.LastUsedSkill?.Template.TemplateKey,
                "ragnarok",
                StringComparison.OrdinalIgnoreCase);

            if (!isRagnarok)
            {
                Subject.StatSheet.SetMp(Math.Min(Subject.StatSheet.CurrentMp + FuryPerSkillUse, HardCap));
                Subject.Client.SendAttributes(StatUpdateType.Vitality);
            }

            return;
        }

        SinceLastSkillUse += delta;

        if (SinceLastSkillUse < IdleDrainInterval)
            return;

        SinceLastSkillUse = TimeSpan.Zero;

        Subject.StatSheet.SubtractMp(IdleDrainAmount);
        Subject.Client.SendAttributes(StatUpdateType.Vitality);
    }
}
