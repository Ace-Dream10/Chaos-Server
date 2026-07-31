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
        if (Subject.UserStatSheet.AdvClass != AdvClass.MartialArtist)
            return;

        var lastDamageTime = Subject.Trackers.LastDamagedEnemy;

        if (!lastDamageTime.HasValue || (lastDamageTime == LastObservedDamageTime))
            return;

        LastObservedDamageTime = lastDamageTime;

        //Tiger Stance doubles Chi generation while active
        var chiGain = Subject.Trackers.Tags.ContainsKey(TigerStanceEffect.TigerStanceTag) ? ChiPerHit * 2 : ChiPerHit;

        Subject.StatSheet.AddMp(chiGain);
        Subject.Client.SendAttributes(StatUpdateType.Vitality);
    }
}
