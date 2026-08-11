#region
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

public sealed class SlowEffect : EffectBase
{
    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 114,
        AnimationSpeed = 100
    };

    private const string SlowedTag = "slowed";

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(4000);

    /// <inheritdoc />
    public override byte Icon => 44;

    /// <inheritdoc />
    public override string Name => "Slow";

    /// <inheritdoc />
    public override bool ShouldApply(Creature source, Creature target)
        => base.ShouldApply(source, target) && !BardMechanics.TryResistCc(target);

    /// <summary>
    ///     The amount added to the subject's MovementSpeedPct while slowed. Verified against Monster.Update's
    ///     movement modifier formula: for the positive-modifier branch, movementDelta = delta / (1 + modifier).
    ///     The XML doc on MovementSpeedPct claims "positive increases speed", but that's backwards from the
    ///     actual formula - positive values slow movement down. Set by the applying script before Apply() is
    ///     called, since EffectFactory.Create does not populate scriptVars onto effects the way Scripts do.
    /// </summary>
    public int SlowAmount { get; set; } = 250;

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[SlowedTag] = bool.TrueString;
        Subject.Animate(ApplyAnimation, Source.Id);

        if (Subject is Monster monster)
            monster.MovementSpeedPct += SlowAmount;
    }

    /// <inheritdoc />
    public override void OnTerminated()
    {
        Subject.Trackers.Tags.TryRemove(SlowedTag, out _);

        if (Subject is Monster monster)
            monster.MovementSpeedPct -= SlowAmount;
    }
}
