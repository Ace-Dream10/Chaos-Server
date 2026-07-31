#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Common;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     Same chain-bounce pattern as Flechette, but a longer chain: full damage on the initial target, then 3 more
///     bounces to the closest not-yet-hit hostile at 70%, 50%, and 30% damage.
/// </summary>
public class ChainBounceScript : ConfigurableSpellScriptBase
{
    private static readonly decimal[] Multipliers = [1.0m, 0.7m, 0.5m, 0.3m];

    private readonly List<PendingBounce> PendingBounces = [];

    /// <inheritdoc />
    public ChainBounceScript(Spell subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;

        if ((context.TargetCreature is not { IsAlive: true } initialTarget) || !Filter.IsValidTarget(source, initialTarget))
            return;

        source.AnimateBody(BodyAnimation);

        var hitTargets = new List<Creature>
        {
            initialTarget
        };

        var baseDamage = CalculateDamage(source);
        Damage(source, initialTarget, Convert.ToInt32(baseDamage * Multipliers[0]));

        PendingBounces.Add(
            new PendingBounce(
                source,
                initialTarget,
                hitTargets,
                baseDamage,
                1,
                TimeSpan.FromMilliseconds(JumpDelayMs)));

        if (Sound.HasValue)
            context.TargetMap.PlaySound(Sound.Value, context.SourcePoint);
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (PendingBounces.Count == 0)
            return;

        for (var i = PendingBounces.Count - 1; i >= 0; i--)
        {
            var pending = PendingBounces[i];
            pending.Remaining -= delta;

            if (pending.Remaining > TimeSpan.Zero)
                continue;

            PendingBounces.RemoveAt(i);

            if (!pending.Source.IsAlive)
                continue;

            var nextTarget = FindNextBounceTarget(pending.Source, pending.FromTarget, pending.HitTargets);

            if (nextTarget == null)
                continue;

            pending.HitTargets.Add(nextTarget);

            var multiplierIndex = Math.Min(pending.BounceNumber, Multipliers.Length - 1);
            Damage(pending.Source, nextTarget, Convert.ToInt32(pending.BaseDamage * Multipliers[multiplierIndex]));

            if (pending.BounceNumber < (Multipliers.Length - 1))
                PendingBounces.Add(
                    new PendingBounce(
                        pending.Source,
                        nextTarget,
                        pending.HitTargets,
                        pending.BaseDamage,
                        pending.BounceNumber + 1,
                        TimeSpan.FromMilliseconds(JumpDelayMs)));
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

    private Creature? FindNextBounceTarget(Creature source, Creature fromTarget, ICollection<Creature> alreadyHit)
        => source.MapInstance
                 .GetEntitiesWithinRange<Creature>(fromTarget, JumpRange)
                 .Where(creature => !alreadyHit.Contains(creature) && Filter.IsValidTarget(source, creature))
                 .ClosestOrDefault(fromTarget);

    private void Damage(Creature source, Creature target, int amount)
    {
        if (amount <= 0)
            return;

        ApplyDamageScript.ApplyDamage(source, target, this, amount, Element);

        if (Animation != null)
            target.Animate(Animation, source.Id);
    }

    private sealed class PendingBounce(
        Creature source,
        Creature fromTarget,
        List<Creature> hitTargets,
        int baseDamage,
        int bounceNumber,
        TimeSpan remaining)
    {
        public int BaseDamage { get; } = baseDamage;
        public int BounceNumber { get; } = bounceNumber;
        public Creature FromTarget { get; } = fromTarget;
        public List<Creature> HitTargets { get; } = hitTargets;
        public TimeSpan Remaining { get; set; } = remaining;
        public Creature Source { get; } = source;
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each creature hit
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
    ///     The element of the damage dealt
    /// </summary>
    public Element? Element { get; init; }

    /// <summary>
    ///     The filter used to find valid bounce targets, and to validate the initial target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The number of milliseconds to wait before each bounce lands
    /// </summary>
    public int JumpDelayMs { get; init; } = 300;

    /// <summary>
    ///     The maximum distance a bounce can travel to find its next target
    /// </summary>
    public int JumpRange { get; init; } = 4;

    /// <summary>
    ///     The sound played once, at the point of the initial cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
