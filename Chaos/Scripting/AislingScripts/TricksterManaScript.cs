#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.World;
using Chaos.Scripting.AislingScripts.Abstractions;
#endregion

namespace Chaos.Scripting.AislingScripts;

/// <summary>
///     For Tricksters, passively regenerates MP every second based on Int and Dex (gear-inclusive, via the effective
///     stats). No decay - it's steady accumulation up to MaxMP, spent on Mana Burst.
/// </summary>
public class TricksterManaScript : AislingScriptBase
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);

    private TimeSpan SinceLastTick = TimeSpan.Zero;

    /// <inheritdoc />
    public TricksterManaScript(Aisling subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (Subject.UserStatSheet.BaseClass != BaseClass.Trickster)
            return;

        SinceLastTick += delta;

        if (SinceLastTick < TickInterval)
            return;

        SinceLastTick = TimeSpan.Zero;

        var mpPerSecond = Convert.ToInt32((Subject.StatSheet.EffectiveInt * 0.1m) + (Subject.StatSheet.EffectiveDex * 0.1m));

        if (mpPerSecond <= 0)
            return;

        Subject.StatSheet.AddMp(mpPerSecond);
        Subject.Client.SendAttributes(StatUpdateType.Vitality);
    }
}
