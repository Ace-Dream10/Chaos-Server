#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Tags the target so it takes 25% increased damage from all sources - the multiplier itself is applied
///     directly in <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" />, since that
///     has to happen before damage is applied, not after the fact like a snapshot-scan effect could manage.
/// </summary>
public sealed class BoilingBloodEffect : EffectBase
{
    public const string BoilingBloodTag = "boiling_blood";

    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 50,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(10000);

    /// <inheritdoc />
    public override byte Icon => 66;

    /// <inheritdoc />
    public override string Name => "Boiling Blood";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[BoilingBloodTag] = bool.TrueString;
        Subject.Animate(ApplyAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(BoilingBloodTag, out _);
}
