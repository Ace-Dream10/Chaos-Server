#region
using Chaos.Definitions;
using Chaos.Models.Data;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.Components.AbilityComponents;
using Chaos.Scripting.Components.Execution;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

public sealed class SpeedyEffect : EffectBase,
                                   GetTargetsAbilityComponent<Creature>.IGetTargetsComponentOptions,
                                   AnimationAbilityComponent.IAnimationComponentOptions
{
    /// <inheritdoc />
    public Animation? Animation { get; init; } = new()
    {
        TargetAnimation = 9,
        AnimationSpeed = 100
    };

    /// <inheritdoc />
    public bool AnimatePoints { get; init; }

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(2);

    /// <inheritdoc />
    public int? ExclusionRange { get; init; }

    /// <inheritdoc />
    public TargetFilter Filter { get; init; }

    /// <inheritdoc />
    public bool MustHaveTargets { get; init; }

    /// <inheritdoc />
    public int Range { get; init; }

    /// <inheritdoc />
    public AoeShape Shape { get; init; }

    /// <inheritdoc />
    public bool SingleTarget { get; init; } = true;

    /// <inheritdoc />
    public override byte Icon => 1;

    /// <inheritdoc />
    public override string Name => "Speedy";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.StatSheet.AddBonus(new Attributes { Dex = 5 });

        new ComponentExecutor(Subject, Subject).WithOptions(this)
                                                .ExecuteAndCheck<GetTargetsAbilityComponent<Creature>>()
                                                ?.Execute<AnimationAbilityComponent>();
    }

    /// <inheritdoc />
    public override void OnTerminated()
        => Subject.StatSheet.SubtractBonus(new Attributes { Dex = 5 });
}
