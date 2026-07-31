#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     A self-buff. While active, the caster's next landed attack poisons and bleeds the target - handled directly
///     in <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" />, which checks for
///     <see cref="ReadyTag" /> on every hit an Aisling lands on a monster.
/// </summary>
public sealed class VenomBladeEffect : EffectBase
{
    public const string ReadyTag = "venom_blade_ready";

    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 46,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(30000);

    /// <inheritdoc />
    public override byte Icon => 58;

    /// <inheritdoc />
    public override string Name => "Venom Blade";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[ReadyTag] = bool.TrueString;
        Subject.Animate(ApplyAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(ReadyTag, out _);
}
