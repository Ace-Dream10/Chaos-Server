#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Applied by Axe Block. Unlike <see cref="StasisEffect" />, this is applied to the CASTER, not an enemy. A raw AC
///     bonus is not viable here - Aisling AC is clamped to <c>WorldOptions.MinimumAislingAc</c> (-90 by default), which
///     only yields a ~90% damage reduction, not the full block this skill is meant to grant. Instead this sets
///     <see cref="BlockingTag" />, checked directly in
///     <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" />, which negates incoming
///     damage entirely while the tag is present - the same pattern used by Counter Strike/Phoenix Rise.
/// </summary>
public sealed class AxeBlockEffect : EffectBase
{
    public const string BlockingTag = "axe_block_active";

    private static readonly Animation BlockAnimation = new()
    {
        TargetAnimation = 24,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(3000);

    /// <inheritdoc />
    public override byte Icon => 46;

    /// <inheritdoc />
    public override string Name => "Axe Block";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[BlockingTag] = bool.TrueString;
        Subject.Animate(BlockAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(BlockingTag, out _);
}
