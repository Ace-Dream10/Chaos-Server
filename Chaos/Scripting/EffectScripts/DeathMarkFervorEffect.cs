#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Death Mark's Tier IV bonus: a short flat-damage buff granted after successfully collecting on a marked kill
///     (see <see cref="DeathMarkEffect" />'s doc comment). Placeholder magnitude/duration, not balance-tested.
/// </summary>
public sealed class DeathMarkFervorEffect : EffectBase
{
    private const int FlatDamageBonus = 25;

    private static readonly Animation AuraAnimation = new()
    {
        TargetAnimation = 374,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(8);

    /// <inheritdoc />
    public override byte Icon => 56;

    /// <inheritdoc />
    public override string Name => "Death Mark Fervor";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.StatSheet.AddBonus(new Attributes { FlatSkillDamage = FlatDamageBonus });
        Subject.Animate(AuraAnimation, Subject.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.StatSheet.SubtractBonus(new Attributes { FlatSkillDamage = FlatDamageBonus });
}
