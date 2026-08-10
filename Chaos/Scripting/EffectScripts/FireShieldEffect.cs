#region
using Chaos.Extensions;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Surround yourself with a protective flame barrier - absorbs damage like
///     <see cref="StaciasBubbleEffect" /> (same shield-pool-in-a-counter shape, absorb logic lives in
///     <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" /> since that's the only
///     point damage is actually applied), but additionally erupts when the shield breaks OR its duration expires
///     - either way, not just on break - damaging and Burning nearby enemies, per the locked design ("erupts when
///     broken/expired").
/// </summary>
public sealed class FireShieldEffect : EffectBase
{
    public const string ShieldCounter = "fireShield";

    private const int EruptRange = 2;

    private static readonly Animation FormAnimation = new()
    {
        TargetAnimation = 50,
        AnimationSpeed = 100
    };

    private static readonly Animation EruptAnimation = new()
    {
        TargetAnimation = 50,
        AnimationSpeed = 100
    };

    private readonly IApplyDamageScript ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(15);

    /// <inheritdoc />
    public override byte Icon => 61;

    /// <inheritdoc />
    public override string Name => "Fire Shield";

    /// <summary>
    ///     The Burn stacks applied to each nearby enemy on eruption
    /// </summary>
    public int EruptBurnStacks { get; set; } = 2;

    /// <summary>
    ///     The damage dealt to each nearby enemy on eruption (break or expiry)
    /// </summary>
    public int EruptDamage { get; set; } = 60;

    /// <summary>
    ///     The amount of damage the shield can absorb. Set by the applying script before Apply() is called, the
    ///     same way <see cref="StaciasBubbleEffect.ShieldAmount" /> is set.
    /// </summary>
    public int ShieldAmount { get; set; } = 150;

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Counters.Set(ShieldCounter, ShieldAmount);
        Subject.Animate(FormAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated()
    {
        Subject.Trackers.Counters.Remove(ShieldCounter, out _);
        Erupt();
    }

    private void Erupt()
    {
        if (!Subject.IsAlive)
            return;

        var map = Subject.MapInstance;
        var point = Point.From(Subject);

        map.ShowAnimation(EruptAnimation.GetPointAnimation(point, Subject.Id));

        foreach (var nearby in map.GetEntitiesWithinRange<Creature>(point, EruptRange))
        {
            if ((nearby == Subject) || !nearby.IsAlive || !Subject.IsHostileTo(nearby))
                continue;

            if (EruptDamage > 0)
                ApplyDamageScript.ApplyDamage(Subject, nearby, this, EruptDamage);

            var burnEffect = new BurnEffect { StacksToApply = EruptBurnStacks };
            nearby.Effects.Apply(Subject, burnEffect, this);
        }
    }
}
