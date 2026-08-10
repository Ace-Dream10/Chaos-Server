#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Chooser of the Slain (Valkyrie passive) - "empowers you" on a killing blow. A brief flat damage buff, the
///     same simple shape <see cref="BifrostBlessingEffect" /> uses for its own, unrelated "movement speed buff"
///     stand-in - kept as a separate dedicated class rather than reusing that one directly, matching this
///     codebase's per-ability-dedicated-effect convention (see e.g. <see cref="WingsOfStaciaShieldEffect" /> vs
///     <see cref="DivineInterventionShieldEffect" />, identical shape, still separate classes).
/// </summary>
public sealed class ChooserOfTheSlainEffect : EffectBase
{
    private const int DamageBonus = 20;

    private static readonly Animation EmpowerAnimation = new()
    {
        TargetAnimation = 70,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(8);

    /// <inheritdoc />
    public override byte Icon => 66;

    /// <inheritdoc />
    public override string Name => "Chooser of the Slain";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.StatSheet.AddBonus(new Attributes { Dmg = DamageBonus });
        Subject.Animate(EmpowerAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.StatSheet.SubtractBonus(new Attributes { Dmg = DamageBonus });
}
