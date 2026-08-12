#region
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts.Abstractions;
using Chaos.Time;
using Chaos.Time.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     One of Ironscale's 7 specialization actives, and one of its 3 evolving abilities - "become immobile,
///     generating massive threat while storing or reflecting incoming damage" per the locked design, evolving into
///     "the Ultimate defensive cooldown." A direct build, no existing precedent for the immobile-tank-cooldown
///     shape. The caster is rooted for the duration (the ability's own cost, not a debuff - reapplied every pulse
///     so it can't be shrugged off early), pulses aggro to every nearby hostile on an interval (same
///     AggroList.AddAggro pattern Bastion's Taunt-equivalent, ChallengingShoutScript, already established), and
///     reflects a percentage of incoming damage back at the attacker via <see cref="ReflectPct" /> - read directly
///     by ApplyAttackDamageScript's defender-side mitigation chain, same tag-based approach as every other
///     percentage modifier built tonight.
/// </summary>
public sealed class IronBodyEffect : IntervalEffectBase
{
    /// <summary>
    ///     The Trackers.Tags key storing the percentage of incoming damage reflected back at the attacker while
    ///     Iron Body is active
    /// </summary>
    public const string ReflectPctTag = "ironBodyReflectPct";

    private static readonly Animation PulseAnimation = new()
    {
        TargetAnimation = 157,
        AnimationSpeed = 100
    };

    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(6);
    protected override IIntervalTimer Interval { get; } = new IntervalTimer(TimeSpan.FromSeconds(1));

    /// <summary>
    ///     The radius around the caster pulsed for threat each tick
    /// </summary>
    public int AggroRadius { get; set; } = 4;

    /// <summary>
    ///     The amount of threat added to every hostile in range on each tick
    /// </summary>
    public int AggroPerTick { get; set; } = 500;

    /// <summary>
    ///     The percentage of incoming damage reflected back at the attacker
    /// </summary>
    public int ReflectPct { get; set; }

    public override byte Icon => 65;
    public override string Name => "Iron Body";

    public override void OnApplied()
    {
        Subject.Trackers.Tags[ReflectPctTag] = ReflectPct.ToString();
        Subject.Animate(PulseAnimation, Source.Id);
        RefreshRoot();
        PulseAggro();
    }

    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(ReflectPctTag, out _);

    protected override void OnIntervalElapsed()
    {
        if (Subject.IsAlive)
        {
            RefreshRoot();
            PulseAggro();
            Subject.Animate(PulseAnimation, Source.Id);
        }
    }

    /// <summary>
    ///     How long each Root refresh lasts - slightly longer than the 1-second tick interval so there's no gap
    ///     where the caster could slip away between pulses
    /// </summary>
    private static readonly TimeSpan RootRefreshDuration = TimeSpan.FromMilliseconds(1250);

    /// <summary>
    ///     Iron Body's own cost, not a debuff - the caster can't move while it's active. Reapplied every tick
    ///     (rather than a single upfront Root matching Iron Body's own full duration) so a mid-cast dispel of the
    ///     Root alone can't let the caster walk away while Iron Body itself is still ticking.
    /// </summary>
    private void RefreshRoot()
    {
        var rootEffect = new RootEffect();
        rootEffect.SetDuration(RootRefreshDuration);
        Subject.Effects.Apply(Subject, rootEffect, SourceScript);
    }

    private void PulseAggro()
    {
        foreach (var monster in Subject.MapInstance.GetEntitiesWithinRange<Monster>(Subject, AggroRadius))
            if (monster.IsAlive)
                monster.AggroList.AddAggro(Subject, AggroPerTick);
    }
}
