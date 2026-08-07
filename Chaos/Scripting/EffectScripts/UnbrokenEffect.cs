#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Applied by Unbroken. Renamed/repurposed directly from the former "Axe Block" ability (same mechanic,
///     unchanged) to fill Berserker's locked-design Unbroken passive slot - see ELYSIUM_CLASS_DESIGN.md. Unlike
///     <see cref="StasisEffect" />, this is applied to the CASTER, not an enemy. A raw AC bonus is not viable here -
///     Aisling AC is clamped to <c>WorldOptions.MinimumAislingAc</c> (-90 by default), which only yields a ~90%
///     damage reduction, not the full block this is meant to grant. Instead this sets <see cref="BlockingTag" />,
///     checked directly in <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" />,
///     which negates incoming damage entirely while the tag is present - the same pattern used by Counter
///     Strike/Phoenix Rise.
/// </summary>
/// <remarks>
///     Design note: the locked design lists Unbroken among Berserker's 3 Passives, but this remains a
///     manually-activated, cooldown-gated effect (identical to the original Axe Block) rather than a true
///     always-on passive or an automatic low-HP proc - the design doc doesn't specify a trigger condition for a
///     passive version, and redesigning the trigger wasn't part of what was asked ("same script, new name/flavor
///     text"). Flagging this classification mismatch rather than silently resolving it.
/// </remarks>
public sealed class UnbrokenEffect : EffectBase
{
    public const string BlockingTag = "unbroken_active";

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
    public override string Name => "Unbroken";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[BlockingTag] = bool.TrueString;
        Subject.Animate(BlockAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(BlockingTag, out _);
}
