#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Scripting.AislingScripts.Abstractions;
#endregion

namespace Chaos.Scripting.AislingScripts;

/// <summary>
///     For Berserkers, tracks rage (stored as MP): landing hits on monsters builds rage, and rage drains away if the
///     Berserker goes too long without hitting something. Rage passively boosts skill damage, and a visible aura
///     intensifies as rage climbs. Hard-capped at <see cref="HardCap" /> regardless of the character's real max mp
///     (set directly via SetMp, bypassing the normal AddMp/EffectiveMaximumMp clamp entirely).
/// </summary>
public class BerserkerRageScript : AislingScriptBase
{
    /// <summary>
    ///     Set by <see cref="Chaos.Scripting.SkillScripts.CycloneScript" /> while its hit sequence is in progress, to
    ///     lock the Berserker's movement/turning until the channel completes.
    /// </summary>
    public const string CyclingTag = "cycloning";

    private const int RagePerHit = 10;
    private const int IdleDrainAmount = 5;
    private const int HardCap = 100;
    private const int MediumAuraThreshold = 50;
    private const int MaxAuraThreshold = 90;
    private const decimal RageDamageBonusPerMp = 0.5m;
    private static readonly TimeSpan IdleDrainInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan AuraPulseInterval = TimeSpan.FromMilliseconds(1500);

    private static readonly Animation MediumAura = new()
    {
        TargetAnimation = 368,
        AnimationSpeed = 100
    };

    private static readonly Animation MaxAura = new()
    {
        TargetAnimation = 402,
        AnimationSpeed = 100
    };

    private int LastAppliedDamageBonus;
    private DateTime? LastObservedDamageTime;
    private TimeSpan SinceLastAuraPulse = TimeSpan.Zero;
    private TimeSpan SinceLastHit = TimeSpan.Zero;

    /// <inheritdoc />
    public BerserkerRageScript(Aisling subject)
        : base(subject) { }

    /// <inheritdoc />
    public override bool CanMove() => !Subject.Trackers.Tags.ContainsKey(CyclingTag);

    /// <inheritdoc />
    public override bool CanTurn() => !Subject.Trackers.Tags.ContainsKey(CyclingTag);

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (Subject.UserStatSheet.AdvClass != AdvClass.Berserker)
            return;

        var lastDamageTime = Subject.Trackers.LastDamagedEnemy;

        if (lastDamageTime.HasValue && (lastDamageTime != LastObservedDamageTime))
        {
            LastObservedDamageTime = lastDamageTime;
            SinceLastHit = TimeSpan.Zero;

            Subject.StatSheet.SetMp(Math.Min(Subject.StatSheet.CurrentMp + RagePerHit, HardCap));
            Subject.Client.SendAttributes(StatUpdateType.Vitality);
        } else
        {
            SinceLastHit += delta;

            if (SinceLastHit >= IdleDrainInterval)
            {
                SinceLastHit = TimeSpan.Zero;

                Subject.StatSheet.SubtractMp(IdleDrainAmount);
                Subject.Client.SendAttributes(StatUpdateType.Vitality);
            }
        }

        SyncDamageBonus();
        UpdateAuraVisual(delta);
    }

    /// <summary>
    ///     Keeps the flat skill damage bonus in sync with current rage, so every hit benefits without needing to hook
    ///     the damage pipeline directly
    /// </summary>
    private void SyncDamageBonus()
    {
        var currentMp = Subject.StatSheet.CurrentMp;
        var desiredBonus = Convert.ToInt32(currentMp * RageDamageBonusPerMp);

        if (desiredBonus == LastAppliedDamageBonus)
            return;

        if (LastAppliedDamageBonus != 0)
            Subject.StatSheet.SubtractBonus(new Attributes { FlatSkillDamage = LastAppliedDamageBonus });

        if (desiredBonus != 0)
            Subject.StatSheet.AddBonus(new Attributes { FlatSkillDamage = desiredBonus });

        LastAppliedDamageBonus = desiredBonus;
    }

    private void UpdateAuraVisual(TimeSpan delta)
    {
        SinceLastAuraPulse += delta;

        if (SinceLastAuraPulse < AuraPulseInterval)
            return;

        SinceLastAuraPulse = TimeSpan.Zero;

        var currentMp = Subject.StatSheet.CurrentMp;

        if (currentMp >= MaxAuraThreshold)
            Subject.Animate(MaxAura, Subject.Id);
        else if (currentMp >= MediumAuraThreshold)
            Subject.Animate(MediumAura, Subject.Id);
    }
}
