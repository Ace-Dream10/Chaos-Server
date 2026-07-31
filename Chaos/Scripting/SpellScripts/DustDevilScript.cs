#region
using Chaos.DarkAges.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.MonsterScripts;
using Chaos.Scripting.SpellScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     Spawns a traveling dust devil that chases down and attacks nearby hostile monsters on its own, reusing the
///     same monster-vs-monster targeting (<see cref="ShadowCloneAggroScript" />) built for the Assassin's Shadow
///     Clone.
/// </summary>
public class DustDevilScript : ConfigurableSpellScriptBase
{
    private readonly IMonsterFactory MonsterFactory;

    /// <inheritdoc />
    public DustDevilScript(Spell subject, IMonsterFactory monsterFactory)
        : base(subject)
        => MonsterFactory = monsterFactory;

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        if (!TryFindSpawnPoint(context, out var spawnPoint))
            return;

        var devil = MonsterFactory.Create(DevilTemplateKey, map, spawnPoint);
        map.AddEntity(devil, spawnPoint);

        if (devil.Script.Is<DecoyExpirationScript>(out var expirationScript))
            expirationScript.DurationMs = DurationMs;

        if (Animation != null)
            source.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, spawnPoint);
    }

    private static bool TryFindSpawnPoint(SpellContext context, out Point spawnPoint)
    {
        var source = context.Source;
        var map = context.TargetMap;

        foreach (var point in Point.From(source)
                                   .SpiralSearch())
        {
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
    ///     The animation played on the caster when the skill is used
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The monster template key used for the dust devil
    /// </summary>
    public string DevilTemplateKey { get; init; } = "dust_devil";

    /// <summary>
    ///     The number of milliseconds the dust devil will exist before disappearing
    /// </summary>
    public int DurationMs { get; init; } = 5000;

    /// <summary>
    ///     The sound played at the spawn point when the dust devil is summoned
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
