#region
using Chaos.Collections;
using Chaos.DarkAges.Definitions;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Shared helper for Slayer skills that apply Severance stacks to a single target (currently
///     <see cref="CruelThrustScript" />). The caster's MP bar doubles as a visual indicator of stacks on their
///     current Severance target: 0 stacks = 0 MP, 5 stacks (max) = 100 MP. Switching targets clears the old
///     target's stacks and resets MP, since stacks are meant to be built on a single target at a time.
/// </summary>
/// <remarks>
///     Standalone - no dependency on the retired Severance Strike/Deep Cut scripts this originally served. Those
///     were removed as pre-redesign orphans; this helper survived because the mechanic it supports (Severance
///     stacking) is still very much part of the locked kit.
/// </remarks>
internal static class SeveranceTargetSync
{
    /// <summary>
    ///     If the given target differs from the caster's currently tracked Severance target, clears stacks from the
    ///     old target, resets the caster's MP to 0, and starts tracking the new target. No-op if the target is
    ///     unchanged.
    /// </summary>
    public static void SwitchTargetIfNeeded(
        Aisling? source, MapInstance map, Creature target, string severanceTargetTag, string stacksTag, string severedTag)
    {
        if (source is null)
            return;

        var targetIdStr = target.Id.ToString();

        if (source.Trackers.Tags.TryGetValue(severanceTargetTag, out var previousTargetIdStr) && (previousTargetIdStr == targetIdStr))
            return;

        if ((previousTargetIdStr is not null)
            && uint.TryParse(previousTargetIdStr, out var previousTargetId)
            && map.TryGetEntity<Creature>(previousTargetId, out var previousTarget))
        {
            previousTarget.Trackers.Tags.TryRemove(stacksTag, out _);
            previousTarget.Trackers.Tags.TryRemove(severedTag, out _);
            previousTarget.Effects.Terminate("Severance");
        }

        source.Trackers.Tags[severanceTargetTag] = targetIdStr;
        source.StatSheet.SetMp(0);
        source.Client.SendAttributes(StatUpdateType.Vitality);
    }

    /// <summary>
    ///     Sets the caster's MP to reflect the target's actual current Severance stack count (stacks x
    ///     <paramref name="mpPerStack" />), keeping the MP bar in exact sync regardless of how many stacks a given hit
    ///     added or whether stacking clamped at the max.
    /// </summary>
    public static void SyncMpToStacks(Aisling? source, Creature target, string stacksTag, int mpPerStack)
    {
        if (source is null)
            return;

        var currentStacks = 0;

        if (target.Trackers.Tags.TryGetValue(stacksTag, out var stacksStr))
            int.TryParse(stacksStr, out currentStacks);

        source.StatSheet.SetMp(currentStacks * mpPerStack);
        source.Client.SendAttributes(StatUpdateType.Vitality);
    }
}
