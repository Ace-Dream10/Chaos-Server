#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Survival Instinct (Beast passive) - "taking damage temporarily increases your damage resistance." Applied
///     to (and refreshed on) the caster by ApplyAttackDamageScript's defender-side hook whenever they take damage -
///     see that script's own comment for the true-always-on-passive trigger shape. Read back by the SAME hook via
///     <see cref="DamageReductionPctTag" /> to reduce the NEXT hit they take, same tag-based approach as Stacia's
///     Blessing.
/// </summary>
public sealed class SurvivalInstinctEffect : EffectBase
{
    /// <summary>
    ///     The Trackers.Tags key storing the percentage damage reduction while Survival Instinct is active
    /// </summary>
    public const string DamageReductionPctTag = "survivalInstinctDamageReductionPct";

    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(4);

    /// <summary>
    ///     The percentage damage reduction granted while active
    /// </summary>
    public int DamageReductionPct { get; set; } = 15;

    public override byte Icon => 65;
    public override string Name => "Survival Instinct";

    public override void OnApplied() => Subject.Trackers.Tags[DamageReductionPctTag] = DamageReductionPct.ToString();

    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(DamageReductionPctTag, out _);
}
