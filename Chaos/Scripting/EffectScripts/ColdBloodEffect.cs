#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Applied by Cold Blood. Instantly fills the subject's MP to maximum and holds it there for the duration,
///     maximizing Severance potential, then releases MP back to normal flow on termination.
/// </summary>
public sealed class ColdBloodEffect : EffectBase
{
    private static readonly Animation ColdAnimation = new()
    {
        TargetAnimation = 70,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(15000);

    /// <inheritdoc />
    public override byte Icon => 47;

    /// <inheritdoc />
    public override string Name => "Cold Blood";

    /// <inheritdoc />
    public override void OnApplied()
    {
        FillToMax();
        Subject.Animate(ColdAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        base.Update(delta);

        if (!Subject.IsAlive)
            return;

        FillToMax();
    }

    private void FillToMax()
    {
        var maxMp = Convert.ToInt32(Subject.StatSheet.EffectiveMaximumMp);

        if (Subject.StatSheet.CurrentMp == maxMp)
            return;

        Subject.StatSheet.SetMp(maxMp);

        if (Subject is Aisling aisling)
            aisling.Client.SendAttributes(StatUpdateType.Vitality);
    }
}
