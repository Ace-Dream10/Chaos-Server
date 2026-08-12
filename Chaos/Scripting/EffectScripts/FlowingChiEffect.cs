#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Flowing Chi (Tempest passive) - "using different abilities in succession empowers the next ability." Same
///     generic-empowerment scoping decision as Bard's CrescendoEffect/Mystic's SpiritualAttunementEffect (a flat
///     stat buff active during the empowered cast, rather than a bespoke "next ability does X extra" hook per
///     ability) - see FlowingChiScript for exactly which cast grants and which one is empowered.
/// </summary>
public sealed class FlowingChiEffect : EffectBase
{
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(6);

    public override byte Icon => 56;
    public override string Name => "Flowing Chi";

    public override void OnApplied() => Subject.StatSheet.AddBonus(new Attributes { FlatSkillDamage = 15, FlatSpellDamage = 15 });

    public override void OnTerminated() => Subject.StatSheet.SubtractBonus(new Attributes { FlatSkillDamage = 15, FlatSpellDamage = 15 });
}
