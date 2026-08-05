#region
using Chaos.DarkAges.Definitions;
using Chaos.Extensions;
using Chaos.Formulae;
using Chaos.Formulae.Abstractions;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.Abstractions;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.FunctionalScripts.Abstractions;
#endregion

namespace Chaos.Scripting.FunctionalScripts.ApplyDamage;

public class ApplyAttackDamageScript : ScriptBase, IApplyDamageScript
{
    /// <summary>
    ///     Tag key used by evolving Stacia's Shrine (tier 3+) to store the remaining absorb amount of its damage
    ///     shield on a buffed Aisling
    /// </summary>
    public const string ShrineShieldTag = "shrineShield";

    public IDamageFormula DamageFormula { get; set; } = DamageFormulae.Default;
    public static string Key { get; } = GetScriptKey(typeof(ApplyAttackDamageScript));

    public virtual int ApplyDamage(
        Creature source,
        Creature target,
        IScript script,
        int damage,
        Element? elementOverride = null)
    {
        if (!elementOverride.HasValue
            && source.StatSheet.AdaptiveOffenseElement
            && (DamageFormula is IElementalDamageFormula elementalDamageFormula))
            elementOverride = elementalDamageFormula.GetBestOffenseElement(target.StatSheet.DefenseElement);

        source.Trackers.LastAttackElement = elementOverride ?? source.StatSheet.OffenseElement;

        damage = DamageFormula.Calculate(
            source,
            target,
            script,
            damage,
            elementOverride);

        //Boiling Blood - the tagged target takes 25% increased damage from every source
        if (target.Trackers.Tags.ContainsKey(BoilingBloodEffect.BoilingBloodTag))
            damage = Convert.ToInt32(damage * 1.25m);

        if (damage <= 0)
            return 0;

        target.Trackers.LastDamagedBy = source;

        if (target is Monster)
            source.Trackers.LastDamagedEnemy = DateTime.UtcNow;

        switch (target)
        {
            case Aisling aisling:
                //Stacia's Veil - flat percentage damage reduction, applies before any other mitigation below
                if (aisling.Effects.TryGetEffect("Stacia's Veil", out var veilEffect) && (veilEffect is VeilEffect veil))
                    damage = Convert.ToInt32(damage * (1 - (veil.DamageReductionPct / 100m)));

                //Stacia's Shrine (tier 3+) damage shield - absorbs up to ShrineShieldHp of the remaining damage
                if (aisling.Trackers.Tags.TryGetValue(ShrineShieldTag, out var shieldHpStr) && int.TryParse(shieldHpStr, out var shieldHp)
                                                                                            && (shieldHp > 0))
                {
                    var absorbed = Math.Min(shieldHp, damage);
                    damage -= absorbed;
                    shieldHp -= absorbed;

                    if (shieldHp <= 0)
                        aisling.Trackers.Tags.TryRemove(ShrineShieldTag, out _);
                    else
                        aisling.Trackers.Tags[ShrineShieldTag] = shieldHp.ToString();
                }

                //Stacia's Bubble - absorbs up to the remaining shield amount of damage; terminates and shatters
                //when depleted (see StaciasBubbleEffect.OnTerminated for the counter cleanup + shatter animation)
                if (aisling.Trackers.Counters.TryGetValue(StaciasBubbleEffect.BubbleShieldCounter, out var bubbleShield) && (bubbleShield > 0))
                {
                    var absorbed = Math.Min(bubbleShield, damage);
                    damage -= absorbed;
                    bubbleShield -= absorbed;

                    if (bubbleShield <= 0)
                        aisling.Effects.Terminate("Stacia's Bubble");
                    else
                        aisling.Trackers.Counters.Set(StaciasBubbleEffect.BubbleShieldCounter, bubbleShield);
                }

                //Axe Block - fully negate incoming damage while the block window is active
                if (aisling.Trackers.Tags.ContainsKey(AxeBlockEffect.BlockingTag))
                {
                    //damage negated entirely, nothing further to apply
                } else if ((aisling.UserStatSheet.BaseClass == BaseClass.Bastion)
                    && aisling.Effects.TryGetEffect("Lancer's Shield", out var shieldEffect)
                    && (shieldEffect is LancerShieldEffect lancerShield))
                {
                    var mpCost = Convert.ToInt32(damage * lancerShield.DamageToMpRate);
                    aisling.StatSheet.SubtractMp(mpCost);
                    aisling.Client.SendAttributes(StatUpdateType.Vitality);

                    if (aisling.StatSheet.CurrentMp <= 0)
                        aisling.Effects.Terminate("Lancer's Shield");
                } else if (aisling.Trackers.Tags.ContainsKey(CounterStrikeEffect.ReadyTag))
                {
                    //Counter Strike - negate the damage entirely, root the attacker, and counter for damage scaled
                    //off the defender's DEX, one use only
                    aisling.Trackers.Tags.TryRemove(CounterStrikeEffect.ReadyTag, out _);
                    aisling.Effects.Terminate("Counter Strike");

                    var rootEffect = new RootEffect();
                    rootEffect.SetDuration(TimeSpan.FromMilliseconds(2000));
                    source.Effects.Apply(aisling, rootEffect, script);

                    var counterDamage = CounterStrikeEffect.CalculateCounterDamage(aisling);

                    if (counterDamage > 0)
                        ApplyDamage(aisling, source, script, counterDamage);

                    source.Animate(
                        new Animation
                        {
                            TargetAnimation = 14,
                            AnimationSpeed = 100
                        },
                        aisling.Id);
                } else if ((damage >= aisling.StatSheet.CurrentHp) && aisling.Trackers.Tags.ContainsKey(PhoenixRiseEffect.ReadyTag))
                {
                    //Phoenix Rise - this hit would be lethal and the buff is ready: survive at 30% HP instead,
                    //explode, and consume the buff (one use only)
                    aisling.Trackers.Tags.TryRemove(PhoenixRiseEffect.ReadyTag, out _);
                    aisling.Effects.Terminate("Phoenix Rise");

                    var surviveHp = Math.Max(1, Convert.ToInt32(aisling.StatSheet.EffectiveMaximumHp * 0.3m));
                    aisling.StatSheet.SetHp(surviveHp);
                    aisling.Client.SendAttributes(StatUpdateType.Vitality);
                    aisling.ShowHealth();

                    var map = aisling.MapInstance;
                    var point = Point.From(aisling);

                    map.ShowAnimation(
                        new Animation
                        {
                            TargetAnimation = 50,
                            AnimationSpeed = 100
                        }.GetPointAnimation(point, aisling.Id));

                    foreach (var nearby in map.GetEntitiesWithinRange<Monster>(point, 2))
                    {
                        if (!nearby.IsAlive)
                            continue;

                        ApplyDamage(aisling, nearby, script, 80);
                    }
                } else
                {
                    aisling.StatSheet.SubtractHp(damage);
                    aisling.Client.SendAttributes(StatUpdateType.Vitality);
                    aisling.ShowHealth();

                    //Lancers passively charge shield MP from monster hits while the shield isn't already up
                    if ((aisling.UserStatSheet.BaseClass == BaseClass.Bastion) && (source is Monster))
                    {
                        aisling.StatSheet.AddMp(damage);
                        aisling.Client.SendAttributes(StatUpdateType.Vitality);
                    }
                }

                aisling.Script.OnAttacked(source, damage);

                if (!aisling.IsAlive)
                    aisling.Script.OnDeath();

                break;
            case Monster monster:
                monster.StatSheet.SubtractHp(damage);
                monster.ShowHealth();
                monster.Script.OnAttacked(source, damage);

                //Stacia's Lullaby - taking any damage wakes the monster immediately
                if (monster.Trackers.Tags.ContainsKey(LullabyEffect.AsleepTag))
                    monster.Effects.Terminate("Lullaby");

                //Venom Blade - if the attacking Assassin has a venom blade ready, this hit poisons + bleeds the
                //target and consumes the buff, one use only
                if ((source is Aisling venomAisling) && venomAisling.Trackers.Tags.ContainsKey(VenomBladeEffect.ReadyTag))
                {
                    venomAisling.Trackers.Tags.TryRemove(VenomBladeEffect.ReadyTag, out _);
                    venomAisling.Effects.Terminate("Venom Blade");

                    var poisonEffect = new PoisonBombEffect();
                    poisonEffect.SetDuration(TimeSpan.FromMilliseconds(6000));
                    monster.Effects.Apply(source, poisonEffect);

                    var venomBleedEffect = new BleedEffect { BleedDamage = 15 };
                    venomBleedEffect.SetDuration(TimeSpan.FromMilliseconds(3000));
                    monster.Effects.Apply(source, venomBleedEffect);
                }

                if (!monster.IsAlive)
                {
                    //Black Lotus - if the dying monster is chain-tagged, the explosion has to fire here, before
                    //OnDeath removes it from the map
                    if (monster.Trackers.Tags.ContainsKey(BlackLotusEffect.BlackLotusTag))
                        BlackLotusEffect.TriggerChainExplosion(monster);

                    monster.Script.OnDeath();
                    source.Trackers.LastKillTime = DateTime.UtcNow;
                    source.Trackers.LastKilledMonsterMaxHp = Convert.ToInt32(monster.StatSheet.EffectiveMaximumHp);
                }

                break;
            case Merchant merchant:
                merchant.Script.OnAttacked(source, damage);

                break;
        }

        return damage;
    }

    public static IApplyDamageScript Create() => FunctionalScriptRegistry.Instance.Get<IApplyDamageScript>(Key);
}