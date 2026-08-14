#region
using Chaos.DarkAges.Definitions;
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
/// <remarks>
///     Reworked per playtest feedback to actually function like retail Dark Ages' "Fas Nadur" - researched rather
///     than guessed at (see darkages.com/community/lore/Larsius_Nadur.html): Fas Nadur's real mechanic swaps the
///     target's DEFENSIVE ELEMENT, making them sharply vulnerable to one specific element while resistant to their
///     original one - a setup debuff for a follow-up elemental hit, not just a flat damage-taken bonus. Layered on
///     top of the existing flat <see cref="MagicDamageTakenBonusPct" /> mechanic (kept as-is, already tier-scaled
///     and tested) rather than replacing it: while active, the target's <c>StatSheet.DefenseElement</c> is
///     temporarily forced to <see cref="Chaos.DarkAges.Definitions.Element.Darkness" /> - Mystic's own thematic
///     element, matching Spirit Burst's own damage type - so a Mystic can genuinely "rend their spirit, then
///     follow up with a Darkness spell" the way Fas Nadur was actually used in retail. Original DefenseElement is
///     restored on termination regardless of how the effect ends (natural expiry or Tier I's on-hit consumption).
/// </remarks>
public sealed class SpiritRendEffect : EffectBase
{
    /// <summary>
    ///     How often the visual is re-played while active. Confirmed before building this: changing
    ///     StatSheet.DefenseElement produces ZERO client-visible feedback on its own - no packet, no icon, no
    ///     animation (SetDefenseElement is a bare field mutator, and the only caller that ever sends an attributes
    ///     packet does so to the WEARER'S OWN client on gear equip, not to observers of a debuffed target). Given
    ///     tonight's repeated "real mechanic, zero visual feedback" pattern (Mark of the Bane, Death Mark, Bard's
    ///     Malediction, Blackout, Delirium all needed this same fix), this effect gets the persistent
    ///     re-animating indicator from the start rather than shipping invisible.
    /// </summary>
    private static readonly TimeSpan AnimationRefreshInterval = TimeSpan.FromMilliseconds(1200);

    private static readonly Animation RendAnimation = new()
    {
        TargetAnimation = 42,
        AnimationSpeed = 100
    };

    private Element OriginalDefenseElement;
    private TimeSpan SinceLastAnimation;

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

    /// <inheritdoc />
    public override void OnApplied()
    {
        OriginalDefenseElement = Subject.StatSheet.DefenseElement;
        Subject.StatSheet.SetDefenseElement(Element.Darkness);

        Subject.Animate(RendAnimation, Source.Id);
        SinceLastAnimation = TimeSpan.Zero;
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.StatSheet.SetDefenseElement(OriginalDefenseElement);

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
        Subject.Animate(RendAnimation, Source.Id);
    }
}
