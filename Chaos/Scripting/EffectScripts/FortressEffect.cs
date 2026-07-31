#region
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     A heavy defensive self-buff - big AC/CON/STR boost, but the caster can't move while it's active. The
///     "fortified" tag is checked by <see cref="Chaos.Scripting.Behaviors.RestrictionBehavior.CanMove" />, which is
///     shared by every creature type, so this works uniformly without needing a per-Aisling script.
/// </summary>
public sealed class FortressEffect : EffectBase
{
    public const string FortifiedTag = "fortified";
    private const int AcBonus = -30;
    private const int ConBonus = 20;
    private const int StrBonus = 10;

    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 35,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(20000);

    /// <inheritdoc />
    public override byte Icon => 63;

    /// <inheritdoc />
    public override string Name => "Fortress";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.StatSheet.AddBonus(
            new Attributes
            {
                Ac = AcBonus,
                Con = ConBonus,
                Str = StrBonus
            });

        Subject.Trackers.Tags[FortifiedTag] = bool.TrueString;
        Subject.Animate(ApplyAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated()
    {
        Subject.StatSheet.SubtractBonus(
            new Attributes
            {
                Ac = AcBonus,
                Con = ConBonus,
                Str = StrBonus
            });

        Subject.Trackers.Tags.TryRemove(FortifiedTag, out _);
    }
}
