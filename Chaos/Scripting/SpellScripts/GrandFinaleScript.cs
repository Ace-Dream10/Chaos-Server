#region
using Chaos.Collections;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SpellScripts.Abstractions;
using Microsoft.Extensions.Logging;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     The Bard ultimate: centers a sustained damage zone on a targeted ally ("the stage") and pulses damage to
///     every hostile monster within range of them, once per tick, for a fixed number of ticks - same delayed/repeat
///     pattern as <see cref="RainOfArrowsScript" />.
/// </summary>
public class GrandFinaleScript : ConfigurableSpellScriptBase
{
    private readonly IApplyDamageScript ApplyDamageScript;
    private readonly ILogger<GrandFinaleScript> Logger;
    private readonly List<PendingPerformance> PendingPerformances = [];

    /// <inheritdoc />
    public GrandFinaleScript(Spell subject, ILogger<GrandFinaleScript> logger)
        : base(subject)
    {
        ApplyDamageScript = ApplyAttackDamageScript.Create();
        Logger = logger;
    }

    /// <inheritdoc />
    public override bool CanUse(SpellContext context)
    {
        if (!context.Source.IsAlive)
            return false;

        if ((context.TargetCreature is not Aisling { IsAlive: true } target) || !Filter.IsValidTarget(context.Source, target))
        {
            Logger.LogInformation(
                "Grand Finale: no valid ally target found - targetCreature={TargetCreature}",
                context.TargetCreature?.Name ?? "null");

            context.SourceAisling?.SendOrangeBarMessage("You must select a valid ally.");

            return false;
        }

        Logger.LogInformation("Grand Finale: valid target found, stage={Stage}", target.Name);

        return true;
    }

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var stage = (Aisling)context.TargetCreature!;
        var map = context.TargetMap;

        if (CasterAnimation != null)
            source.Animate(CasterAnimation, source.Id);
        else
            source.AnimateBody(BodyAnimation);

        //first pulse fires immediately, the rest are queued on the tick interval
        PulseDamage(source, stage, map);

        PendingPerformances.Add(
            new PendingPerformance(
                source,
                stage,
                map,
                TickCount - 1,
                TimeSpan.FromMilliseconds(TickIntervalMs)));

        Logger.LogInformation(
            "Grand Finale: scheduled {RemainingTicks} additional damage ticks every {TickIntervalMs}ms, stage={Stage}, pendingPerformances={PendingCount}",
            TickCount - 1,
            TickIntervalMs,
            stage.Name,
            PendingPerformances.Count);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (PendingPerformances.Count == 0)
            return;

        for (var i = PendingPerformances.Count - 1; i >= 0; i--)
        {
            var pending = PendingPerformances[i];
            pending.SinceLastTick += delta;

            if (pending.SinceLastTick < pending.TickInterval)
                continue;

            pending.SinceLastTick = TimeSpan.Zero;
            pending.TicksRemaining--;

            if (!pending.Source.IsAlive || !pending.Stage.IsAlive)
            {
                PendingPerformances.RemoveAt(i);

                continue;
            }

            PulseDamage(pending.Source, pending.Stage, pending.Map);

            if (pending.TicksRemaining <= 0)
                PendingPerformances.RemoveAt(i);
        }
    }

    private void PulseDamage(Creature source, Creature stage, MapInstance map)
    {
        var damage = BaseDamage + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat) * DamageStatMultiplier);

        var hostiles = map.GetEntitiesWithinRange<Monster>(stage, DamageRange)
                          .Where(monster => monster.IsAlive)
                          .ToList();

        Logger.LogInformation(
            "Grand Finale: pulse at stage={Stage} range={DamageRange} found {HostileCount} alive hostiles, damage={Damage}",
            stage.Name,
            DamageRange,
            hostiles.Count,
            damage);

        foreach (var monster in hostiles)
        {
            ApplyDamageScript.ApplyDamage(source, monster, this, damage);

            if (Animation != null)
                monster.Animate(Animation, source.Id);
        }
    }

    private sealed class PendingPerformance(Creature source, Aisling stage, MapInstance map, int ticksRemaining, TimeSpan tickInterval)
    {
        public MapInstance Map { get; } = map;
        public TimeSpan SinceLastTick { get; set; } = TimeSpan.Zero;
        public Creature Source { get; } = source;
        public Aisling Stage { get; } = stage;
        public TimeSpan TickInterval { get; } = tickInterval;
        public int TicksRemaining { get; set; } = ticksRemaining;
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each hit monster, every tick
    /// </summary>
    public Animation? Animation { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.BaseDamage" />
    public int BaseDamage { get; init; } = 40;

    /// <summary>
    ///     The body animation played by the caster, used if <see cref="CasterAnimation" /> is not set
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The grand animation played on the caster at the start of the performance
    /// </summary>
    public Animation? CasterAnimation { get; init; }

    /// <summary>
    ///     The radius around the stage (the targeted ally) damaged on each pulse
    /// </summary>
    public int DamageRange { get; init; } = 3;

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.DamageStat" />
    public Stat DamageStat { get; init; } = Stat.WIS;

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.DamageStatMultiplier" />
    public decimal DamageStatMultiplier { get; init; } = 2;

    /// <summary>
    ///     The filter used to determine whether the selected ally target is valid
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     Sound played once, on cast
    /// </summary>
    public byte? Sound { get; init; }

    /// <summary>
    ///     How many times damage pulses total, including the immediate first one
    /// </summary>
    public int TickCount { get; init; } = 10;

    /// <summary>
    ///     The number of milliseconds between each pulse
    /// </summary>
    public int TickIntervalMs { get; init; } = 1000;
    #endregion
}
