#region
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Models.World;
using Chaos.Scripting.MonsterScripts;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.AislingScripts;

/// <summary>
///     Shared illusion-spawn logic for Smoke and Mirrors (Trickster passive), extracted out of
///     <see cref="TricksterIllusionScript" /> so <see cref="Chaos.Scripting.SpellScripts.ShadowStepScript" /> can
///     call it directly. Shadow Step is special-cased with a direct hook (called from inside its own OnUse,
///     before the teleport/animation) rather than going through TricksterIllusionScript's normal one-tick-later
///     observer pattern - per playtest feedback, the decoy needs to appear BEFORE the jump, and the deferred
///     detection can only ever fire strictly after an ability (including its animation) has already executed.
///     Every other triggering ability keeps using the deferred pattern; this direct hook is Shadow Step-only.
/// </summary>
internal static class TricksterIllusionHelper
{
    private const int AggroRange = 5;
    private const int IllusionDurationMs = 1500;

    /// <summary>
    ///     Spawns the illusion near <paramref name="originPoint" /> if <paramref name="source" /> has learned
    ///     Smoke and Mirrors. No-ops otherwise.
    /// </summary>
    public static void SpawnIllusionIfLearned(Aisling source, Point originPoint, IMonsterFactory monsterFactory)
    {
        if (!source.SpellBook.TryGetObjectByTemplateKey("smoke_and_mirrors", out _))
            return;

        SpawnIllusion(source, originPoint, monsterFactory);
    }

    private static void SpawnIllusion(Aisling source, Point originPoint, IMonsterFactory monsterFactory)
    {
        var map = source.MapInstance;

        foreach (var point in originPoint.SpiralSearch())
        {
            if (point == originPoint)
                continue;

            if (!map.IsWalkable(point, source, false))
                continue;

            var illusion = monsterFactory.Create("mirror_image_decoy", map, point);
            map.AddEntity(illusion, point);

            if (illusion.Script.As<DecoyExpirationScript>() is { } expirationScript)
                expirationScript.DurationMs = IllusionDurationMs;

            //scan around the illusion's own spot (originPoint), not the caster's current position - "nearby
            //enemies" should mean nearby the decoy that's supposed to be distracting them
            foreach (var monster in map.GetEntitiesWithinRange<Monster>(originPoint, AggroRange))
                monster.AggroList.AddAggro(illusion, 99999);

            break;
        }
    }
}
