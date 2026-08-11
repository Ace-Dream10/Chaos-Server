#region
using Chaos.Collections;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     A direct build - nothing existing matched "fire an arrow skyward that returns as celestial arrows striking
///     multiple enemies". Borrows the delayed-resolution pattern <see cref="RainOfArrowsScript" />/(the retired)
///     Deadcenter established (fire now, land later via a pending-action list ticked in <see cref="Update" />), but
///     as a single delayed AoE burst around the original target point rather than either a single delayed single-
///     target hit or repeated immediate waves. One of Fletcher's 5 evolving abilities (evolution specifics weren't
///     detailed in the locked design - filled in here as "more targets, wider area", flagged as such). Tiers per
///     Fletcher's own floor arc (Floor7 intro, Floor8, Floor9, Floor10 max/finale) via the same "Level ≈ 2×Floor"
///     ratio used throughout tonight - see <see cref="GetTierValues" />. All placeholder values, not
///     balance-tested.
/// </summary>
public class MoonfallScript : ConfigurableSpellScriptBase
{
    private readonly List<PendingFall> PendingFalls = [];

    /// <inheritdoc />
    public MoonfallScript(Spell subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var tier = GetTierValues();

        if (!source.StatSheet.TrySubtractMp(ManaCost))
        {
            context.SourceAisling?.SendOrangeBarMessage("Not enough focus.");

            return;
        }

        context.SourceAisling?.Client.SendAttributes(StatUpdateType.Vitality);

        source.AnimateBody(BodyAnimation);

        if (Sound.HasValue)
            context.TargetMap.PlaySound(Sound.Value, context.TargetPoint);

        PendingFalls.Add(new PendingFall(source, context.TargetMap, context.TargetPoint, tier.Radius, tier.MaxTargets, TimeSpan.FromMilliseconds(DelayMs)));
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (PendingFalls.Count == 0)
            return;

        for (var i = PendingFalls.Count - 1; i >= 0; i--)
        {
            var pending = PendingFalls[i];
            pending.Remaining -= delta;

            if (pending.Remaining > TimeSpan.Zero)
                continue;

            PendingFalls.RemoveAt(i);

            if (!pending.Source.IsAlive)
                continue;

            var targets = pending.Map
                                 .GetEntitiesWithinRange<Monster>(pending.TargetPoint, pending.Radius)
                                 .Where(monster => Filter.IsValidTarget(pending.Source, monster))
                                 .Take(pending.MaxTargets);

            foreach (var target in targets)
            {
                var damage = CalculateDamage(pending.Source);

                if (damage > 0)
                    ApplyDamageScript.ApplyDamage(pending.Source, target, this, damage);

                if (Animation != null)
                    target.Animate(Animation, pending.Source.Id);
            }

            if (OverlayAnimation != null)
                pending.Map.ShowAnimation(OverlayAnimation.GetPointAnimation(pending.TargetPoint, pending.Source.Id));
        }
    }

    private int CalculateDamage(Creature source)
    {
        var damage = BaseDamage ?? 0;

        if (!DamageStat.HasValue)
            return damage;

        var statValue = source.StatSheet.GetEffectiveStat(DamageStat.Value);

        damage += DamageStatMultiplier.HasValue ? Convert.ToInt32(statValue * DamageStatMultiplier.Value) : statValue;

        return damage;
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor7(Level&lt;=14)=I(intro,radius1,3 targets),
    ///     Floor8(&lt;=16)=II(radius2,5), Floor9(&lt;=18)=III(radius2,5), Floor10+(&gt;18)=IV(max/finale,radius3,8).
    /// </summary>
    private (int Radius, int MaxTargets) GetTierValues() =>
        Subject.Level switch
        {
            <= 14 => (1, 3),
            <= 16 => (2, 5),
            <= 18 => (2, 5),
            _     => (3, 8)
        };

    private sealed class PendingFall(Creature source, MapInstance map, Point targetPoint, int radius, int maxTargets, TimeSpan remaining)
    {
        public int MaxTargets { get; } = maxTargets;
        public MapInstance Map { get; } = map;
        public int Radius { get; } = radius;
        public TimeSpan Remaining { get; set; } = remaining;
        public Creature Source { get; } = source;
        public Point TargetPoint { get; } = targetPoint;
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each struck target
    /// </summary>
    public Animation? Animation { get; init; }

    public IApplyDamageScript ApplyDamageScript { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.BaseDamage" />
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
    ///     How long, in milliseconds, between cast and the celestial arrows landing
    /// </summary>
    public int DelayMs { get; init; } = 1500;

    /// <summary>
    ///     The filter used to determine which creatures near the target point are struck
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The MP cost to use this spell
    /// </summary>
    public int ManaCost { get; init; }

    /// <summary>
    ///     The animation played as an overlay, centered on the target point, when the arrows land
    /// </summary>
    public Animation? OverlayAnimation { get; init; }

    /// <summary>
    ///     Sound played once, on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
