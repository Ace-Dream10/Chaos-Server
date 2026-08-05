#region
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.AislingScripts.Abstractions;
#endregion

namespace Chaos.Scripting.AislingScripts;

/// <summary>
///     Attached to every Aisling (see <see cref="Aisling" />'s default <c>ScriptKeys</c>). Currently only captures
///     the "damaged by a tracked Ascension Chamber boss" half of participation tracking -
///     <see cref="Chaos.Scripting.MonsterScripts.AscensionBossDeathScript" /> captures the "dealt damage to the
///     boss" half from the monster side. Floor-entry detection (populating
///     <c>Trackers.Counters["currentFloor"]</c>) is not implemented yet - it's gated on the floor↔map resolution
///     convention decision noted in FLOOR_TRACKER_DESIGN.md, which is still open.
/// </summary>
public class AscensionFloorTrackerScript : AislingScriptBase
{
    /// <inheritdoc />
    public AscensionFloorTrackerScript(Aisling subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnAttacked(Creature source, int damage)
    {
        if ((source is Monster boss) && boss.Trackers.Tags.ContainsKey("ascensionFloor"))
            boss.AscensionParticipants.Add(Subject);
    }
}
