#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Stacia's Grace (Bard passive) - when the Bard's own Health falls below a threshold, Stacia intervenes with
///     brief 100% damage immunity, on a long cooldown. Reuses
///     <see cref="StaciasBulwarkEffect.InvulnerableTag" /> directly, same as Guardian's Anthem - applied
///     automatically by the stateless low-HP check in
///     <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" /> (same
///     "Wings of Stacia" cooldown-gated low-HP trigger shape Valkyrie's own passive used) rather than a learnable
///     skill. Placeholder duration, not balance-tested.
/// </summary>
public sealed class StaciasGraceEffect : EffectBase
{
    private static readonly Animation FormAnimation = new()
    {
        TargetAnimation = 157,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(3);

    /// <inheritdoc />
    public override byte Icon => 65;

    /// <inheritdoc />
    public override string Name => "Stacia's Grace";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[StaciasBulwarkEffect.InvulnerableTag] = bool.TrueString;
        Subject.Animate(FormAnimation, Subject.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(StaciasBulwarkEffect.InvulnerableTag, out _);
}
