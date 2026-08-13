#region
using Chaos.DarkAges.Definitions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Reworked to match the locked design's actual description: "mark an enemy - if they die before the mark
///     expires, nearby allies are healed." The original implementation was a delayed-detonation damage nuke
///     instead (fired on the mark's own timer, not on the target's death) - a genuine mechanical mismatch found
///     during Assassin's investigation pass, not just a naming gap like Valkyrie's Chooser of the Slain. Now a
///     simple presence-tag mark (same shape as <see cref="MarkedForValhallaEffect" />); the actual heal fires from
///     <see cref="TriggerDeathMarkHeal" />, called from
///     <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" /> at the moment a marked
///     monster dies, before OnDeath removes it from the map - checks for death from ANY source while marked (same
///     convention as Black Lotus's chain explosion), not specifically a killing blow from the marking Assassin,
///     since the locked description doesn't require that. "Nearby" is centered on the death point.
///     <see cref="HealAmount" />/<see cref="HealRadius" /> are set per-tier by
///     <see cref="Chaos.Scripting.SkillScripts.DeathMarkScript" /> before applying (same "properties set by the
///     skill script, not scriptVars" convention <see cref="BloodlustEffect" /> established) - all placeholder
///     magnitudes, not balance-tested.
/// </summary>
public sealed class DeathMarkEffect : EffectBase
{
    public const string MarkTag = "deathMark";

    /// <summary>
    ///     How often the mark visual is re-played while the effect is active - same refresh-tick pattern
    ///     <see cref="StasisEffect" /> already established for "needs an ongoing, not just apply-once, visual".
    /// </summary>
    private static readonly TimeSpan AnimationRefreshInterval = TimeSpan.FromMilliseconds(1500);

    private static readonly Animation MarkAnimation = new()
    {
        TargetAnimation = 374,
        AnimationSpeed = 100
    };

    private TimeSpan SinceLastAnimation;

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(8000);

    /// <summary>
    ///     Tier IV only: grants <see cref="DeathMarkFervorEffect" /> to the marking Assassin after a successful
    ///     collect
    /// </summary>
    public bool GrantsFervorOnKill { get; init; }

    /// <summary>
    ///     Flat HP restored to each nearby ally when the mark is collected - tier-scaled ("stronger healing" at
    ///     Tier II)
    /// </summary>
    public int HealAmount { get; init; } = 40;

    /// <summary>
    ///     The radius, in tiles from the death point, within which allies are healed - tier-scaled ("larger healing
    ///     radius" at Tier III)
    /// </summary>
    public int HealRadius { get; init; } = 3;

    /// <inheritdoc />
    public override byte Icon => 56;

    /// <inheritdoc />
    public override string Name => "Death Mark";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[MarkTag] = bool.TrueString;
        Subject.Animate(MarkAnimation, Source.Id);
        SinceLastAnimation = TimeSpan.Zero;
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(MarkTag, out _);

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        base.Update(delta);

        if (!Subject.IsAlive)
            return;

        SinceLastAnimation += delta;

        if (SinceLastAnimation < AnimationRefreshInterval)
            return;

        SinceLastAnimation = TimeSpan.Zero;
        Subject.Animate(MarkAnimation, Source.Id);
    }

    /// <summary>
    ///     Called from the damage pipeline the instant a Death-Marked monster dies. Looks up the live effect
    ///     instance attached to it (so the heal scales off its own tier) and pays out the heal.
    /// </summary>
    public static void TriggerDeathMarkHeal(Monster monster)
    {
        if (!monster.Effects.TryGetEffect("Death Mark", out var rawEffect) || (rawEffect is not DeathMarkEffect effect))
            return;

        effect.HealNearbyAllies(monster);
    }

    private void HealNearbyAllies(Monster monster)
    {
        if (Source is not Aisling caster)
            return;

        var map = monster.MapInstance;
        var deathPoint = Point.From(monster);

        foreach (var nearby in map.GetEntitiesWithinRange<Aisling>(deathPoint, HealRadius))
        {
            var isAlly = nearby.Equals(caster) || (caster.Group?.Any(member => member.Equals(nearby)) ?? false);

            if (!isAlly)
                continue;

            nearby.StatSheet.AddHp(HealAmount);
            nearby.Client.SendAttributes(StatUpdateType.Vitality);
            nearby.ShowHealth();
        }

        if (GrantsFervorOnKill)
            caster.Effects.Apply(caster, new DeathMarkFervorEffect(), SourceScript);
    }
}
