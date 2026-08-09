#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Applied by Cold Blood. Instantly fills the subject's MP to maximum and holds it there for the duration,
///     maximizing Severance potential, then releases MP back to normal flow on termination. Also grants lifesteal
///     while active - checked directly in
///     <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" /> via
///     <see cref="LifestealTag" />, the same tag-read pattern used for Boiling Blood/Mark of the Bane.
/// </summary>
/// <remarks>
///     One of Slayer's 5 evolving abilities - <see cref="Chaos.Scripting.SkillScripts.ColdBloodScript" /> tier-scales <see cref="Duration" />
///     and <see cref="LifestealPct" /> per the locked design's evolution note ("longer duration... increased
///     lifesteal while active"). "Faster Execution generation" is not separately implemented - Cold Blood already
///     instantly maxes Severance-stack-equivalent MP on application, which makes a generation-rate boost moot
///     while it's up. "Reduced cooldown" is deferred - this pass tier-scales the effect's own numbers, not the
///     learnable skill's cooldown value, consistent with how the skill's JSON template stores a single flat
///     cooldownMs regardless of level.
/// </remarks>
public sealed class ColdBloodEffect : EffectBase
{
    public const string LifestealTag = "cold_blood_lifesteal_pct";

    private static readonly Animation ColdAnimation = new()
    {
        TargetAnimation = 70,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(15000);

    /// <inheritdoc />
    public override byte Icon => 47;

    /// <inheritdoc />
    public override string Name => "Cold Blood";

    /// <summary>
    ///     The lifesteal percentage granted while active - set by <see cref="Chaos.Scripting.SkillScripts.ColdBloodScript" /> per tier before
    ///     Apply() is called, the same way <see cref="BleedEffect.BleedDamage" /> is set.
    /// </summary>
    public int LifestealPct { get; set; }

    /// <inheritdoc />
    public override void OnApplied()
    {
        FillToMax();
        Subject.Trackers.Tags[LifestealTag] = LifestealPct.ToString();
        Subject.Animate(ColdAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(LifestealTag, out _);

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        base.Update(delta);

        if (!Subject.IsAlive)
            return;

        FillToMax();
    }

    private void FillToMax()
    {
        var maxMp = Convert.ToInt32(Subject.StatSheet.EffectiveMaximumMp);

        if (Subject.StatSheet.CurrentMp == maxMp)
            return;

        Subject.StatSheet.SetMp(maxMp);

        if (Subject is Aisling aisling)
            aisling.Client.SendAttributes(StatUpdateType.Vitality);
    }
}
