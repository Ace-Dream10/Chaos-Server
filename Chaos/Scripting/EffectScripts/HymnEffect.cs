#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Boosts skill and spell damage - applied to every friendly Aisling on the map by
///     <see cref="Chaos.Scripting.SpellScripts.StaciasHymnScript" /> when Stacia's Hymn is cast.
/// </summary>
public sealed class HymnEffect : EffectBase
{
    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(30000);

    /// <summary>
    ///     Set by the applying script before Apply() is called, since EffectFactory.Create does not populate
    ///     scriptVars onto effects the way Scripts do.
    /// </summary>
    public int FlatDamageBonus { get; set; } = 15;

    public int FlatSpellBonus { get; set; } = 15;

    /// <inheritdoc />
    public override byte Icon => 61;

    /// <inheritdoc />
    public override string Name => "Stacia's Hymn";

    /// <inheritdoc />
    public override void OnApplied()
        => Subject.StatSheet.AddBonus(
            new Attributes
            {
                FlatSkillDamage = FlatDamageBonus,
                FlatSpellDamage = FlatSpellBonus
            });

    /// <inheritdoc />
    public override void OnTerminated()
        => Subject.StatSheet.SubtractBonus(
            new Attributes
            {
                FlatSkillDamage = FlatDamageBonus,
                FlatSpellDamage = FlatSpellBonus
            });
}
