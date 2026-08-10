#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Absorbs incoming damage until either the duration elapses or the shield is fully depleted, whichever comes
///     first. Same shield-pool-in-a-counter shape as <see cref="StaciasBubbleEffect" />/
///     <see cref="FireShieldEffect" /> (absorb logic lives in
///     <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" />, the only point damage
///     is actually applied), given its own dedicated counter key rather than reusing Stacia's Bubble's - distinct
///     abilities keep independently-tracked state even though there's no actual cross-class collision risk (a
///     character is only ever one class). No eruption on break, unlike Fire Shield - Divine Intervention is pure
///     protection, not also an offensive payoff.
/// </summary>
public sealed class DivineInterventionShieldEffect : EffectBase
{
    public const string ShieldCounter = "divineInterventionShield";

    private static readonly Animation FormAnimation = new()
    {
        TargetAnimation = 70,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(20);

    /// <inheritdoc />
    public override byte Icon => 64;

    /// <inheritdoc />
    public override string Name => "Divine Intervention";

    /// <summary>
    ///     The amount of damage the shield can absorb. Set by the applying script before Apply() is called, the
    ///     same way <see cref="StaciasBubbleEffect.ShieldAmount" /> is set.
    /// </summary>
    public int ShieldAmount { get; set; } = 100;

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Counters.Set(ShieldCounter, ShieldAmount);
        Subject.Animate(FormAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Counters.Remove(ShieldCounter, out _);
}
