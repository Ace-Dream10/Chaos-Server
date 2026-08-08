#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Self-buff applied by <see cref="Chaos.Scripting.SkillScripts.ChallengingShoutScript" /> at tier II and above
///     (one of Bastion's 5 evolving abilities, tier scales with the skill's own level per the established
///     Cyclone/Berserker Gate convention). Grants a flat AC bonus ("damage reduction while active" per the locked
///     design) and sets <see cref="TauntImmuneTag" /> for "brief taunt immunity" (tier III+).
/// </summary>
/// <remarks>
///     Design ambiguity surfaced during implementation: nothing in the current codebase applies a fear/taunt-style
///     effect to a player character (<see cref="FearedEffect" /> only ever targets monsters), so
///     <see cref="TauntImmuneTag" /> currently has no consumer - it's a forward-compatible scaffold, set and
///     cleared correctly, ready for whatever future effect would need to respect it. Not a stub; just unconsumed.
///     Magnitude/duration are placeholders, not balance-tested.
/// </remarks>
public sealed class ChallengingResolveEffect : EffectBase
{
    public const string TauntImmuneTag = "taunt_immune";

    private static readonly Animation ResolveAnimation = new()
    {
        TargetAnimation = 73,
        AnimationSpeed = 100
    };

    /// <summary>
    ///     Defense bonus applied while active - a negative AC value, since lower AC is better defense in this
    ///     engine's inverted AC convention.
    /// </summary>
    public int AcBonus { get; set; }

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(5);

    /// <inheritdoc />
    public override byte Icon => 73;

    /// <inheritdoc />
    public override string Name => "Challenging Resolve";

    /// <summary>
    ///     Whether this instance should set <see cref="TauntImmuneTag" /> - only true at tier III and above.
    /// </summary>
    public bool GrantsTauntImmunity { get; set; }

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.StatSheet.AddBonus(new Attributes { Ac = AcBonus });

        if (GrantsTauntImmunity)
            Subject.Trackers.Tags[TauntImmuneTag] = bool.TrueString;

        Subject.Animate(ResolveAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated()
    {
        Subject.StatSheet.SubtractBonus(new Attributes { Ac = AcBonus });

        if (GrantsTauntImmunity)
            Subject.Trackers.Tags.TryRemove(TauntImmuneTag, out _);
    }
}
