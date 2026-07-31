#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

public sealed class StaciasBlessingEffect : EffectBase
{
    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(1800000);

    /// <summary>
    ///     All of the below are set by the applying script before Apply() is called, since EffectFactory.Create does
    ///     not populate scriptVars onto effects the way Scripts do.
    /// </summary>
    public int ConBonus { get; set; } = 5;

    public int DexBonus { get; set; } = 5;
    public int HpBonus { get; set; } = 500;
    public int IntBonus { get; set; } = 5;
    public int MpBonus { get; set; } = 300;
    public int StrBonus { get; set; } = 5;
    public int WisBonus { get; set; } = 5;

    /// <inheritdoc />
    public override byte Icon => 56;

    /// <inheritdoc />
    public override string Name => "Stacia's Blessing";

    /// <inheritdoc />
    public override void OnApplied()
        => Subject.StatSheet.AddBonus(
            new Attributes
            {
                Str = StrBonus,
                Dex = DexBonus,
                Int = IntBonus,
                Wis = WisBonus,
                Con = ConBonus,
                MaximumHp = HpBonus,
                MaximumMp = MpBonus
            });

    /// <inheritdoc />
    public override void OnTerminated()
        => Subject.StatSheet.SubtractBonus(
            new Attributes
            {
                Str = StrBonus,
                Dex = DexBonus,
                Int = IntBonus,
                Wis = WisBonus,
                Con = ConBonus,
                MaximumHp = HpBonus,
                MaximumMp = MpBonus
            });
}
