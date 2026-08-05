#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.World;
using Chaos.Scripting.AislingScripts.Abstractions;
#endregion

namespace Chaos.Scripting.AislingScripts;

/// <summary>
///     For Archers, passively regenerates MP every second based on Dex and Wis (gear-inclusive, via the effective
///     stats). No decay. Archer skills spend this MP directly in their own OnUse checks.
/// </summary>
public class ArcherResourceScript : AislingScriptBase
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);

    private TimeSpan SinceLastTick = TimeSpan.Zero;

    /// <inheritdoc />
    public ArcherResourceScript(Aisling subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (Subject.UserStatSheet.BaseClass != BaseClass.Archer)
            return;

        SinceLastTick += delta;

        if (SinceLastTick < TickInterval)
            return;

        SinceLastTick = TimeSpan.Zero;

        var mpPerSecond = Convert.ToInt32((Subject.StatSheet.EffectiveDex * 0.1m) + (Subject.StatSheet.EffectiveWis * 0.1m));

        if (mpPerSecond <= 0)
            return;

        Subject.StatSheet.AddMp(mpPerSecond);
        Subject.Client.SendAttributes(StatUpdateType.Vitality);
    }
}
