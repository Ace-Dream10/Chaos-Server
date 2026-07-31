#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
using Chaos.Time;
using Chaos.Time.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

public sealed class DragoonsWrathEffect : IntervalEffectBase
{
    private const int StrBonus = 10;
    private const int DexBonus = 5;
    private const int AtkSpeedBonus = 10;
    private const int MpPerTick = 10;

    private static readonly Attributes Bonus = new()
    {
        Str = StrBonus,
        Dex = DexBonus,
        AtkSpeedPct = AtkSpeedBonus
    };

    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 14,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(15000);

    /// <inheritdoc />
    protected override IIntervalTimer Interval { get; } = new IntervalTimer(TimeSpan.FromMilliseconds(2000), false);

    /// <inheritdoc />
    public override byte Icon => 50;

    /// <inheritdoc />
    public override string Name => "Dragoon's Wrath";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.StatSheet.AddBonus(Bonus);
        Subject.Animate(ApplyAnimation, Source.Id);
    }

    /// <inheritdoc />
    protected override void OnIntervalElapsed()
    {
        Subject.StatSheet.AddMp(MpPerTick);
        AislingSubject?.Client.SendAttributes(StatUpdateType.Vitality);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.StatSheet.SubtractBonus(Bonus);
}
