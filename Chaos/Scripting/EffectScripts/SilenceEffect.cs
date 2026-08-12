#region
using Chaos.Models.Data;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Prevents the subject from casting spells. Can still move and use physical skills - the mirror image of
///     Blackout (which blocks physical/casting but allows movement). Enforced via
///     <see cref="Chaos.Scripting.Behaviors.RestrictionBehavior" />'s CanUseSpell override, which
///     <c>Creature.CanUse(Spell, ...)</c> checks for both Aislings and Monsters before any cast is allowed to
///     proceed. Was previously just an unused hook (AislingScriptBase.CanUseSpell/ICreatureScript.CanTalk's own doc
///     comment both flagged "silence" as intended but never wired up) - this is that wiring, plus the effect
///     itself.
/// </summary>
public sealed class SilenceEffect : EffectBase
{
    public const string SilencedTag = "silenced";

    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 39,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(3000);

    /// <inheritdoc />
    public override byte Icon => 61;

    /// <inheritdoc />
    public override string Name => "Silence";

    /// <inheritdoc />
    public override bool ShouldApply(Creature source, Creature target)
        => base.ShouldApply(source, target) && !BardMechanics.TryResistCc(target);

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[SilencedTag] = bool.TrueString;
        Subject.Animate(ApplyAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(SilencedTag, out _);
}
