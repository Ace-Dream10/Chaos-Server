#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     One of Tempest's 7 specialization actives, and one of its 3 evolving abilities - "increase attack speed,
///     cast speed, and movement speed" per the locked design, evolving into a "self-buff that increasingly
///     enhances your combat flow." Scoped to <see cref="AtkSpeedPct" /> alone - this engine has no distinct cast-
///     speed or movement-speed stat (confirmed by checking Attributes' full field list), so all three named speeds
///     collapse onto the one speed stat that actually exists, same scoping approach as every other "the design
///     wants a stat this engine doesn't model" case handled tonight (Elemental Defense, Bedrock).
/// </summary>
public sealed class TempestFocusEffect : EffectBase
{
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     The bonus applied to AtkSpeedPct while active
    /// </summary>
    public int AtkSpeedBonus { get; set; }

    public override byte Icon => 58;
    public override string Name => "Tempest Focus";

    public override void OnApplied() => Subject.StatSheet.AddBonus(new Attributes { AtkSpeedPct = AtkSpeedBonus });

    public override void OnTerminated() => Subject.StatSheet.SubtractBonus(new Attributes { AtkSpeedPct = AtkSpeedBonus });
}
