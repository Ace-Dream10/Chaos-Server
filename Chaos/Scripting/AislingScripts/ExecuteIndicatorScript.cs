#region
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Scripting.AislingScripts.Abstractions;
#endregion

namespace Chaos.Scripting.AislingScripts;

/// <summary>
///     For Aislings that know the Execute skill, periodically re-plays an animation over any nearby monster below
///     the execute threshold - a visual indicator that the target is executable.
/// </summary>
public class ExecuteIndicatorScript : AislingScriptBase
{
    private const int AnimationRefreshMs = 500;
    private const ushort IndicatorAnimation = 374;
    private const int Range = 13;
    private const decimal ThresholdPct = 20m;

    private TimeSpan SinceLastCheck = TimeSpan.Zero;

    /// <inheritdoc />
    public ExecuteIndicatorScript(Aisling subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (!Subject.SkillBook.ContainsByTemplateKey("execute"))
            return;

        SinceLastCheck += delta;

        if (SinceLastCheck < TimeSpan.FromMilliseconds(AnimationRefreshMs))
            return;

        SinceLastCheck = TimeSpan.Zero;

        var animation = new Animation
        {
            TargetAnimation = IndicatorAnimation,
            AnimationSpeed = 100
        };

        foreach (var monster in Subject.MapInstance.GetEntitiesWithinRange<Monster>(Subject, Range))
            if (monster.IsAlive && (monster.StatSheet.HealthPercent <= ThresholdPct))
                monster.Animate(animation, Subject.Id);
    }
}
