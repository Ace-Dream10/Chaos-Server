#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     One of Ironscale's 7 specialization actives - "reflect the next incoming attack back to its attacker" per
///     the locked design. Distinct from Counter Strike/Bastion's Iron Reprisal (which negate the hit and deal a
///     flat STR/DEX-scaled counter-strike instead) - Perfect Counter genuinely mirrors the actual incoming damage
///     back at the attacker rather than substituting a fixed formula, handled directly in
///     ApplyAttackDamageScript, which checks for <see cref="ReadyTag" /> before applying damage to an Aisling. One
///     use only. Not one of Ironscale's 5 evolving abilities - flat.
/// </summary>
public sealed class PerfectCounterEffect : EffectBase
{
    public const string ReadyTag = "perfect_counter_ready";

    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 14,
        AnimationSpeed = 100
    };

    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(3000);

    public override byte Icon => 73;
    public override string Name => "Perfect Counter";

    public override void OnApplied()
    {
        Subject.Trackers.Tags[ReadyTag] = bool.TrueString;
        Subject.Animate(ApplyAnimation, Source.Id);
    }

    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(ReadyTag, out _);
}
