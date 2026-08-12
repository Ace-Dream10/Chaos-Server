#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     One with the Wind (Tempest passive) - "mobility and chi techniques briefly increase movement speed and
///     evasion." Same speed/evasion scoping as Tempest Focus/Bedrock (this engine has no dedicated movement-speed
///     or evasion stat) - realized as AtkSpeedPct (speed proxy) and a negative Ac bonus (evasion proxy, matching
///     this engine's inverted-AC convention).
/// </summary>
public sealed class OneWithTheWindEffect : EffectBase
{
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(4);

    public override byte Icon => 65;
    public override string Name => "One with the Wind";

    public override void OnApplied() => Subject.StatSheet.AddBonus(new Attributes { AtkSpeedPct = 20, Ac = -10 });

    public override void OnTerminated() => Subject.StatSheet.SubtractBonus(new Attributes { AtkSpeedPct = 20, Ac = -10 });
}
