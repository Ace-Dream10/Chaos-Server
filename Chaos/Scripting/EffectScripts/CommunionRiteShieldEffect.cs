#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Communion Rite's Tier III/IV brief post-revive invulnerability, so the revive can't be instantly undone.
///     Reuses <see cref="StaciasBulwarkEffect.InvulnerableTag" /> directly - the same tag Guardian's Anthem and
///     Stacia's Grace (both Bard) already set - rather than inventing a parallel invulnerability tag, so
///     ApplyAttackDamageScript's existing invulnerability short-circuit picks it up with zero new hook code. A
///     dedicated effect class (rather than reusing GuardiansAnthemEffect directly) purely so the buff displays as
///     "Communion Rite" instead of Bard's ability name.
/// </summary>
public sealed class CommunionRiteShieldEffect : EffectBase
{
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(2);

    public override byte Icon => 65;
    public override string Name => "Communion Rite";

    public override void OnApplied() => Subject.Trackers.Tags[StaciasBulwarkEffect.InvulnerableTag] = bool.TrueString;

    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(StaciasBulwarkEffect.InvulnerableTag, out _);
}
