#region
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.ReactorTileScripts.Abstractions;
#endregion

namespace Chaos.Scripting.ReactorTileScripts;

/// <summary>
///     Placed by Trickster's Deceiver's Cache. Detonates the instant a hostile monster steps on it ("approaches or
///     opens it" simplified to the same exact-tile-step trigger <see cref="WebTrapReactorScript" /> uses, not a
///     wider proximity check) - deals AoE damage and applies <see cref="AfflictionsPerDetonation" /> random mental
///     afflictions (see <see cref="TricksterAfflictions" />) among the enemies caught in the blast.
///     <see cref="BlastRadius" />/<see cref="AfflictionsPerDetonation" />/<see cref="ChainTriggersNearbyCaches" />
///     are tier-scaled and set by <see cref="Chaos.Scripting.SkillScripts.DeceiversCacheScript" /> right after
///     creation, the same runtime-override pattern <see cref="Chaos.Scripting.MonsterScripts.DecoyExpirationScript" />
///     established for decoys.
/// </summary>
public class DeceiversCacheReactorScript : ConfigurableReactorTileScriptBase
{
    private readonly IApplyDamageScript ApplyDamageScript;
    private bool Detonated;
    private TimeSpan Elapsed;

    /// <inheritdoc />
    public DeceiversCacheReactorScript(ReactorTile subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <summary>
    ///     Whether detonating this cache also triggers every other Deceiver's Cache owned by the same caster within
    ///     <see cref="ChainTriggerRange" /> - Tier IV's "possibly chain-triggering nearby caches"
    /// </summary>
    public bool ChainTriggersNearbyCaches { get; set; }

    /// <summary>
    ///     How many random mental afflictions are applied (to different enemies, where possible) per detonation
    /// </summary>
    public int AfflictionsPerDetonation { get; set; } = 1;

    /// <summary>
    ///     The radius, in tiles, of the detonation
    /// </summary>
    public int BlastRadius { get; set; } = 1;

    /// <inheritdoc />
    public override void OnWalkedOn(Creature source)
    {
        if (Detonated || (Subject.Owner is not { } owner))
            return;

        if ((source is not Monster) || !source.IsAlive || !owner.IsHostileTo(source))
            return;

        Detonate();
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (Detonated)
            return;

        Elapsed += delta;

        if (Elapsed >= TimeSpan.FromMilliseconds(TrapDurationMs))
            Detonate();
    }

    private void Detonate()
    {
        if (Detonated || (Subject.Owner is not { } owner))
            return;

        Detonated = true;

        var map = Subject.MapInstance;
        var centerPoint = Point.From(Subject);

        var targets = map.GetEntitiesWithinRange<Monster>(centerPoint, BlastRadius)
                         .Where(monster => monster.IsAlive && owner.IsHostileTo(monster))
                         .ToList();

        foreach (var target in targets)
            if (BaseDamage > 0)
                ApplyDamageScript.ApplyDamage(owner, target, this, BaseDamage);

        for (var i = 0; (i < AfflictionsPerDetonation) && (targets.Count > 0); i++)
        {
            var target = targets[i % targets.Count];
            target.Effects.Apply(owner, TricksterAfflictions.CreateRandom(), this);
        }

        if (Animation != null)
            map.ShowAnimation(Animation.GetPointAnimation(centerPoint, owner.Id));

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, centerPoint);

        if (ChainTriggersNearbyCaches)
            foreach (var nearbyTile in map.GetEntities<ReactorTile>())
            {
                if (nearbyTile.Equals(Subject) || (nearbyTile.Owner is null) || !nearbyTile.Owner.Equals(owner))
                    continue;

                if ((nearbyTile.Script is DeceiversCacheReactorScript nearbyCache)
                    && !nearbyCache.Detonated
                    && (Point.From(nearbyTile).ManhattanDistanceFrom(centerPoint) <= ChainTriggerRange))
                    nearbyCache.Detonate();
            }

        Map.RemoveEntity(Subject);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played at the detonation point
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The flat damage dealt to each enemy caught in the blast
    /// </summary>
    public int BaseDamage { get; init; } = 60;

    /// <summary>
    ///     The range, in tiles, a detonation will chain-trigger other nearby caches at (Tier IV only)
    /// </summary>
    public int ChainTriggerRange { get; init; } = 4;

    /// <summary>
    ///     Sound played at the detonation point
    /// </summary>
    public byte? Sound { get; init; }

    /// <summary>
    ///     How long, in milliseconds, the cache lingers before detonating on its own if never triggered
    /// </summary>
    public int TrapDurationMs { get; init; } = 30000;
    #endregion
}
