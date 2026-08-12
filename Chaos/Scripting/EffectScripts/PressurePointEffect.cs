#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     One of Ironscale's 7 specialization actives, and one of its 3 evolving abilities - "expose an enemy's weak
///     point, increasing all damage they take" per the locked design, evolving into "a full raid-support debuff."
///     Tag-based like Break/Boiling Blood - see ApplyAttackDamageScript's hook for where the tag is actually read
///     and applied (that hook is shared with Break already, both just contribute their own percentage). The "full
///     raid-support debuff" evolution is realized by PressurePointScript spreading the mark to nearby enemies at
///     the max tier, rather than a mechanic unique to this effect itself.
/// </summary>
public sealed class PressurePointEffect : EffectBase
{
    /// <summary>
    ///     The Trackers.Tags key storing the percentage bonus damage taken while Pressure Point is active
    /// </summary>
    public const string BonusDamageTakenPctTag = "pressurePointBonusDamageTakenPct";

    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(8);

    /// <summary>
    ///     The percentage bonus damage the target takes from all sources while Pressure Point is active
    /// </summary>
    public int BonusDamageTakenPct { get; set; }

    public override byte Icon => 42;
    public override string Name => "Pressure Point";

    public override void OnApplied() => Subject.Trackers.Tags[BonusDamageTakenPctTag] = BonusDamageTakenPct.ToString();

    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(BonusDamageTakenPctTag, out _);
}
