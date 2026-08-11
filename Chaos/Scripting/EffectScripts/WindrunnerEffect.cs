#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Windrunner (Fletcher passive) - applied by <see cref="Chaos.Scripting.SkillScripts.ArrowstepScript" /> after
///     Arrowstep is used, if the caster has learned this passive. The caster's next ranged attack deals bonus
///     damage (consumed the same one-shot-buff way Assassin's Ghost Step/Killing Intent readied effects were
///     tonight) and gains bonus critical-strike chance - see
///     <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" />'s crit-roll hook for
///     why that's a scoped local check rather than an engine-wide crit system. Placeholder values, not
///     balance-tested.
/// </summary>
public sealed class WindrunnerEffect : EffectBase
{
    public const string CritChanceBonusTag = "windrunnerCritBonus";
    public const string ReadyTag = "windrunner_ready";
    private const int CritChanceBonusPct = 20;

    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 133,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(6);

    /// <inheritdoc />
    public override byte Icon => 58;

    /// <inheritdoc />
    public override string Name => "Windrunner";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[ReadyTag] = bool.TrueString;
        Subject.Trackers.Tags[CritChanceBonusTag] = CritChanceBonusPct.ToString();
        Subject.Animate(ApplyAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated()
    {
        Subject.Trackers.Tags.TryRemove(ReadyTag, out _);
        Subject.Trackers.Tags.TryRemove(CritChanceBonusTag, out _);
    }
}
