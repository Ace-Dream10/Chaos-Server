#region
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Applied by Trickster's Vanishing Act. Immediately clears the caster from every monster's aggro list/current
///     target map-wide, and <see cref="Chaos.Scripting.MonsterScripts.AggroTargetingScript" /> refuses to
///     (re)acquire an Aisling tagged <see cref="VanishedTag" />. Breaks early the moment the caster uses any other
///     skill or spell (same "breaks on action" behavior as classic Hide) - the baseline skill/spell-use timestamps
///     are captured on the first tick after being applied rather than at apply time, since the very cast that
///     applied this effect hasn't finished stamping <c>Trackers.LastSkillUse</c> yet when <see cref="OnApplied" />
///     runs.
/// </summary>
public sealed class VanishEffect : EffectBase
{
    public const string VanishedTag = "vanished";

    private static readonly Animation ApplyAnimation = new()
    {
        TargetAnimation = 133,
        AnimationSpeed = 100
    };

    private bool BaselineCaptured;
    private DateTime? BaselineSkillUse;
    private DateTime? BaselineSpellUse;

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(3000);

    /// <inheritdoc />
    public override byte Icon => 76;

    /// <inheritdoc />
    public override string Name => "Vanish";

    /// <inheritdoc />
    public override void OnApplied()
    {
        Subject.Trackers.Tags[VanishedTag] = bool.TrueString;

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
    public override void OnTerminated() => Subject.Trackers.Tags.TryRemove(VanishedTag, out _);

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        base.Update(delta);

        if (!Subject.IsAlive)
            return;

        if (!BaselineCaptured)
        {
            BaselineSkillUse = Subject.Trackers.LastSkillUse;
            BaselineSpellUse = Subject.Trackers.LastSpellUse;
            BaselineCaptured = true;

            return;
        }

        if ((Subject.Trackers.LastSkillUse != BaselineSkillUse) || (Subject.Trackers.LastSpellUse != BaselineSpellUse))
            Remaining = TimeSpan.Zero;
    }
}
