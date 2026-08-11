#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts.HideEffects;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts.Abstractions;
using Microsoft.Extensions.Logging;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Renamed from Coup de Grâce - kept its exact vault-behind-and-strike mechanic (a direct match for the locked
///     design's "dash through your target with a swift slash"), plus its distinctive "turned" double-strike combo
///     (strike twice if the target is already turned away, once if not - but they'll be turned for next time,
///     which doubles as the "reposition for your next assassination" the locked design calls for). Adds the one
///     piece the original didn't have: the caster is dropped into Hide immediately after the strike ("before
///     instantly returning to Hide"), via the same <see cref="HideEffect" /> the base game's own Hide skill uses.
/// </summary>
public class VanishingSlashScript : ConfigurableSkillScriptBase
{
    private const string TurnedTag = "turned";

    private readonly IApplyDamageScript ApplyDamageScript;
    private readonly ILogger<VanishingSlashScript> Logger;

    /// <inheritdoc />
    public VanishingSlashScript(Skill subject, ILogger<VanishingSlashScript> logger)
        : base(subject)
    {
        ApplyDamageScript = ApplyAttackDamageScript.Create();
        Logger = logger;
    }

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

        if (CastAnimation != null)
            source.Animate(CastAnimation, source.Id);

        //the direction from the caster to the target, continued one tile past the target, lands behind them
        var behindTargetDirection = target.DirectionalRelationTo(context.SourcePoint);

        //the direction the target would need to face to be looking at the caster
        var directionTargetNeedsToFaceCaster = context.SourcePoint.DirectionalRelationTo(Point.From(target));

        //the monster's own AI (MoveToTargetScript) re-faces it toward its target every tick once adjacent, which
        //happens almost immediately after the first cast vaults the caster behind it - so the live Direction can't
        //be trusted to still reflect the turn we forced a moment ago. The "turned" tag persists independently of
        //that AI re-facing and is what actually drives the double-strike check.
        var isTurned = target.Trackers.Tags.ContainsKey(TurnedTag);

        //whether the target's LIVE direction currently has it facing away from the caster, independent of the
        //"turned" tag - logged separately so the tag-driven result can be compared against the raw direction math
        var facingAwayFromCasterNow = target.Direction == directionTargetNeedsToFaceCaster.Reverse();

        Logger.LogInformation(
            "Vanishing Slash: target={Target} targetId={TargetId} sourceDirection={SourceDirection} targetDirection={TargetDirection} facingAwayFromCasterNow={FacingAwayNow} turnedTag={TurnedTag} sourcePoint={SourcePoint} targetPoint={TargetPoint}",
            target.Name,
            target.Id,
            source.Direction,
            target.Direction,
            facingAwayFromCasterNow,
            isTurned,
            context.SourcePoint,
            Point.From(target));

        var destinationPoint = target.DirectionalOffset(behindTargetDirection);

        if (!map.IsWalkable(destinationPoint, source, false))
        {
            Logger.LogInformation(
                "Vanishing Slash: vault BLOCKED, destinationPoint={DestinationPoint} - aborting before tag update",
                destinationPoint);

            return;
        }

        source.WarpTo(destinationPoint);
        var newDirection = target.DirectionalRelationTo(source);
        source.Turn(newDirection);

        source.AnimateBody(ArrivalBodyAnimation);

        int damage;

        if (isTurned)
        {
            damage = CalculateDamage(source, DoubleStrikeMultiplier ?? 2);
            target.Trackers.Tags.TryRemove(TurnedTag, out _);
        } else
        {
            //the target wasn't turned - force them to face away from the caster (for visual feedback; the AI will
            //likely re-face them almost immediately) and set the tag that actually drives next cast's double strike
            target.Turn(directionTargetNeedsToFaceCaster.Reverse(), forced: true);
            target.Trackers.Tags[TurnedTag] = bool.TrueString;
            damage = CalculateDamage(source, 1);
        }

        if (damage > 0)
            ApplyDamageScript.ApplyDamage(source, target, this, damage);

        if (Animation != null)
            target.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, Point.From(target));

        //instantly return to Hide after the strike, repositioning the caster for their next assassination
        source.Effects.Apply(source, new HideEffect(), this);
    }

    private int CalculateDamage(Creature source, decimal multiplier)
    {
        var damage = BaseDamage ?? 0;

        if (DamageStat.HasValue)
        {
            var statValue = source.StatSheet.GetEffectiveStat(DamageStat.Value);

            damage += DamageStatMultiplier.HasValue ? Convert.ToInt32(statValue * DamageStatMultiplier.Value) : statValue;
        }

        return Convert.ToInt32(damage * multiplier);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the target on hit
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster when the skill is used, before the vault
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.BaseDamage" />
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster upon arrival behind the target
    /// </summary>
    public BodyAnimation ArrivalBodyAnimation { get; init; } = BodyAnimation.Stab;

    /// <summary>
    ///     The self-animation played on the caster when the skill is used
    /// </summary>
    public Animation? CastAnimation { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.DamageStat" />
    public Stat? DamageStat { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.DamageStatMultiplier" />
    public decimal? DamageStatMultiplier { get; init; }

    /// <summary>
    ///     The multiplier applied to the calculated damage when the target is already turned away from the caster
    /// </summary>
    public decimal? DoubleStrikeMultiplier { get; init; }

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
    #endregion
}
