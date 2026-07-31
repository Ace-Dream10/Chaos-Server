#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Absorbs incoming damage until either 30 seconds pass or the shield is fully depleted, whichever comes
///     first. The shield pool lives in Trackers.Counters["bubbleShield"] rather than a stat bonus, since it needs
///     to be consumed down over multiple hits rather than just reducing/redirecting each hit independently - the
///     actual absorb happens in the Aisling case of
///     <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" />, since that's the only
///     point damage is actually applied. When the counter hits 0, that same code path terminates this effect,
///     which plays the shatter animation and clears the counter.
/// </summary>
public sealed class StaciasBubbleEffect : EffectBase
{
    public const string BubbleShieldCounter = "bubbleShield";

    private static readonly Animation FormAnimation = new()
    {
        TargetAnimation = 14,
        AnimationSpeed = 100
    };

    private static readonly Animation ShatterAnimation = new()
    {
        TargetAnimation = 55,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(30000);

    /// <inheritdoc />
    public override byte Icon => 65;

    /// <inheritdoc />
    public override string Name => "Stacia's Bubble";

    /// <summary>
    ///     The amount of damage the shield can absorb. Set by the applying script before Apply() is called, since
    ///     EffectFactory.Create does not populate scriptVars onto effects the way Scripts do.
    /// </summary>
    public int ShieldAmount { get; set; } = 200;

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Counters.Set(BubbleShieldCounter, ShieldAmount);
        Subject.Animate(FormAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated()
    {
        Subject.Trackers.Counters.Remove(BubbleShieldCounter, out _);
        Subject.Animate(ShatterAnimation, Source.Id);
    }
}
