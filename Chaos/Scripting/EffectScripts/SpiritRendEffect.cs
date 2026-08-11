#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     One of Mystic's 5 evolving abilities, formerly "Fas" - a direct build, no existing precedent to reuse.
///     Uniquely among this session's evolving actives, the locked design says it "evolves through BEHAVIOR, not
///     just numbers": Tier I is a single-consumption mark (the next magical hit against the target is amplified,
///     then the mark is gone), while Tiers II-IV are a standard timed window instead (every magical hit lands
///     harder for the whole duration). <see cref="ConsumeOnHit" /> is how ApplyAttackDamageScript's hook tells the
///     two behaviors apart - see the hook's own comment for how the bonus is actually read and applied (magical
///     damage is approximated as any hit whose element was explicitly overridden by the attacking script, per this
///     engine's existing Fire-specific passive checks - there's no clean physical/magical damage-type split
///     surfaced at this hook, so this is a scoping decision, not a full magic-damage-detection system). DoT
///     amplification (the Tier III/IV "amplifies magical damage-over-time effects" clause) falls out of this same
///     hook for free, since effects like Burn route their own tick damage through the identical ApplyDamage call.
/// </summary>
public sealed class SpiritRendEffect : EffectBase
{
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    ///     Tier I only - true means this effect is consumed (terminated) the instant it amplifies a single hit,
    ///     rather than lasting for its full Duration
    /// </summary>
    public bool ConsumeOnHit { get; set; }

    /// <summary>
    ///     The percentage bonus magical damage the marked target takes while this effect is active
    /// </summary>
    public int MagicDamageTakenBonusPct { get; set; }

    public override byte Icon => 42;
    public override string Name => "Spirit Rend";
}
