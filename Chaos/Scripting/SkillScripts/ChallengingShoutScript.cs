#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

public class ChallengingShoutScript : ConfigurableSkillScriptBase
{
    /// <summary>
    ///     Animation.DurationMs is never transmitted over the wire (see AnimationConverter) - it's only used
    ///     server-side for animation-priority interpolation. To make the mob animation visually persist, it has
    ///     to be re-triggered on an interval instead, the same technique StasisEffect uses for its freeze visual.
    /// </summary>
    private static readonly TimeSpan MobAnimationRefreshInterval = TimeSpan.FromMilliseconds(500);

    private readonly List<PulseTarget> PulseTargets = [];

    /// <inheritdoc />
    public ChallengingShoutScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        foreach (var monster in map.GetEntities<Monster>())
            if (Filter.IsValidTarget(source, monster))
            {
                monster.AggroList.AddAggro(source, 99999);

                if (MobAnimation != null)
                {
                    monster.Animate(MobAnimation, source.Id);

                    PulseTargets.Add(new PulseTarget(monster, source.Id, TimeSpan.FromMilliseconds(MobAnimationDurationMs)));
                }
            }

        if (CasterAnimation != null)
            map.ShowAnimation(CasterAnimation.GetPointAnimation(context.SourcePoint, source.Id));

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (PulseTargets.Count == 0)
            return;

        for (var i = PulseTargets.Count - 1; i >= 0; i--)
        {
            var pulse = PulseTargets[i];
            pulse.Remaining -= delta;
            pulse.SinceLastPulse += delta;

            if (!pulse.Monster.IsAlive || (pulse.Remaining <= TimeSpan.Zero))
            {
                PulseTargets.RemoveAt(i);

                continue;
            }

            if ((pulse.SinceLastPulse >= MobAnimationRefreshInterval) && (MobAnimation != null))
            {
                pulse.SinceLastPulse = TimeSpan.Zero;
                pulse.Monster.Animate(MobAnimation, pulse.SourceId);
            }
        }
    }

    private sealed class PulseTarget(Monster monster, uint sourceId, TimeSpan remaining)
    {
        public Monster Monster { get; } = monster;
        public TimeSpan Remaining { get; set; } = remaining;
        public TimeSpan SinceLastPulse { get; set; } = TimeSpan.Zero;
        public uint SourceId { get; } = sourceId;
    }

    #region ScriptVars
    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The pulse animation played centered on the caster's point
    /// </summary>
    public Animation? CasterAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine which creatures on the map are valid aggro targets
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The animation played on each affected monster
    /// </summary>
    public Animation? MobAnimation { get; init; }

    /// <summary>
    ///     How long, in milliseconds, the mob animation is re-triggered for on each affected monster
    /// </summary>
    public int MobAnimationDurationMs { get; init; } = 2500;

    /// <summary>
    ///     The sound played at the caster's position on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
