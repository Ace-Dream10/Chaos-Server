#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Common;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

public class FlechetteScript : ConfigurableSpellScriptBase
{
    private readonly List<PendingBounce> PendingBounces = [];

    /// <inheritdoc />
    public FlechetteScript(Spell subject)
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

        if ((context.TargetCreature is not { IsAlive: true } initialTarget) || !Filter.IsValidTarget(source, initialTarget))
            return;

        if (!source.StatSheet.TrySubtractMp(ManaCost))
        {
            if (source is Aisling manaAisling)
                manaAisling.SendOrangeBarMessage("Not enough focus.");

            return;
        }

        if (source is Aisling attackerAisling)
            attackerAisling.Client.SendAttributes(StatUpdateType.Vitality);

        source.AnimateBody(BodyAnimation);

        var hitTargets = new List<Creature>
        {
            initialTarget
        };

        var initialDamage = CalculateDamage(source);
        Damage(source, initialTarget, initialDamage);

        //after a short delay, bounce to the closest valid target near the initial target
        PendingBounces.Add(
            new PendingBounce(
                source,
                initialTarget,
                hitTargets,
                initialDamage,
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

            var multiplier = pending.BounceNumber == 1 ? 0.6m : 0.4m;
            Damage(pending.Source, nextTarget, Convert.ToInt32(pending.InitialDamage * multiplier));

            //queue the second bounce after the first lands
            if (pending.BounceNumber < 2)
                PendingBounces.Add(
                    new PendingBounce(
                        pending.Source,
                        nextTarget,
                        pending.HitTargets,
                        pending.InitialDamage,
                        pending.BounceNumber + 1,
                        TimeSpan.FromMilliseconds(JumpDelayMs)));
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
    ///     Finds the closest alive valid creature to <paramref name="fromTarget" /> that hasn't already been hit by this
    ///     cast
    /// </summary>
    private Creature? FindNextBounceTarget(Creature source, Creature fromTarget, ICollection<Creature> alreadyHit)
        => source.MapInstance
                 .GetEntitiesWithinRange<Creature>(fromTarget, JumpRange)
                 .Where(creature => !alreadyHit.Contains(creature) && Filter.IsValidTarget(source, creature))
                 .ClosestOrDefault(fromTarget);

    private void Damage(Creature source, Creature target, int amount)
    {
        if (amount <= 0)
            return;

        ApplyDamageScript.ApplyDamage(source, target, this, amount);

        if (Animation != null)
            target.Animate(Animation, source.Id);
    }

    private sealed class PendingBounce(
        Creature source,
        Creature fromTarget,
        List<Creature> hitTargets,
        int initialDamage,
        int bounceNumber,
        TimeSpan remaining)
    {
        public int BounceNumber { get; } = bounceNumber;
        public Creature FromTarget { get; } = fromTarget;
        public List<Creature> HitTargets { get; } = hitTargets;
        public int InitialDamage { get; } = initialDamage;
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
    ///     The filter used to find valid bounce targets, and to validate the initial target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The number of milliseconds to wait before each bounce lands
    /// </summary>
    public int JumpDelayMs { get; init; } = 400;

    /// <summary>
    ///     The MP cost to use this spell
    /// </summary>
    public int ManaCost { get; init; }

    /// <summary>
    ///     The maximum distance a bounce can travel to find its next target
    /// </summary>
    public int JumpRange { get; init; }

    /// <summary>
    ///     The sound played once, at the point of the initial cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
