#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Bastion's "Stacia's Bulwark" (renamed from "Perfect Stand") - temporary invulnerability, one of Bastion's
///     5 evolving abilities (longer duration and stronger effects with each evolution, per the locked design).
///     Genuinely new; no "Perfect Stand" or invulnerability mechanic existed anywhere in the codebase before this.
///     Follows <c>builder.md</c>'s explicit guidance for this exact case: true invulnerability should NOT rely on
///     a huge negative AC bonus (Aisling AC is clamped to a much less extreme floor than Monster AC, so an
///     AC trick that fully negates damage for a monster would not do the same for a player) - instead this just
///     sets <see cref="InvulnerableTag" />, which <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" />
///     checks first, before any other mitigation, and short-circuits the entire hit.
/// </summary>
public sealed class StaciasBulwarkEffect : EffectBase
{
    public const string InvulnerableTag = "stacias_bulwark_invulnerable";

    private static readonly Animation BulwarkAnimation = new()
    {
        TargetAnimation = 157,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(2);

    /// <inheritdoc />
    public override byte Icon => 57;

    /// <inheritdoc />
    public override string Name => "Stacia's Bulwark";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[InvulnerableTag] = bool.TrueString;
        Subject.Animate(BulwarkAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(InvulnerableTag, out _);
}
