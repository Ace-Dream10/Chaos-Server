#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Applied by Killing Intent. A self-buff, next-hit-consumed one-shot buff - same shape as
///     <see cref="GhostStepEffect" />'s ready tag, but a flat damage bonus on the next damaging ability rather than
///     a visibility/aggro change. Consumed by
///     <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" /> on the caster's next
///     landed hit. <see cref="BonusDamagePct" /> and the placeholder duration are not balance-tested.
/// </summary>
public sealed class KillingIntentEffect : EffectBase
{
    public const string ReadyTag = "killing_intent_ready";
    private const int BonusDamagePct = 40;

    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 133,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(8);

    /// <inheritdoc />
    public override byte Icon => 58;

    /// <inheritdoc />
    public override string Name => "Killing Intent";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[ReadyTag] = BonusDamagePct.ToString();
        Subject.Animate(ApplyAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(ReadyTag, out _);
}
