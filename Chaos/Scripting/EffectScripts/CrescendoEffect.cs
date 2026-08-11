#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Crescendo (Bard passive) - applied by <see cref="Chaos.Scripting.AislingScripts.CrescendoScript" /> once
///     the caster's spell-cast stack counter reaches its max. Empowers the Bard generically (bonus WIS, since
///     nearly every Bard spell scales off WIS for heal/damage/shield output) rather than needing a per-spell hook
///     for "empowers your next spell" - a scoping decision, flagged the same way Fletcher's Eagle Eye used a
///     distance proxy instead of a real projectile simulation. Breaks (terminates) on the Bard's own next spell
///     cast via <see cref="Chaos.Scripting.AislingScripts.CrescendoScript" />, not a hardcoded duration alone.
///     Placeholder magnitude/duration, not balance-tested.
/// </summary>
public sealed class CrescendoEffect : EffectBase
{
    private const int WisBonus = 15;

    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 133,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(10);

    /// <inheritdoc />
    public override byte Icon => 58;

    /// <inheritdoc />
    public override string Name => "Crescendo";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.StatSheet.AddBonus(new Attributes { Wis = WisBonus });
        Subject.Animate(ApplyAnimation, Subject.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.StatSheet.SubtractBonus(new Attributes { Wis = WisBonus });
}
