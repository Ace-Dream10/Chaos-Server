#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

public class PoisonBombScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public PoisonBombScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var centerPoint = Point.From(source);

        source.AnimateBody(BodyAnimation);

        var targets = map.GetEntitiesAtPoints<Monster>(centerPoint.SpiralSearch(AoeRange))
                         .Where(monster => Filter.IsValidTarget(source, monster));

        foreach (var monster in targets)
        {
            var poisonBombEffect = new PoisonBombEffect
            {
                DamagePerTick = DamagePerTick,
                ApplyAnimation = Animation
            };
            poisonBombEffect.SetDuration(TimeSpan.FromMilliseconds(EffectDurationMs));
            monster.Effects.Apply(source, poisonBombEffect, this);
        }

        if (Animation != null)
            map.ShowAnimation(Animation.GetPointAnimation(centerPoint, source.Id));

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, centerPoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The radius, around the caster, affected by the bomb
    /// </summary>
    public int AoeRange { get; init; } = 2;

    /// <summary>
    ///     The animation played on each affected monster, and as the burst effect at the caster's position
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The amount of damage the poison deals on each tick
    /// </summary>
    public int DamagePerTick { get; init; } = 30;

    /// <summary>
    ///     How long, in milliseconds, the poison lasts
    /// </summary>
    public int EffectDurationMs { get; init; } = 6000;

    /// <summary>
    ///     The filter used to determine which nearby creatures are affected
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     Sound played at the caster's position on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
