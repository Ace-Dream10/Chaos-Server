#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Bastion's "Iron Reprisal" (renamed from "Counter", Floor 1 shield-focused starter trio) - a defensive
///     stance where the next hit taken is negated entirely and countered. Cloned from Martial Artist's
///     <see cref="CounterStrikeEffect" />, which already implements this exact "timed counterattack" mechanic -
///     kept as a distinct class (rather than sharing the Martial Artist effect directly) so the tooltip/combat
///     text reads "Iron Reprisal" for Bastions, and so its own <see cref="ReadyTag" /> doesn't collide with
///     Counter Strike's in <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" />.
///     Handled directly there, which checks for <see cref="ReadyTag" /> before applying damage to an Aisling. One
///     use only.
/// </summary>
public sealed class IronReprisalEffect : EffectBase
{
    public const string ReadyTag = "iron_reprisal_ready";
    private const int BaseDamage = 60;
    private const decimal StrMultiplier = 3m;

    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 14,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(3000);

    /// <inheritdoc />
    public override byte Icon => 73;

    /// <inheritdoc />
    public override string Name => "Iron Reprisal";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[ReadyTag] = bool.TrueString;
        Subject.Animate(ApplyAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(ReadyTag, out _);

    /// <summary>
    ///     The counter-attack damage dealt to whoever triggers this, scaled off the defender's own STR (a tank
    ///     stat, unlike Counter Strike's DEX scaling which fits Martial Artist better).
    /// </summary>
    public static int CalculateCounterDamage(Creature defender) => BaseDamage + Convert.ToInt32(defender.StatSheet.GetEffectiveStat(Stat.STR) * StrMultiplier);
}
