#region
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     A passive self-buff. While active, the next time the caster's HP would hit 0, death is prevented instead -
///     handled directly in <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" />,
///     which checks for <see cref="ReadyTag" /> before applying lethal damage to an Aisling. One use only - the tag
///     (and this effect) is consumed the moment it saves the caster.
/// </summary>
public sealed class PhoenixRiseEffect : EffectBase
{
    public const string ReadyTag = "phoenix_rise_ready";

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(300000);

    /// <inheritdoc />
    public override byte Icon => 62;

    /// <inheritdoc />
    public override string Name => "Phoenix Rise";

    /// <inheritdoc />
    public override void OnApplied() => Subject.Trackers.Tags[ReadyTag] = bool.TrueString;

    /// <inheritdoc />
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(ReadyTag, out _);
}
