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
/// <remarks>
///     One of Assassin's 5 evolving abilities. Tiers per the locked design ("one clone -&gt; stronger clone -&gt;
///     two clones -&gt; clones inherit a portion of your abilities") mapped to Assassin's own floor arc (Floor4
///     intro, Floor5, Floor6, Floor7 max) via the same "Level ≈ 2×Floor" ratio used throughout tonight - see
///     <see cref="GetTierValues" />. "Stronger" is a flat HP/damage bonus applied to the clone's StatSheet after
///     spawning (the clone's own base stats come from its monster template, which this script doesn't control).
///     Tier IV's "inherit a portion of your abilities" is simplified to the same strength bonus as Tier II/III plus
///     a second clone and longer duration - actually granting clones a subset of the caster's own skills is out of
///     scope for tonight's pass and is flagged here rather than silently skipped. All placeholder values, not
///     balance-tested.
/// </remarks>
public class ShadowCloneScript : ConfigurableSkillScriptBase
{
    private const int StrongerCloneBonusDamage = 15;
    private const int StrongerCloneBonusHp = 100;
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
        var tier = GetTierValues();

        for (var i = 0; i < tier.CloneCount; i++)
        {
            if (!TryFindSpawnPoint(context, out var spawnPoint))
                continue;

            var clone = MonsterFactory.Create(CloneTemplateKey, map, spawnPoint);
            map.AddEntity(clone, spawnPoint);

            if (clone.Script.As<DecoyExpirationScript>() is { } expirationScript)
                expirationScript.DurationMs = tier.DurationMs;

            if (tier.IsStronger)
                clone.StatSheet.AddBonus(
                    new Attributes
                    {
                        MaximumHp = StrongerCloneBonusHp,
                        FlatSkillDamage = StrongerCloneBonusDamage
                    });

            //force every nearby monster to switch aggro to the clone
            foreach (var monster in map.GetEntitiesWithinRange<Monster>(context.SourcePoint, AggroRange))
                monster.AggroList.AddAggro(clone, 99999);
        }

        if (Animation != null)
            source.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, Point.From(source));
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor4(Level&lt;=8)=I(intro,1 clone), Floor5(&lt;=10)=II
    ///     (1 stronger clone), Floor6(&lt;=12)=III(2 stronger clones), Floor7+(&gt;12)=IV(max, 2 stronger clones +
    ///     longer duration; see remarks above re: the "inherit abilities" simplification).
    /// </summary>
    private (int CloneCount, bool IsStronger, int DurationMs) GetTierValues() =>
        Subject.Level switch
        {
            <= 8  => (1, false, DurationMs),
            <= 10 => (1, true, DurationMs),
            <= 12 => (2, true, DurationMs),
            _     => (2, true, DurationMs + 3000)
        };

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
