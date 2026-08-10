#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     Detonates all Burn stacks currently on the target for a burst of damage, consuming them - Fire Tier III's
///     replacement for Heat Wave (locked this session, per the design doc's own note). Mirrors
///     <see cref="ScytheScript" />'s exact "consume stacks for a burst" shape, applied to Burn instead of
///     Severance - creates the same "build stacks via Kindling, then cash them in" mini-loop Scorch already
///     rewards passively.
/// </summary>
public class CombustionScript : ConfigurableSpellScriptBase
{
    private readonly IApplyDamageScript ApplyDamageScript;

    /// <inheritdoc />
    public CombustionScript(Spell subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
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

        if (target.Trackers.Tags.TryGetValue(BurnEffect.StacksTag, out var stacksStr))
            int.TryParse(stacksStr, out stackCount);

        if (stackCount == 0)
            context.SourceAisling?.SendOrangeBarMessage("No burn stacks on target.");

        var damage = BaseDamage + (stackCount * StackMultiplier);

        if (damage > 0)
            ApplyDamageScript.ApplyDamage(source, target, this, damage);

        target.Effects.Terminate("Burn");

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
    ///     The flat portion of the damage dealt, before the burn-stack bonus
    /// </summary>
    public int BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster when the spell is used
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine whether the first creature encountered in the scan is a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The maximum number of tiles scanned in front of the caster for a target
    /// </summary>
    public int Range { get; init; } = 8;

    /// <summary>
    ///     Sound played on hit
    /// </summary>
    public byte? Sound { get; init; }

    /// <summary>
    ///     The bonus damage added per burn stack on the target
    /// </summary>
    public int StackMultiplier { get; init; }
    #endregion
}
