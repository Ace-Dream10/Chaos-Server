#region
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Reduces incoming damage by a percentage - applied to every friendly Aisling on the map by
///     <see cref="Chaos.Scripting.SpellScripts.StaciasVeilScript" /> when Stacia's Veil is cast. The actual
///     reduction happens in the Aisling case of <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" />,
///     since that's the only point damage is actually applied - this effect just carries the configured percentage.
/// </summary>
public sealed class VeilEffect : EffectBase
{
    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(30000);

    /// <summary>
    ///     The percentage (0-100) that incoming damage is reduced by while this effect is active
    /// </summary>
    public int DamageReductionPct { get; init; } = 20;

    /// <inheritdoc />
    public override byte Icon => 63;

    /// <inheritdoc />
    public override string Name => "Stacia's Veil";
}
