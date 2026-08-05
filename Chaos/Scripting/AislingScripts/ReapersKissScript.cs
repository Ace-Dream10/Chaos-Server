#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Scripting.AislingScripts.Abstractions;
#endregion

namespace Chaos.Scripting.AislingScripts;

/// <summary>
///     For Assassins who have learned Reaper's Kiss: each killing blow on a monster heals a flat amount plus a
///     percentage of that monster's max HP. Purely passive - reuses the same <c>Trackers.LastKillTime</c>
///     kill-detection pattern as <see cref="AssassinFrenzyScript" />, plus the max-HP snapshot
///     <c>Trackers.LastKilledMonsterMaxHp</c> taken alongside it.
/// </summary>
public class ReapersKissScript : AislingScriptBase
{
    private const int HealOnKill = 50;
    private const int HealPct = 5;

    private static readonly Animation HealAnimation = new()
    {
        TargetAnimation = 68,
        AnimationSpeed = 100
    };

    private DateTime? LastObservedKillTime;

    /// <inheritdoc />
    public ReapersKissScript(Aisling subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (Subject.UserStatSheet.BaseClass != BaseClass.Assassin)
            return;

        if (!Subject.SkillBook.TryGetObjectByTemplateKey("reapers_kiss", out _))
            return;

        var lastKillTime = Subject.Trackers.LastKillTime;

        if (!lastKillTime.HasValue || (lastKillTime == LastObservedKillTime))
            return;

        LastObservedKillTime = lastKillTime;

        var monsterMaxHp = Subject.Trackers.LastKilledMonsterMaxHp ?? 0;
        var healAmount = HealOnKill + Convert.ToInt32(monsterMaxHp * (HealPct / 100m));

        if (healAmount <= 0)
            return;

        Subject.StatSheet.AddHp(healAmount);
        Subject.Client.SendAttributes(StatUpdateType.Vitality);
        Subject.ShowHealth();
        Subject.Animate(HealAnimation, Subject.Id);
    }
}
