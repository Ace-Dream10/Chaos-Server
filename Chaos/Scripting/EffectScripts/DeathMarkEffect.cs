#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     A delayed-detonation mark. If the target is still alive when the mark expires, it detonates for massive
///     damage scaled off the caster's DEX. If the target dies before the mark expires (from any other source), the
///     detonation simply never happens - there's nothing left to hit.
/// </summary>
public sealed class DeathMarkEffect : EffectBase
{
    private const int BaseDamage = 200;
    private const decimal DexMultiplier = 5m;

    private static readonly Animation MarkAnimation = new()
    {
        TargetAnimation = 374,
        AnimationSpeed = 100
    };

    private readonly IApplyDamageScript ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(3000);

    /// <inheritdoc />
    public override byte Icon => 56;

    /// <inheritdoc />
    public override string Name => "Death Mark";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[Name] = bool.TrueString;
        Subject.Animate(MarkAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated()
    {
        Subject.Trackers.Tags.TryRemove(Name, out _);

        if (!Subject.IsAlive)
            return;

        var dex = Source.StatSheet.GetEffectiveStat(Stat.DEX);
        var damage = BaseDamage + Convert.ToInt32(dex * DexMultiplier);

        ApplyDamageScript.ApplyDamage(Source, Subject, this, damage);
        Subject.Animate(MarkAnimation, Source.Id);
    }
}
