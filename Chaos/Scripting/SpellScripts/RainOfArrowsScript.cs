#region
using Chaos.Collections;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Common;
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

public class RainOfArrowsScript : ConfigurableSpellScriptBase
{
    private readonly List<PendingVolley> PendingVolleys = [];

    /// <inheritdoc />
    public RainOfArrowsScript(Spell subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;

        if ((source is Aisling aisling) && !HasBowEquipped(aisling))
        {
            aisling.SendOrangeBarMessage("You need a bow equipped.");

            return;
        }

        var map = context.TargetMap;
        var targetPoint = context.TargetPoint;

        source.AnimateBody(BodyAnimation);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, targetPoint);

        //volley 1 fires immediately, the rest are queued at VolleyDelayMs intervals
        FireVolley(source, map, targetPoint);

        for (var i = 1; i < VolleyCount; i++)
            PendingVolleys.Add(
                new PendingVolley(
                    source,
                    map,
                    targetPoint,
                    TimeSpan.FromMilliseconds(VolleyDelayMs * i)));
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (PendingVolleys.Count == 0)
            return;

        for (var i = PendingVolleys.Count - 1; i >= 0; i--)
        {
            var pending = PendingVolleys[i];
            pending.Remaining -= delta;

            if (pending.Remaining > TimeSpan.Zero)
                continue;

            PendingVolleys.RemoveAt(i);

            if (!pending.Source.IsAlive)
                continue;

            FireVolley(pending.Source, pending.Map, pending.TargetPoint);
        }
    }

    private void FireVolley(Creature source, MapInstance map, Point targetPoint)
    {
        var targets = map.GetEntitiesWithinRange<Creature>(targetPoint, Range)
                         .Where(creature => Filter.IsValidTarget(source, creature));

        foreach (var creature in targets)
        {
            var damage = CalculateDamage(source);

            if (damage > 0)
                ApplyDamageScript.ApplyDamage(source, creature, this, damage);

            if (Animation != null)
                creature.Animate(Animation, source.Id);
        }
    }

    private static bool HasBowEquipped(Aisling aisling)
    {
        var weapon = aisling.Equipment[EquipmentSlot.Weapon];

        return (weapon != null) && weapon.Template.Category.EqualsI("bow");
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

    private sealed class PendingVolley(Creature source, MapInstance map, Point targetPoint, TimeSpan remaining)
    {
        public MapInstance Map { get; } = map;
        public TimeSpan Remaining { get; set; } = remaining;
        public Creature Source { get; } = source;
        public Point TargetPoint { get; } = targetPoint;
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each creature hit by a volley
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
    ///     The filter used to determine which creatures in the target area are valid targets
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The radius around the target point affected by each volley
    /// </summary>
    public int Range { get; init; } = 2;

    /// <summary>
    ///     Sound played once, on cast
    /// </summary>
    public byte? Sound { get; init; }

    /// <summary>
    ///     How many volleys land total, including the immediate first one
    /// </summary>
    public int VolleyCount { get; init; } = 5;

    /// <summary>
    ///     The number of milliseconds between each volley
    /// </summary>
    public int VolleyDelayMs { get; init; } = 500;
    #endregion
}
