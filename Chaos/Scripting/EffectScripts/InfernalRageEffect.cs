#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     A self-buff boosting flat spell damage for the duration - all spells (Fire included) hit harder while active.
/// </summary>
public sealed class InfernalRageEffect : EffectBase
{
    private const int FlatSpellDamageBonus = 50;

    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 50,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(15000);

    /// <inheritdoc />
    public override byte Icon => 61;

    /// <inheritdoc />
    public override string Name => "Infernal Rage";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.StatSheet.AddBonus(new Attributes { FlatSpellDamage = FlatSpellDamageBonus });
        Subject.Animate(ApplyAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.StatSheet.SubtractBonus(new Attributes { FlatSpellDamage = FlatSpellDamageBonus });
}
