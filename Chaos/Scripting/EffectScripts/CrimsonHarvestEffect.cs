#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Renamed from "Bloodlust" to resolve the naming collision with Assassin's resource and Beast's passive. For
///     the duration, heals the caster for a percentage of all damage they deal - checked directly in
///     <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" /> via
///     <see cref="LifestealTag" />, the same lifesteal-tag pattern <see cref="ColdBloodEffect" /> uses. The two
///     stack additively if both happen to be active at once - not treated as a bug, just two separate lifesteal
///     sources.
/// </summary>
public sealed class CrimsonHarvestEffect : EffectBase
{
    public const string LifestealTag = "crimson_harvest_lifesteal_pct";

    private static readonly Animation HarvestAnimation = new()
    {
        TargetAnimation = 24,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(8);

    /// <inheritdoc />
    public override byte Icon => 59;

    /// <inheritdoc />
    public override string Name => "Crimson Harvest";

    /// <summary>
    ///     The lifesteal percentage granted while active - set by <see cref="Chaos.Scripting.SkillScripts.CrimsonHarvestScript" /> before
    ///     Apply() is called, the same way <see cref="BleedEffect.BleedDamage" /> is set.
    /// </summary>
    public int LifestealPct { get; set; } = 20;

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[LifestealTag] = LifestealPct.ToString();
        Subject.Animate(HarvestAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(LifestealTag, out _);
}
