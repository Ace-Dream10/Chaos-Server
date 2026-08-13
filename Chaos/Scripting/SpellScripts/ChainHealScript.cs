#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

public class ChainHealScript : ConfigurableSpellScriptBase
{
    private readonly List<PendingJump> PendingJumps = [];

    /// <inheritdoc />
    public ChainHealScript(Spell subject)
        : base(subject)
        => ApplyHealScript = FunctionalScripts.ApplyHealing.ApplyHealScript.Create();

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        if ((context.TargetCreature is not { IsAlive: true } initialTarget) || !Filter.IsValidTarget(source, initialTarget))
            return;

        if (!source.StatSheet.TrySubtractMp(ManaCost))
        {
            context.SourceAisling?.SendOrangeBarMessage("Not enough mana.");

            return;
        }

        context.SourceAisling?.Client.SendAttributes(StatUpdateType.Vitality);

        var healedTargets = new List<Creature>
        {
            initialTarget
        };

        source.AnimateBody(BodyAnimation);

        var initialHeal = CalculateInitialHeal(source);
        Heal(context, initialTarget, initialHeal);

        //after a short delay, jump to the closest ally of the initial target
        PendingJumps.Add(
            new PendingJump(
                TimeSpan.FromMilliseconds(JumpDelayMs),
                () =>
                {
                    var secondTarget = FindNextJumpTarget(context, initialTarget, healedTargets);

                    if (secondTarget == null)
                        return;

                    healedTargets.Add(secondTarget);
                    Heal(context, secondTarget, Convert.ToInt32((BaseHeal ?? 0) * 0.6m));

                    //after another short delay, jump to the closest ally of the second target
                    PendingJumps.Add(
                        new PendingJump(
                            TimeSpan.FromMilliseconds(JumpDelayMs),
                            () =>
                            {
                                var thirdTarget = FindNextJumpTarget(context, secondTarget, healedTargets);

                                if (thirdTarget != null)
                                    Heal(context, thirdTarget, Convert.ToInt32((BaseHeal ?? 0) * 0.4m));
                            }));
                }));

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (PendingJumps.Count == 0)
            return;

        for (var i = PendingJumps.Count - 1; i >= 0; i--)
        {
            var pending = PendingJumps[i];
            pending.Remaining -= delta;

            if (pending.Remaining > TimeSpan.Zero)
                continue;

            PendingJumps.RemoveAt(i);
            pending.Callback();
        }
    }

    private int CalculateInitialHeal(Creature source)
    {
        var heal = BaseHeal ?? 0;

        if (!HealStat.HasValue)
            return heal;

        var statValue = source.StatSheet.GetEffectiveStat(HealStat.Value);

        heal += HealStatMultiplier.HasValue ? Convert.ToInt32(statValue * HealStatMultiplier.Value) : statValue;

        return heal;
    }

    /// <summary>
    ///     Finds the closest alive valid creature to <paramref name="fromPoint" /> that hasn't already been healed by this
    ///     cast
    /// </summary>
    private Creature? FindNextJumpTarget(SpellContext context, Creature fromPoint, ICollection<Creature> alreadyHealed)
        => context.TargetMap
                  .GetEntitiesWithinRange<Creature>(fromPoint, JumpRange)
                  .Where(creature => !alreadyHealed.Contains(creature) && Filter.IsValidTarget(context.Source, creature))
                  .ClosestOrDefault(fromPoint);

    private void Heal(SpellContext context, Creature target, int amount)
    {
        if (amount <= 0)
            return;

        ApplyHealScript.ApplyHeal(context.Source, target, this, amount);

        if (Animation != null)
            target.Animate(Animation, context.Source.Id);
    }

    private sealed class PendingJump(TimeSpan remaining, Action callback)
    {
        public Action Callback { get; } = callback;
        public TimeSpan Remaining { get; set; } = remaining;
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each healed target
    /// </summary>
    public Animation? Animation { get; init; }

    public IApplyHealScript ApplyHealScript { get; init; }

    /// <summary>
    ///     The base amount healed on the initial target. The two jump heals are 60% and 40% of this value, unscaled by
    ///     HealStat
    /// </summary>
    public int? BaseHeal { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used to find valid jump targets, and to validate the initial target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The stat used to scale the initial heal
    /// </summary>
    public Stat? HealStat { get; init; }

    /// <summary>
    ///     The multiplier applied to HealStat for the initial heal
    /// </summary>
    public decimal? HealStatMultiplier { get; init; }

    /// <summary>
    ///     The number of milliseconds to wait before each chain jump lands
    /// </summary>
    public int JumpDelayMs { get; init; } = 750;

    /// <summary>
    ///     The MP cost to use this spell
    /// </summary>
    public int ManaCost { get; init; }

    /// <summary>
    ///     The maximum distance a jump can travel to find its next target
    /// </summary>
    public int JumpRange { get; init; }

    /// <summary>
    ///     The sound played once, at the point of the initial cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
