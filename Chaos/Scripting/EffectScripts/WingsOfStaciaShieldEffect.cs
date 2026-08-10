#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Wings of Stacia (Valkyrie passive) - falling below a health threshold grants a divine shield, on a
///     cooldown. Same absorb-shield shape as <see cref="DivineInterventionShieldEffect" /> (own dedicated counter
///     key, not shared - see that effect's doc comment for why dedicated-per-ability keys are used throughout
///     even without a real collision risk), applied automatically by the stateless low-HP check in
///     <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" /> rather than a
///     learnable skill.
/// </summary>
public sealed class WingsOfStaciaShieldEffect : EffectBase
{
    public const string ShieldCounter = "wingsOfStaciaShield";

    private static readonly Animation FormAnimation = new()
    {
        TargetAnimation = 70,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(10);

    /// <inheritdoc />
    public override byte Icon => 65;

    /// <inheritdoc />
    public override string Name => "Wings of Stacia";

    /// <summary>
    ///     The amount of damage the shield can absorb
    /// </summary>
    public int ShieldAmount { get; set; } = 80;

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Counters.Set(ShieldCounter, ShieldAmount);
        Subject.Animate(FormAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Counters.Remove(ShieldCounter, out _);
}
