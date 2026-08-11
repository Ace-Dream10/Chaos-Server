#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Spiritual Attunement (Mystic passive) - after casting a healing spell, the caster's next non-healing spell
///     is empowered. Same generic-empowerment scoping decision as Bard's CrescendoEffect (a flat WIS buff active
///     during the empowered cast, rather than a bespoke "next spell does X extra" hook per spell) - see
///     SpiritualAttunementScript for exactly which cast consumes it.
/// </summary>
public sealed class SpiritualAttunementEffect : EffectBase
{
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(30);

    public override byte Icon => 56;
    public override string Name => "Spiritual Attunement";

    public override void OnApplied() => Subject.StatSheet.AddBonus(new Attributes { Wis = 15 });

    public override void OnTerminated() => Subject.StatSheet.SubtractBonus(new Attributes { Wis = 15 });
}
