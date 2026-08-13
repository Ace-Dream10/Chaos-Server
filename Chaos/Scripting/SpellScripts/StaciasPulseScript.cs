#region
using Chaos.Collections;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.FunctionalScripts.ApplyHealing;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

public class StaciasPulseScript : ConfigurableSpellScriptBase
{
    private readonly IApplyDamageScript ApplyDamageScript;
    private readonly IApplyHealScript ApplyHealScript;
    private readonly List<PendingHit> PendingHits = [];

    /// <inheritdoc />
    public StaciasPulseScript(Spell subject)
        : base(subject)
    {
        ApplyDamageScript = ApplyAttackDamageScript.Create();
        ApplyHealScript = ApplyNonAlertingHealScript.Create();
    }

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        if (!source.StatSheet.TrySubtractMp(ManaCost))
        {
            context.SourceAisling?.SendOrangeBarMessage("Not enough mana.");

            return;
        }

        context.SourceAisling?.Client.SendAttributes(StatUpdateType.Vitality);

        source.AnimateBody(BodyAnimation);

        var endPoint = source.DirectionalOffset(source.Direction, Range);

        var points = source.GetDirectPath(endPoint)
                            .Skip(1)
                            .TakeWhile(point => !map.IsWall(point))
                            .ToList();

        var stepDelay = TimeSpan.FromMilliseconds(StepDelayMs);
        var cumulative = TimeSpan.Zero;

        //forward pass
        foreach (var point in points)
        {
            cumulative += stepDelay;
            PendingHits.Add(new PendingHit(cumulative, source, map, point));
        }

        //return pass, same tiles (excluding the furthest point, already hit), traveling back toward the caster
        for (var i = points.Count - 2; i >= 0; i--)
        {
            cumulative += stepDelay;
            PendingHits.Add(new PendingHit(cumulative, source, map, points[i]));
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (PendingHits.Count == 0)
            return;

        for (var i = PendingHits.Count - 1; i >= 0; i--)
        {
            var pending = PendingHits[i];
            pending.Remaining -= delta;

            if (pending.Remaining > TimeSpan.Zero)
                continue;

            PendingHits.RemoveAt(i);
            HitPoint(pending.Source, pending.Map, pending.Point);
        }
    }

    private void HitPoint(Creature source, MapInstance map, Point point)
    {
        if (Animation != null)
            map.ShowAnimation(Animation.GetPointAnimation(point, source.Id));

        foreach (var creature in map.GetEntitiesAtPoints<Creature>(point))
        {
            if (!creature.IsAlive)
                continue;

            if ((creature is Monster) && Filter.IsValidTarget(source, creature))
            {
                var damage = (BaseDamage ?? 0) + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.WIS) * (DamageStatMultiplier ?? 1));
                ApplyDamageScript.ApplyDamage(source, creature, this, damage);
            } else if ((creature is Aisling) && (creature.Id != source.Id) && !source.IsHostileTo(creature))
            {
                var heal = (BaseHeal ?? 0) + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.WIS) * (HealStatMultiplier ?? 1));
                ApplyHealScript.ApplyHeal(source, creature, this, heal);
            }
        }
    }

    private sealed class PendingHit(TimeSpan remaining, Creature source, MapInstance map, Point point)
    {
        public MapInstance Map { get; } = map;
        public Point Point { get; } = point;
        public TimeSpan Remaining { get; set; } = remaining;
        public Creature Source { get; } = source;
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each traveled tile, whether or not it hits anything
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt to hostile monsters
    /// </summary>
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The flat portion of the healing done to friendly Aislings
    /// </summary>
    public int? BaseHeal { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The stat used to scale both bonus damage and bonus healing
    /// </summary>
    public Stat? DamageStat { get; init; }

    /// <summary>
    ///     The multiplier applied to <see cref="DamageStat" /> when calculating bonus damage
    /// </summary>
    public decimal? DamageStatMultiplier { get; init; }

    /// <summary>
    ///     The filter used to determine which creatures on the path are valid damage targets
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The multiplier applied to <see cref="DamageStat" /> when calculating bonus healing
    /// </summary>
    public decimal? HealStatMultiplier { get; init; }

    /// <summary>
    ///     The MP cost to use this spell
    /// </summary>
    public int ManaCost { get; init; }

    /// <summary>
    ///     The maximum number of tiles the wave travels before returning
    /// </summary>
    public int Range { get; init; } = 5;

    /// <summary>
    ///     The sound played at the caster's position on cast
    /// </summary>
    public byte? Sound { get; init; }

    /// <summary>
    ///     How long, in milliseconds, the wave takes to travel from one tile to the next
    /// </summary>
    public int StepDelayMs { get; init; } = 80;
    #endregion
}
