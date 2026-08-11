#region
using Chaos.Definitions;
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Applied by Ghost Step. Combines two existing precedents rather than one direct rename: sets the caster's
///     visibility to <see cref="VisibilityType.Hidden" /> (same mechanism as the base game's own
///     <see cref="Chaos.Scripting.EffectScripts.HideEffects.HideEffect" />) AND immediately drops all threat
///     (same map-wide aggro-clear <see cref="VanishEffect" /> uses for Trickster's Vanishing Act) - the locked
///     description asks for both ("drop all threat, enter Hide, and vanish from enemy sight") where the base Hide
///     skill only does the visibility half. Also sets <see cref="ReadyTag" />, consumed by
///     <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" /> on the caster's next
///     landed hit for the "next attack from Hide deals bonus damage" bonus, same one-shot-buff shape as
///     <see cref="KillingIntentEffect" />. Placeholder duration, not balance-tested.
/// </summary>
public sealed class GhostStepEffect : EffectBase
{
    public const string ReadyTag = "ghost_step_ready";

    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 133,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(10);

    /// <inheritdoc />
    public override byte Icon => 10;

    /// <inheritdoc />
    public override string Name => "Ghost Step";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.SetVisibility(VisibilityType.Hidden);
        Subject.Trackers.Tags[ReadyTag] = bool.TrueString;

        if (Subject is Aisling aisling)
            foreach (var monster in Subject.MapInstance.GetEntities<Monster>())
            {
                monster.AggroList.Clear(aisling);

                if ((monster.Target != null) && monster.Target.Equals(aisling))
                    monster.Target = null;
            }

        Subject.Animate(ApplyAnimation, Source.Id);
    }

    /// <inheritdoc />
    public override void OnTerminated()
    {
        Subject.SetVisibility(VisibilityType.Normal);
        Subject.Trackers.Tags.TryRemove(ReadyTag, out _);
    }
}
