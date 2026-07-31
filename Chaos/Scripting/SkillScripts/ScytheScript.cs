#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

public class ScytheScript : ConfigurableSkillScriptBase
{
    private const string SeveranceTargetTag = "severanceTarget";
    private const string StacksTag = "severance_stacks";
    private const string SeveredTag = "severed";

    private readonly IApplyDamageScript ApplyDamageScript;

    /// <inheritdoc />
    public ScytheScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        var endPoint = source.DirectionalOffset(source.Direction, Range);

        var points = source.GetDirectPath(endPoint)
                            .Skip(1);

        Creature? target = null;

        foreach (var point in points)
        {
            if (map.IsWall(point) || map.IsBlockingReactor(point))
                break;

            var entity = map.GetEntitiesAtPoints<Creature>(point)
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

        var stackCount = 0;

        if (target.Trackers.Tags.TryGetValue(StacksTag, out var stacksStr))
            int.TryParse(stacksStr, out stackCount);

        if (stackCount == 0)
            context.SourceAisling?.SendOrangeBarMessage("No severance stacks on target.");

        var damage = (BaseDamage ?? 0) + (stackCount * (StackMultiplier ?? 0));

        if (damage > 0)
            ApplyDamageScript.ApplyDamage(source, target, this, damage);

        target.Trackers.Tags.TryRemove(StacksTag, out _);
        target.Trackers.Tags.TryRemove(SeveredTag, out _);
        target.Effects.Terminate("Severance");

        if (context.SourceAisling is { } sourceAisling)
        {
            sourceAisling.Trackers.Tags.TryRemove(SeveranceTargetTag, out _);
            sourceAisling.StatSheet.SetMp(0);
            sourceAisling.Client.SendAttributes(StatUpdateType.Vitality);
        }

        if (Animation != null)
            target.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, Point.From(target));
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the target on hit
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt, before the severance stack bonus
    /// </summary>
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster when the skill is used
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine whether the first creature encountered in the scan is a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The maximum number of tiles scanned in front of the caster for a target
    /// </summary>
    public int Range { get; init; }

    /// <summary>
    ///     Sound played on hit
    /// </summary>
    public byte? Sound { get; init; }

    /// <summary>
    ///     The bonus damage added per severance stack on the target
    /// </summary>
    public int? StackMultiplier { get; init; }
    #endregion
}
