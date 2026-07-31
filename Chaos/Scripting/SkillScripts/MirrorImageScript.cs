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

public class MirrorImageScript : ConfigurableSkillScriptBase
{
    private const string DecoyTemplateKey = "mirror_image_decoy";

    private readonly IMonsterFactory MonsterFactory;

    /// <inheritdoc />
    public MirrorImageScript(Skill subject, IMonsterFactory monsterFactory)
        : base(subject)
        => MonsterFactory = monsterFactory;

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        if (!TryFindSpawnPoint(context, out var spawnPoint))
            return;

        var decoy = MonsterFactory.Create(DecoyTemplateKey, map, spawnPoint);
        map.AddEntity(decoy, spawnPoint);

        //override the decoy's default expiration duration with the one configured on this skill
        if (decoy.Script.As<DecoyExpirationScript>() is { } expirationScript)
            expirationScript.DurationMs = DurationMs;

        //force every nearby monster to switch aggro to the decoy
        foreach (var monster in map.GetEntitiesWithinRange<Monster>(context.SourcePoint, AggroRange))
            monster.AggroList.AddAggro(decoy, 99999);

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
    ///     The range, in tiles, within which nearby monsters will have their aggro forced onto the decoy
    /// </summary>
    public int AggroRange { get; init; }

    /// <summary>
    ///     The animation played on the caster when the skill is used
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The number of milliseconds the decoy will exist before disappearing
    /// </summary>
    public int DurationMs { get; init; } = 2500;

    /// <summary>
    ///     The sound played at the spawn point when the decoy is summoned
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
