#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     A defensive stance. While active, the next hit the caster takes is negated entirely and countered - handled
///     directly in <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" />, which
///     checks for <see cref="ReadyTag" /> before applying damage to an Aisling. One use only.
/// </summary>
public sealed class CounterStrikeEffect : EffectBase
{
    public const string ReadyTag = "counter_strike_ready";
    private const int BaseDamage = 60;
    private const decimal DexMultiplier = 3m;

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
    public override string Name => "Counter Strike";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[ReadyTag] = bool.TrueString;
        Subject.Animate(ApplyAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(ReadyTag, out _);

    /// <summary>
    ///     The counter-attack damage dealt to whoever triggers this, scaled off the defender's own DEX
    /// </summary>
    public static int CalculateCounterDamage(Creature defender) => BaseDamage + Convert.ToInt32(defender.StatSheet.GetEffectiveStat(Stat.DEX) * DexMultiplier);
}
