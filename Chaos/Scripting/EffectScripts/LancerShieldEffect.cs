#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     While active, redirects incoming monster damage to MP instead of HP (at <see cref="DamageToMpRate" />) - see
///     the Aisling case in <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" /> for
///     the actual redirect, since that's the only point damage is actually applied. This effect just carries the
///     configured rate and the "shieldActive" tag that gates it. Ends when
///     <see cref="Chaos.Scripting.AislingScripts.LancerShieldScript" /> detects MP has hit 0, or is cancelled
///     manually - not on a real timer, so <see cref="Duration" /> is just a long safety net.
/// </summary>
public sealed class LancerShieldEffect : EffectBase
{
    public const string ShieldActiveTag = "shieldActive";

    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 157,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(10);

    /// <inheritdoc />
    public override byte Icon => 57;

    /// <inheritdoc />
    public override string Name => "Lancer's Shield";

    /// <summary>
    ///     The multiplier applied to incoming damage to determine the MP cost while the shield is up
    /// </summary>
    public decimal DamageToMpRate { get; init; } = 2.25m;

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[ShieldActiveTag] = bool.TrueString;
        Subject.Animate(ApplyAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(ShieldActiveTag, out _);
}
