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

/// <summary>
///     Scans the direct line in front of the caster for the first hostile monster within range and turns them
///     against their allies for a short time via <see cref="PuppeteerEffect" />.
/// </summary>
public class PuppeteerScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public PuppeteerScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        var endPoint = source.DirectionalOffset(source.Direction, Range);

        var points = source.GetDirectPath(endPoint)
                           .Skip(1);

        Monster? target = null;

        foreach (var point in points)
        {
            if (map.IsWall(point) || map.IsBlockingReactor(point))
                break;

            var entity = map.GetEntitiesAtPoints<Monster>(point)
                            .TopOrDefault();

            if (entity != null)
            {
                if (Filter.IsValidTarget(source, entity))
                    target = entity;

                break;
            }
        }

        if (target == null)
        {
            context.SourceAisling?.SendOrangeBarMessage("No target in range.");

            return;
        }

        source.AnimateBody(BodyAnimation);

        var puppeteerEffect = new PuppeteerEffect();
        puppeteerEffect.SetDuration(TimeSpan.FromMilliseconds(DurationMs));
        target.Effects.Apply(source, puppeteerEffect, this);

        if (Animation != null)
            target.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, Point.From(target));
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the target
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     How long, in milliseconds, the target is puppeteered for
    /// </summary>
    public int DurationMs { get; init; } = 8000;

    /// <summary>
    ///     The filter used to determine whether the first creature encountered in the scan is a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The maximum number of tiles scanned in front of the caster for a target
    /// </summary>
    public int Range { get; init; } = 4;

    /// <summary>
    ///     Sound played on the target's position
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
