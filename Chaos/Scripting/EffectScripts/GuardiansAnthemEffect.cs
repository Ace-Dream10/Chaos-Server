#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Guardian's Anthem, Bard's iconic raid-saving cooldown. Reuses
///     <see cref="StaciasBulwarkEffect.InvulnerableTag" /> directly (the same tag Bastion's Stacia's Bulwark sets)
///     rather than inventing a parallel invulnerability tag - <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" />
///     already checks for it first, before any other mitigation, so no new hook is needed. Placeholder duration
///     (tier-scaled), not balance-tested.
/// </summary>
public sealed class GuardiansAnthemEffect : EffectBase
{
    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 157,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(3);

    /// <inheritdoc />
    public override byte Icon => 65;

    /// <inheritdoc />
    public override string Name => "Guardian's Anthem";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[StaciasBulwarkEffect.InvulnerableTag] = bool.TrueString;
        Subject.Animate(ApplyAnimation, Subject.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(StaciasBulwarkEffect.InvulnerableTag, out _);
}
