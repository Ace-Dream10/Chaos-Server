#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     One of Beast's 7 specialization actives, and one of its 3 evolving abilities - "greatly increase Attack
///     Speed" per the locked design. <see cref="AtkSpeedBonus" /> is set by PerfectFormScript before applying.
/// </summary>
public sealed class PerfectFormEffect : EffectBase
{
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     The bonus applied to AtkSpeedPct while active
    /// </summary>
    public int AtkSpeedBonus { get; set; }

    public override byte Icon => 58;
    public override string Name => "Perfect Form";

    public override void OnApplied() => Subject.StatSheet.AddBonus(new Attributes { AtkSpeedPct = AtkSpeedBonus });

    public override void OnTerminated() => Subject.StatSheet.SubtractBonus(new Attributes { AtkSpeedPct = AtkSpeedBonus });
}
