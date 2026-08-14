#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
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
/// <remarks>
///     Also implements the second half of Divine Fury (the passive this script's resource half is named after):
///     "the more Fury you possess, the stronger your Holy abilities become." Scope decision, flagged rather than
///     guessed silently: the locked design doesn't specify which of Valkyrie's 11 actives count as "Holy" (none
///     are named/flavored as elemental Holy attacks the way Sorcerer abilities carry an explicit element), so this
///     applies the bonus to ALL skill damage while Fury is held, matching how Berserker's Rage (the closest
///     existing precedent - <see cref="BerserkerRageScript.SyncDamageBonus" />) scales ALL skill damage from its
///     own resource without an element restriction either.
/// </remarks>
public class ValkyrieFuryScript : AislingScriptBase
{
    private const int FuryPerSkillUse = 10;
    private const int IdleDrainAmount = 5;
    private const int HardCap = 100;
    private const decimal FuryDamageBonusPerMp = 0.4m;
    private static readonly TimeSpan IdleDrainInterval = TimeSpan.FromSeconds(2);

    private DateTime? LastObservedSkillUse;
    private int LastAppliedDamageBonus;
    private TimeSpan SinceLastSkillUse = TimeSpan.Zero;

    /// <inheritdoc />
    public ValkyrieFuryScript(Aisling subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        //the locked design's own description of Divine Fury is "assails and abilities generate Fury. The more
        //Fury you possess, the stronger your Holy abilities become" - that's this entire script, not just the
        //damage-bonus half, so the whole thing requires the passive to actually be learned rather than gating
        //only on BaseClass (same real-passive-gating fix applied to Valkyrie's other 3 passives). Safe in
        //practice since Divine Fury is granted on Floor 1 alongside Ragnarok itself per the locked floor
        //schedule, so a Valkyrie who can cast Ragnarok always already has Divine Fury too.
        if ((Subject.UserStatSheet.BaseClass != BaseClass.Valkyrie) || !Subject.SkillBook.ContainsByTemplateKey("divine_fury"))
        {
            if (LastAppliedDamageBonus != 0)
                RemoveDamageBonus();

            return;
        }

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
        } else
        {
            SinceLastSkillUse += delta;

            if (SinceLastSkillUse >= IdleDrainInterval)
            {
                SinceLastSkillUse = TimeSpan.Zero;

                Subject.StatSheet.SubtractMp(IdleDrainAmount);
                Subject.Client.SendAttributes(StatUpdateType.Vitality);
            }
        }

        SyncDamageBonus();
    }

    /// <summary>
    ///     Keeps the flat skill damage bonus in sync with current Fury, mirroring
    ///     <see cref="BerserkerRageScript.SyncDamageBonus" />'s exact shape
    /// </summary>
    private void SyncDamageBonus()
    {
        var desiredBonus = Convert.ToInt32(Subject.StatSheet.CurrentMp * FuryDamageBonusPerMp);

        if (desiredBonus == LastAppliedDamageBonus)
            return;

        if (LastAppliedDamageBonus != 0)
            Subject.StatSheet.SubtractBonus(new Attributes { FlatSkillDamage = LastAppliedDamageBonus });

        if (desiredBonus != 0)
            Subject.StatSheet.AddBonus(new Attributes { FlatSkillDamage = desiredBonus });

        LastAppliedDamageBonus = desiredBonus;
    }

    private void RemoveDamageBonus()
    {
        Subject.StatSheet.SubtractBonus(new Attributes { FlatSkillDamage = LastAppliedDamageBonus });
        LastAppliedDamageBonus = 0;
    }
}
