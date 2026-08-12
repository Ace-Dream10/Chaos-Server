#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Bedrock (Ironscale passive) - "gain increasing damage reduction the longer you remain stationary." Applied
///     and refreshed by <see cref="Chaos.Scripting.AislingScripts.BedrockScript" /> while the caster hasn't moved,
///     read back by ApplyAttackDamageScript's mitigation chain via <see cref="DamageReductionPctTag" /> - same
///     tag-based approach as every other percentage modifier built tonight. Terminates (and the stack count resets)
///     the instant the caster moves.
/// </summary>
public sealed class BedrockEffect : EffectBase
{
    /// <summary>
    ///     The Trackers.Tags key storing the percentage damage reduction while Bedrock is active
    /// </summary>
    public const string DamageReductionPctTag = "bedrockDamageReductionPct";

    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    ///     The percentage damage reduction granted at the caster's current stationary-stack level
    /// </summary>
    public int DamageReductionPct { get; set; }

    public override byte Icon => 42;
    public override string Name => "Bedrock";

    public override void OnApplied() => Subject.Trackers.Tags[DamageReductionPctTag] = DamageReductionPct.ToString();

    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(DamageReductionPctTag, out _);
}
