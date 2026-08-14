#region
using Chaos.Models.Data;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Applied by Heavenly Strike on hit. Resolves the design ambiguity flagged when Chooser of the Slain was
///     first built: the locked design says that passive triggers on defeating a "marked" enemy, but nothing in
///     Valkyrie's kit applied a mark - this is that mark. Simple presence-tag debuff, same shape as
///     <see cref="BoilingBloodEffect" /> - no mechanical effect of its own beyond existing; Chooser of the Slain
///     (a stateless hook in
///     <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" />) checks for its
///     presence at the moment a marked target dies.
/// </summary>
public sealed class MarkedForValhallaEffect : EffectBase
{
    public const string MarkedTag = "markedForValhalla";

    /// <summary>
    ///     How often the mark visual is re-played while the effect is active - same refresh-tick pattern used for
    ///     Mark of the Bane/Death Mark (per playtest feedback: an apply-once visual fades well before the effect
    ///     ends, so the connection between "this enemy is marked" and Chooser of the Slain triggering on its death
    ///     wasn't visible/clear to players).
    /// </summary>
    private static readonly TimeSpan AnimationRefreshInterval = TimeSpan.FromMilliseconds(1500);

    private static readonly Animation MarkAnimation = new()
    {
        TargetAnimation = 56,
        AnimationSpeed = 100
    };

    private TimeSpan SinceLastAnimation;

    /// <summary>
    ///     Placeholder duration - not balance-tested, per every other number in this class's kit.
    /// </summary>
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(5);

    /// <inheritdoc />
    public override byte Icon => 67;

    /// <inheritdoc />
    public override string Name => "Marked for Valhalla";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[MarkedTag] = bool.TrueString;
        Subject.Animate(MarkAnimation, Source.Id);
        SinceLastAnimation = TimeSpan.Zero;
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(MarkedTag, out _);

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
        Subject.Animate(MarkAnimation, Source.Id);
    }

    /// <summary>
    ///     Always allow reapplication - each Heavenly Strike hit refreshes the mark's duration, same reasoning as
    ///     <see cref="SeveranceEffect.ShouldApply" />/<see cref="MarkOfTheBaneEffect.ShouldApply" />.
    /// </summary>
    public override bool ShouldApply(Creature source, Creature target) => true;
}
