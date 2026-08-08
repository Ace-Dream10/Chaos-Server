#region
using Chaos.DarkAges.Definitions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Scripting.AislingScripts.Abstractions;
#endregion

namespace Chaos.Scripting.AislingScripts;

/// <summary>
///     Bastion's "Guardian's Resolve" - a true always-on passive, following the same
///     no-op-unless-currently-a-Bastion/tick-in-Update() shape as <see cref="BerserkerRageScript" />. Genuinely
///     new; no prior mechanic existed for this. Periodically scans for nearby hostile monsters and grants a flat
///     AC bonus scaled to how many are close by (capped), synced the same way Rage keeps its damage bonus in sync
///     with current MP - by comparing the desired bonus to the last-applied one and only touching the StatSheet
///     bonus when it actually changes.
/// </summary>
public class GuardiansResolveScript : AislingScriptBase
{
    private const int AcBonusPerEnemy = -2;
    private const int MaxAcBonus = -20;
    private const int ScanRange = 4;
    private static readonly TimeSpan ScanInterval = TimeSpan.FromMilliseconds(500);

    private int LastAppliedAcBonus;
    private TimeSpan SinceLastScan = TimeSpan.Zero;

    /// <inheritdoc />
    public GuardiansResolveScript(Aisling subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (Subject.UserStatSheet.BaseClass != BaseClass.Bastion)
        {
            //if the Aisling somehow stopped being a Bastion mid-buff (class change, etc), make sure the bonus
            //doesn't linger
            ClearBonusIfApplied();

            return;
        }

        SinceLastScan += delta;

        if (SinceLastScan < ScanInterval)
            return;

        SinceLastScan = TimeSpan.Zero;

        var nearbyEnemyCount = Subject.MapInstance.GetEntitiesWithinRange<Monster>(Subject, ScanRange)
                                      .Count(monster => monster.IsAlive);

        var desiredBonus = Math.Max(MaxAcBonus, AcBonusPerEnemy * nearbyEnemyCount);

        if (desiredBonus == LastAppliedAcBonus)
            return;

        if (LastAppliedAcBonus != 0)
            Subject.StatSheet.SubtractBonus(new Attributes { Ac = LastAppliedAcBonus });

        if (desiredBonus != 0)
            Subject.StatSheet.AddBonus(new Attributes { Ac = desiredBonus });

        LastAppliedAcBonus = desiredBonus;
        Subject.Client.SendAttributes(StatUpdateType.Secondary);
    }

    private void ClearBonusIfApplied()
    {
        if (LastAppliedAcBonus == 0)
            return;

        Subject.StatSheet.SubtractBonus(new Attributes { Ac = LastAppliedAcBonus });
        LastAppliedAcBonus = 0;
        Subject.Client.SendAttributes(StatUpdateType.Secondary);
    }
}
