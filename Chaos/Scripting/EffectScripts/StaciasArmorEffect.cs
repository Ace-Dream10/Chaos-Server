#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Lowers AC (better defense in Dark Ages) - applied to every friendly Aisling on the map by
///     <see cref="Chaos.Scripting.SpellScripts.StaciasArmorScript" /> when Stacia's Armor is cast.
/// </summary>
public sealed class StaciasArmorEffect : EffectBase
{
    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(60000);

    /// <summary>
    ///     Set by the applying script before Apply() is called, since EffectFactory.Create does not populate
    ///     scriptVars onto effects the way Scripts do. Negative values lower AC (better defense).
    /// </summary>
    public int AcBonus { get; set; } = -20;

    /// <inheritdoc />
    public override byte Icon => 64;

    /// <inheritdoc />
    public override string Name => "Stacia's Armor";

    /// <inheritdoc />
    public override void OnApplied() => Subject.StatSheet.AddBonus(new Attributes { Ac = AcBonus });

    /// <inheritdoc />
    public override void OnTerminated() => Subject.StatSheet.SubtractBonus(new Attributes { Ac = AcBonus });
}
