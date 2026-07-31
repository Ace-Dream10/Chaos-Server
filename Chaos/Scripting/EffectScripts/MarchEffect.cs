#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Boosts attack speed - applied to every friendly Aisling on the map by
///     <see cref="Chaos.Scripting.SpellScripts.StaciasMarchScript" /> when Stacia's March is cast.
/// </summary>
public sealed class MarchEffect : EffectBase
{
    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(30000);

    /// <summary>
    ///     Set by the applying script before Apply() is called, since EffectFactory.Create does not populate
    ///     scriptVars onto effects the way Scripts do.
    /// </summary>
    public int AtkSpeedBonus { get; set; } = 20;

    /// <inheritdoc />
    public override byte Icon => 62;

    /// <inheritdoc />
    public override string Name => "Stacia's March";

    /// <inheritdoc />
    public override void OnApplied() => Subject.StatSheet.AddBonus(new Attributes { AtkSpeedPct = AtkSpeedBonus });

    /// <inheritdoc />
    public override void OnTerminated() => Subject.StatSheet.SubtractBonus(new Attributes { AtkSpeedPct = AtkSpeedBonus });
}
