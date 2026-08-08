#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.FunctionalScripts.ApplyHealing;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Bastion's Floor 10 finale, flat (NOT one of the 5 evolving abilities - deliberately, per the locked
///     design, to keep the class-wide 5-evolving rule consistent). Genuinely new, complex content - flagged in
///     the Step 0 investigation before writing, per builder.md's guidance not to hand-write complex scripts
///     without flagging first. Hurl the shield in a line; unlike <see cref="BastionsChargeScript" /> (which stops
///     at the first creature), this ricochets through every hostile in its path, damaging and staggering each,
///     then - after a short return delay representing the boomerang - heals the caster for an amount scaled to
///     how many enemies were struck. Requires a shield equipped, same check as <see cref="ShieldThrustScript" />.
/// </summary>
public class ValkorsAegisScript : ConfigurableSkillScriptBase
{
    private readonly List<PendingReturn> PendingReturns = [];

    /// <inheritdoc />
    public ValkorsAegisScript(Skill subject)
        : base(subject)
    {
        ApplyDamageScript = ApplyAttackDamageScript.Create();
        ApplyHealScript = FunctionalScripts.ApplyHealing.ApplyHealScript.Create();
    }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        if ((source is Aisling aisling) && !HasShieldEquipped(aisling))
        {
            aisling.SendOrangeBarMessage("You need a shield equipped.");

            return;
        }

        source.AnimateBody(BodyAnimation);

        var endPoint = source.DirectionalOffset(source.Direction, RangeTiles);

        var points = source.GetDirectPath(endPoint)
                            .Skip(1);

        var strikeCount = 0;

        foreach (var point in points)
        {
            if (map.IsWall(point) || map.IsBlockingReactor(point))
                break;

            foreach (var creature in map.GetEntitiesAtPoints<Creature>(point))
            {
                if (!Filter.IsValidTarget(source, creature) || !creature.IsAlive)
                    continue;

                strikeCount++;

                var damage = (BaseDamage ?? 0)
                             + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.STR) * (DamageStatMultiplier ?? 1));

                if (damage > 0)
                    ApplyDamageScript.ApplyDamage(source, creature, this, damage);

                var staggerEffect = new RootEffect();
                staggerEffect.SetDuration(TimeSpan.FromMilliseconds(StaggerDurationMs));
                creature.Effects.Apply(source, staggerEffect, this);

                if (Animation != null)
                    creature.Animate(Animation, source.Id);
            }

            if (OutboundAnimation != null)
                map.ShowAnimation(OutboundAnimation.GetPointAnimation(point, source.Id));
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);

        //The boomerang return - reward scales with how many enemies were struck on the way out. A pure heal for
        //now (the design says "shield/heal" - either satisfies it); a damage-absorb-shield variant could replace
        //this later without changing the ability's shape.
        if (strikeCount > 0)
            PendingReturns.Add(new PendingReturn(source, TimeSpan.FromMilliseconds(ReturnDelayMs), strikeCount));
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (PendingReturns.Count == 0)
            return;

        for (var i = PendingReturns.Count - 1; i >= 0; i--)
        {
            var pending = PendingReturns[i];
            pending.Remaining -= delta;

            if (pending.Remaining > TimeSpan.Zero)
                continue;

            PendingReturns.RemoveAt(i);

            if (!pending.Source.IsAlive)
                continue;

            var healAmount = HealPerEnemyStruck * pending.StrikeCount;

            if (healAmount > 0)
                ApplyHealScript.ApplyHeal(pending.Source, pending.Source, this, healAmount);

            if (ReturnAnimation != null)
                pending.Source.Animate(ReturnAnimation, pending.Source.Id);
        }
    }

    private static bool HasShieldEquipped(Aisling aisling) => aisling.Equipment[EquipmentSlot.Shield] != null;

    private sealed class PendingReturn(Creature source, TimeSpan remaining, int strikeCount)
    {
        public TimeSpan Remaining { get; set; } = remaining;
        public Creature Source { get; } = source;
        public int StrikeCount { get; } = strikeCount;
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each struck creature
    /// </summary>
    public Animation? Animation { get; init; }

    public IApplyDamageScript ApplyDamageScript { get; init; }
    public IApplyHealScript ApplyHealScript { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt to each struck creature
    /// </summary>
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.DamageStat" />
    public Stat? DamageStat { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.DamageStatMultiplier" />
    public decimal? DamageStatMultiplier { get; init; }

    /// <summary>
    ///     The filter used to determine which creatures along the line are valid targets
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     Flat heal granted per enemy struck on the outbound throw, applied when the shield "returns"
    /// </summary>
    public int HealPerEnemyStruck { get; init; } = 15;

    /// <summary>
    ///     The trail animation played at each point along the outbound throw
    /// </summary>
    public Animation? OutboundAnimation { get; init; }

    /// <summary>
    ///     The maximum number of tiles the shield travels outward
    /// </summary>
    public int RangeTiles { get; init; } = 6;

    /// <summary>
    ///     The animation played on the caster when the shield returns
    /// </summary>
    public Animation? ReturnAnimation { get; init; }

    /// <summary>
    ///     How long, in milliseconds, after the outbound throw the shield "returns" and grants its reward
    /// </summary>
    public int ReturnDelayMs { get; init; } = 600;

    /// <summary>
    ///     Sound played on cast
    /// </summary>
    public byte? Sound { get; init; }

    /// <summary>
    ///     How long, in milliseconds, each struck creature is staggered (rooted) for
    /// </summary>
    public int StaggerDurationMs { get; init; } = 1200;
    #endregion
}
