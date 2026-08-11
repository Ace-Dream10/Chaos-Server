#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Applied by the Shadowmark passive (see
///     <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" />'s stateless hit-counter
///     hook) once every third damaging ability an Assassin lands on the same target. While active, the target
///     takes increased damage from the marking Assassin - same tag-read shape as
///     <see cref="MarkOfTheBaneEffect" />, but restricted to the attacker who actually earned the mark (checked via
///     <see cref="OwnerId" />) rather than applying from all sources, since Shadowmark is explicitly "the damage
///     YOU deal to them" in the locked design, not a shared debuff. <see cref="BonusDamagePct" /> is a placeholder,
///     not balance-tested.
/// </summary>
public sealed class ShadowmarkEffect : EffectBase
{
    public const string OwnerIdTagPrefix = "shadowmark_owner_";
    private const int BonusDamagePct = 15;

    private static readonly Animation MarkAnimation = new()
    {
        TargetAnimation = 56,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(6);

    /// <inheritdoc />
    public override byte Icon => 56;

    /// <inheritdoc />
    public override string Name => "Shadowmark";

    /// <summary>
    ///     The id of the Assassin whose hits benefit from this mark - set before Apply() is called
    /// </summary>
    public uint OwnerId { get; init; }

    /// <summary>
    ///     The tag key this specific owner's bonus is stored under - unique per attacker so two different Assassins
    ///     marking the same target don't clobber each other's bonus
    /// </summary>
    public string OwnerTag => OwnerIdTagPrefix + OwnerId;

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[OwnerTag] = BonusDamagePct.ToString();
        Subject.Animate(MarkAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(OwnerTag, out _);
}
