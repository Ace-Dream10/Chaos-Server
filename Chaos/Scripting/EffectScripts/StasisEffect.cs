#region
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

public sealed class StasisEffect : EffectBase
{
    /// <summary>
    ///     How often the freeze visual is re-played while the effect is active
    /// </summary>
    private static readonly TimeSpan AnimationRefreshInterval = TimeSpan.FromMilliseconds(500);

    private static readonly Animation FreezeAnimation = new()
    {
        TargetAnimation = 40,
        AnimationSpeed = 100
    };

    /// <summary>
    ///     A bonus large enough that, once clamped by MinimumMonsterAc, guarantees effective invulnerability regardless of
    ///     the monster's base AC
    /// </summary>
    private const int InvulnerabilityAcBonus = -10000;

    private const string StasisTag = "stasis";

    private TimeSpan SinceLastAnimation;

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(3500);

    /// <inheritdoc />
    public override byte Icon => 42;

    /// <inheritdoc />
    public override string Name => "Stasis";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.StatSheet.AddBonus(new Attributes { Ac = InvulnerabilityAcBonus });
        Subject.Trackers.Tags[StasisTag] = bool.TrueString;

        //stop chasing/attacking whoever it was targeting
        if (Subject is Monster monster)
        {
            monster.Target = null;
            monster.AggroList.Clear();
        }

        Subject.Animate(FreezeAnimation, Source.Id);
        SinceLastAnimation = TimeSpan.Zero;
    }

    /// <inheritdoc />
    public override void OnTerminated()
    {
        Subject.StatSheet.SubtractBonus(new Attributes { Ac = InvulnerabilityAcBonus });
        Subject.Trackers.Tags.TryRemove(StasisTag, out _);
    }

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
        Subject.Animate(FreezeAnimation, Source.Id);
    }
}
