#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.World;
using Chaos.Scripting.AislingScripts.Abstractions;
using Chaos.Scripting.EffectScripts;
#endregion

namespace Chaos.Scripting.AislingScripts;

/// <summary>
///     For Martial Artists, builds Chi (stored as MP) from landing hits on monsters. Unlike Fury or Rage, Chi never
///     decays - it's banked indefinitely until spent on Beast Form. Meditate remains the out-of-combat Chi builder
///     and needs no changes here. Not hard-capped - unlike Assassin/Berserker/Valkyrie, Martial Artist Chi is meant
///     to scale with the character's real max mp (grows from leveling/gear), same as Lancer's shield charge.
/// </summary>
/// <remarks>
///     On-hit generation is fully suspended while Martial Form is active - confirmed decision after alpha
///     feedback that Chi felt too easy to sustain in Form. Was previously always-on regardless of Form state,
///     which meant landing hits while transformed (i.e. exactly when you're actively fighting) largely offset
///     MartialFormEffect's own per-second drain, making the drain barely felt. Form is meant to be a pure
///     one-way cost for now - Meditate (one of the 5 evolving abilities) is the intended answer to Chi
///     sustain/management as IT levels up through its own tiers, not something Form should provide passively on
///     its own.
/// </remarks>
public class MartialArtistChiScript : AislingScriptBase
{
    private const int ChiPerHit = 10;

    private DateTime? LastObservedDamageTime;

    /// <inheritdoc />
    public MartialArtistChiScript(Aisling subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (Subject.UserStatSheet.BaseClass != BaseClass.MartialArtist)
            return;

        //no on-hit Chi generation while Form is active - see remarks above
        if (Subject.Effects.Contains("Martial Form"))
            return;

        var lastDamageTime = Subject.Trackers.LastDamagedEnemy;

        if (!lastDamageTime.HasValue || (lastDamageTime == LastObservedDamageTime))
            return;

        LastObservedDamageTime = lastDamageTime;

        //Tiger Stance doubles Chi generation while active
        var chiGain = Subject.Trackers.Tags.ContainsKey(ValkorsFervorEffect.ValkorsFervorTag) ? ChiPerHit * 2 : ChiPerHit;

        Subject.StatSheet.AddMp(chiGain);
        Subject.Client.SendAttributes(StatUpdateType.Vitality);
    }
}
