#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     One of Beast's 7 specialization actives - "target takes increased damage for a short duration" per the
///     locked design. A direct build, tag-based like Boiling Blood/Mark of the Bane - see ApplyAttackDamageScript's
///     hook for where the tag is actually read and applied. Not one of Beast's 3 evolving abilities - flat.
/// </summary>
public sealed class BreakEffect : EffectBase
{
    /// <summary>
    ///     The Trackers.Tags key storing the percentage bonus damage taken while Break is active
    /// </summary>
    public const string BonusDamageTakenPctTag = "breakBonusDamageTakenPct";

    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(6);

    /// <summary>
    ///     The percentage bonus damage the target takes from all sources while Break is active
    /// </summary>
    public int BonusDamageTakenPct { get; set; } = 25;

    public override byte Icon => 42;
    public override string Name => "Break";

    public override void OnApplied() => Subject.Trackers.Tags[BonusDamageTakenPctTag] = BonusDamageTakenPct.ToString();

    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(BonusDamageTakenPctTag, out _);
}
