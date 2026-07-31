#region
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.MonsterScripts;
using Chaos.Scripting.SkillScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Summons a dark decoy that pulls aggro from nearby monsters (same pattern as Mirror Image) and actively
///     attacks nearby hostile monsters on its own via <see cref="Chaos.Scripting.MonsterScripts.ShadowCloneAggroScript" />
///     + the standard attacking/facing monster scripts.
/// </summary>
public class ShadowCloneScript : ConfigurableSkillScriptBase
{
    private readonly IMonsterFactory MonsterFactory;

    /// <inheritdoc />
    public ShadowCloneScript(Skill subject, IMonsterFactory monsterFactory)
        : base(subject)
        => MonsterFactory = monsterFactory;

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        if (!TryFindSpawnPoint(context, out var spawnPoint))
            return;

        var clone = MonsterFactory.Create(CloneTemplateKey, map, spawnPoint);
        map.AddEntity(clone, spawnPoint);

        if (clone.Script.As<DecoyExpirationScript>() is { } expirationScript)
            expirationScript.DurationMs = DurationMs;

        //force every nearby monster to switch aggro to the clone
        foreach (var monster in map.GetEntitiesWithinRange<Monster>(context.SourcePoint, AggroRange))
            monster.AggroList.AddAggro(clone, 99999);

        if (Animation != null)
            source.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, spawnPoint);
    }

    /// <summary>
    ///     Finds the closest walkable point to the source, spiraling outward, so a spawn point is always found
    ///     regardless of walls or creatures immediately surrounding the caster
    /// </summary>
    private static bool TryFindSpawnPoint(ActivationContext context, out Point spawnPoint)
    {
        var source = context.Source;
        var map = context.TargetMap;

        foreach (var point in Point.From(source)
                                   .SpiralSearch())
        {
            //skip the caster's own tile, which SpiralSearch yields first
            if (point == Point.From(source))
                continue;

            if (map.IsWalkable(point, source, false))
            {
                spawnPoint = point;

                return true;
            }
        }

        spawnPoint = default;

        return false;
    }

    #region ScriptVars
    /// <summary>
    ///     The range, in tiles, within which nearby monsters will have their aggro forced onto the clone
    /// </summary>
    public int AggroRange { get; init; }

    /// <summary>
    ///     The animation played on the caster when the skill is used
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The monster template key used for the clone
    /// </summary>
    public string CloneTemplateKey { get; init; } = "shadow_clone";

    /// <summary>
    ///     The number of milliseconds the clone will exist before disappearing
    /// </summary>
    public int DurationMs { get; init; } = 5000;

    /// <summary>
    ///     The sound played at the spawn point when the clone is summoned
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
