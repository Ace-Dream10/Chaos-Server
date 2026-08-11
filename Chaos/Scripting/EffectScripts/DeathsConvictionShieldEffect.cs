#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Death's Conviction's Tier IV bonus - "endgame survivability while casting". Same dedicated-counter absorb
///     shape as <see cref="WingsOfStaciaShieldEffect" />/<see cref="DivineInterventionShieldEffect" />, granted
///     immediately on cast at Tier IV only (see <see cref="Chaos.Scripting.SkillScripts.DeathsConvictionScript" />)
///     rather than being conditional on a health threshold - a brief window of protection right after paying the
///     HP sacrifice, since that's the moment of highest risk. Placeholder amount/duration, not balance-tested.
/// </summary>
public sealed class DeathsConvictionShieldEffect : EffectBase
{
    public const string ShieldCounter = "deathsConvictionShield";

    private static readonly Animation FormAnimation = new()
    {
        TargetAnimation = 133,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(4);

    /// <inheritdoc />
    public override byte Icon => 65;

    /// <inheritdoc />
    public override string Name => "Death's Conviction";

    /// <summary>
    ///     The amount of damage the shield can absorb
    /// </summary>
    public int ShieldAmount { get; set; } = 150;

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Counters.Set(ShieldCounter, ShieldAmount);
        Subject.Animate(FormAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Counters.Remove(ShieldCounter, out _);
}
