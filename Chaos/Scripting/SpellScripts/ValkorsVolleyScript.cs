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

/// <summary>
///     Renamed from Rain of Arrows - kept its exact sustained-volley-on-an-area mechanic (a direct match for the
///     locked design's "unleashing a divine rain of arrows upon a target area"). One of Fletcher's 5 evolving
///     abilities. Tiers per Fletcher's own floor arc (Floor6 intro, Floor7, Floor8, Floor9 max) via the same
///     "Level ≈ 2×Floor" ratio used throughout tonight - see <see cref="GetTierValues" />. All placeholder values,
///     not balance-tested.
/// </summary>
public class ValkorsVolleyScript : ConfigurableSpellScriptBase
{
    private readonly List<PendingVolley> PendingVolleys = [];

    /// <inheritdoc />
    public ValkorsVolleyScript(Spell subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var tier = GetTierValues();

        if ((source is Aisling aisling) && !HasBowEquipped(aisling))
        {
            aisling.SendOrangeBarMessage("You need a bow equipped.");

            return;
        }

        if (!source.StatSheet.TrySubtractMp(ManaCost))
        {
            if (source is Aisling manaAisling)
                manaAisling.SendOrangeBarMessage("Not enough mana.");

            return;
        }

        if (source is Aisling attackerAisling)
            attackerAisling.Client.SendAttributes(StatUpdateType.Vitality);

        var map = context.TargetMap;
        var targetPoint = context.TargetPoint;

        source.AnimateBody(BodyAnimation);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, targetPoint);

        //volley 1 fires immediately, the rest are queued at VolleyDelayMs intervals
        FireVolley(source, map, targetPoint, tier.Range);

        for (var i = 1; i < tier.VolleyCount; i++)
            PendingVolleys.Add(
                new PendingVolley(
                    source,
                    map,
                    targetPoint,
                    tier.Range,
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

            FireVolley(pending.Source, pending.Map, pending.TargetPoint, pending.Range);
        }
    }

    private void FireVolley(Creature source, MapInstance map, Point targetPoint, int range)
    {
        var targets = map.GetEntitiesWithinRange<Creature>(targetPoint, range)
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

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor6(Level&lt;=12)=I(intro,3 volleys,range2),
    ///     Floor7(&lt;=14)=II(5,2), Floor8(&lt;=16)=III(5,3), Floor9+(&gt;16)=IV(max,7,3).
    /// </summary>
    private (int VolleyCount, int Range) GetTierValues() =>
        Subject.Level switch
        {
            <= 12 => (3, 2),
            <= 14 => (5, 2),
            <= 16 => (5, 3),
            _     => (7, 3)
        };

    private sealed class PendingVolley(Creature source, MapInstance map, Point targetPoint, int range, TimeSpan remaining)
    {
        public MapInstance Map { get; } = map;
        public int Range { get; } = range;
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
    ///     The MP cost to use this spell
    /// </summary>
    public int ManaCost { get; init; }

    /// <summary>
    ///     Sound played once, on cast
    /// </summary>
    public byte? Sound { get; init; }

    /// <summary>
    ///     The number of milliseconds between each volley
    /// </summary>
    public int VolleyDelayMs { get; init; } = 500;
    #endregion
}
