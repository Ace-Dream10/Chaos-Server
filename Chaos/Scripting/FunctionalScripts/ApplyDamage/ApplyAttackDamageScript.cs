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
using Chaos.Scripting.AislingScripts;
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

    /// <summary>
    ///     Scaling factor for Bastion's Retribution's AC-based reflect - carried over unchanged from the retired
    ///     "Lancer's Retribution" effect's own <c>AcScaling</c> constant.
    /// </summary>
    private const decimal BastionRetributionAcScaling = 0.005m;

    /// <summary>
    ///     Placeholder values for Hold the Line, not balance-tested: how far from a hit ally a Bastion still
    ///     benefits, and how much bonus aggro each hit grants them on the attacking monster.
    /// </summary>
    private const int HoldTheLineRange = 6;

    private const int HoldTheLineBonusAggro = 15;

    /// <summary>
    ///     Slayer's Oath - bonus damage percent granted per Severance stack currently on the target. Placeholder,
    ///     not balance-tested: +4% per stack, up to +20% at the 5-stack cap.
    /// </summary>
    private const decimal SlayersOathPctPerStack = 0.04m;

    /// <summary>
    ///     Merciless - the maximum bonus damage percent granted as the target's HP approaches 0. Placeholder, not
    ///     balance-tested.
    /// </summary>
    private const decimal MercilessMaxBonusPct = 0.3m;

    /// <summary>
    ///     Overkill - how far from the killed monster the "nearest enemy" search looks for a target to roll excess
    ///     damage into. Placeholder, not balance-tested.
    /// </summary>
    private const int OverkillRange = 5;

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

        //Mark of the Bane (Slayer) - the marked target takes increased damage from every source, per the tag's
        //own stored percentage (evolves per tier, unlike Boiling Blood's fixed 25%)
        if (target.Trackers.Tags.TryGetValue(MarkOfTheBaneEffect.BonusDamagePctTag, out var markPctStr) && int.TryParse(markPctStr, out var markPct))
            damage = Convert.ToInt32(damage * (1 + (markPct / 100m)));

        //Slayer's Oath (Slayer passive) - a true always-on passive, stateless like Bastion's Retribution: the
        //longer you focus one enemy (the more Severance stacks it's carrying), the stronger your attacks against
        //it become. Reuses Severance stacks as the "focus" proxy rather than inventing separate tracking.
        if ((source is Aisling oathAisling)
            && (oathAisling.UserStatSheet.BaseClass == BaseClass.Slayer)
            && target.Trackers.Tags.TryGetValue(SeveranceEffect.StacksTag, out var oathStacksStr)
            && int.TryParse(oathStacksStr, out var oathStacks)
            && (oathStacks > 0))
            damage = Convert.ToInt32(damage * (1 + (oathStacks * SlayersOathPctPerStack)));

        //Merciless (Slayer passive) - a true always-on passive: the lower an enemy's health, the more damage you
        //deal to it. Scales linearly from 0% bonus at full HP up to MercilessMaxBonusPct as HP approaches 0.
        if ((source is Aisling mercilessAisling) && (mercilessAisling.UserStatSheet.BaseClass == BaseClass.Slayer))
        {
            var targetMaxHp = target.StatSheet.EffectiveMaximumHp;

            if (targetMaxHp > 0)
            {
                var targetHpPct = target.StatSheet.CurrentHp / (decimal)targetMaxHp;
                var mercilessBonusPct = MercilessMaxBonusPct * (1 - targetHpPct);
                damage = Convert.ToInt32(damage * (1 + mercilessBonusPct));
            }
        }

        if (damage <= 0)
            return 0;

        target.Trackers.LastDamagedBy = source;

        if (target is Monster)
            source.Trackers.LastDamagedEnemy = DateTime.UtcNow;

        switch (target)
        {
            case Aisling aisling:
                //Stacia's Bulwark (Bastion, renamed from "Perfect Stand") - true temporary invulnerability, checked
                //before any other mitigation and short-circuiting the entire hit; see StaciasBulwarkEffect's doc
                //comment for why this is a tag check rather than an AC trick.
                if (aisling.Trackers.Tags.ContainsKey(StaciasBulwarkEffect.InvulnerableTag))
                {
                    aisling.Animate(
                        new Animation
                        {
                            TargetAnimation = 157,
                            AnimationSpeed = 100
                        },
                        aisling.Id);

                    aisling.Script.OnAttacked(source, 0);

                    break;
                }

                //Hold the Line (Bastion passive) - a true always-on passive, stateless like Bastion's Retribution
                //above (no AislingScript needed - this is purely reactive to an event this pipeline already sees,
                //not something that needs periodic ticking). Whenever a monster hits an ally, any nearby Bastion
                //gets bonus threat toward that monster, per the design's "enemies attacking nearby allies
                //generate increased threat toward you". Hooks directly into the existing per-monster AggroList
                //system (see AggroTargetingScript) rather than inventing a separate threat mechanism. Placeholder
                //range/amount, not balance-tested.
                if (source is Monster attackingMonster)
                    foreach (var nearbyBastion in aisling.MapInstance.GetEntitiesWithinRange<Aisling>(aisling, HoldTheLineRange))
                    {
                        if ((nearbyBastion.UserStatSheet.BaseClass != BaseClass.Bastion) || (nearbyBastion.Id == aisling.Id))
                            continue;

                        attackingMonster.AggroList.AddAggro(nearbyBastion, HoldTheLineBonusAggro);
                    }

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

                if ((aisling.UserStatSheet.BaseClass == BaseClass.Bastion)
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
                } else if (aisling.Trackers.Tags.ContainsKey(IronReprisalEffect.ReadyTag))
                {
                    //Iron Reprisal (Bastion, renamed from "Counter") - negate the damage entirely, root the
                    //attacker, and counter for damage scaled off the defender's STR, one use only. Same shape as
                    //Counter Strike above, cloned rather than shared so it has its own ready-tag and tooltip name.
                    aisling.Trackers.Tags.TryRemove(IronReprisalEffect.ReadyTag, out _);
                    aisling.Effects.Terminate("Iron Reprisal");

                    var rootEffect = new RootEffect();
                    rootEffect.SetDuration(TimeSpan.FromMilliseconds(2000));
                    source.Effects.Apply(aisling, rootEffect, script);

                    var counterDamage = IronReprisalEffect.CalculateCounterDamage(aisling);

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
                } else if ((damage >= aisling.StatSheet.CurrentHp)
                    && aisling.Trackers.Tags.ContainsKey(BerserkerUnbrokenScript.ReadyTag))
                {
                    //Unbroken (Berserker passive, always-on - see BerserkerUnbrokenScript) - this hit would be
                    //lethal and the passive is armed: survive at 1 HP instead, consume the arming tag.
                    //BerserkerUnbrokenScript re-arms it automatically after its own cooldown, unlike Phoenix Rise
                    //(a castable buff, one use per cast) this never needs to be reapplied - that's the actual
                    //difference between an activated defensive cooldown and a true always-on passive.
                    aisling.Trackers.Tags.TryRemove(BerserkerUnbrokenScript.ReadyTag, out _);
                    aisling.StatSheet.SetHp(1);
                    aisling.Client.SendAttributes(StatUpdateType.Vitality);
                    aisling.ShowHealth();
                    aisling.SendOrangeBarMessage("You refuse to fall.");

                    aisling.Animate(
                        new Animation
                        {
                            TargetAnimation = 24,
                            AnimationSpeed = 100
                        },
                        aisling.Id);
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

                    //Bastion's Retribution (renamed from "Lancer's Retribution") - a TRUE always-on passive, not a
                    //learnable cooldown-gated skill (that was the original, incorrect build - same class of
                    //mistake Unbroken had before its own correction). Unlike Rage/Unbroken, this needs no
                    //AislingScript/tag/Update() at all: it's stateless, computed fresh on every hit directly here
                    //rather than via the old effect's periodic HP-snapshot workaround (that workaround existed
                    //because effects have no hook into their own subject being attacked - this pipeline IS that
                    //hook, so there's nothing left to poll for). Reflects a portion of the damage just taken back
                    //at the attacker, scaled by the Bastion's current AC. Placeholder scaling, not balance-tested.
                    //Sign note (bug found and fixed during this conversion, not carried over from the old effect):
                    //AC is inverted in this engine (lower/negative = stronger defense - see BerserkerGateScript's
                    //doc comment), so the multiplier uses -EffectiveAc, not EffectiveAc directly - the original
                    //"Lancer's Retribution" effect used the un-negated value, which meant a well-defended (negative
                    //AC) Bastion would always compute a negative reflect and clamp to 0 via the Math.Max below,
                    //silently never reflecting anything for exactly the characters its own flavor text ("the
                    //stronger your defense, the harder the counter") was supposed to reward most.
                    if ((aisling.UserStatSheet.BaseClass == BaseClass.Bastion) && aisling.IsAlive)
                    {
                        var attacker = aisling.Trackers.LastDamagedBy;

                        if ((attacker != null) && attacker.IsAlive && (attacker.Id != aisling.Id))
                        {
                            var reflectDamage = Math.Max(0, Convert.ToInt32(damage * (-aisling.StatSheet.EffectiveAc * BastionRetributionAcScaling)));

                            if (reflectDamage > 0)
                            {
                                ApplyDamage(aisling, attacker, script, reflectDamage);

                                attacker.Animate(
                                    new Animation
                                    {
                                        TargetAnimation = 24,
                                        AnimationSpeed = 100
                                    },
                                    aisling.Id);
                            }
                        }
                    }
                }

                aisling.Script.OnAttacked(source, damage);

                if (!aisling.IsAlive)
                    aisling.Script.OnDeath();

                break;
            case Monster monster:
                //Overkill (Slayer passive) needs the pre-hit HP to compute excess damage below - captured here
                //since SubtractHp clamps at 0 and the original value would otherwise be lost
                var monsterHpBeforeHit = monster.StatSheet.CurrentHp;

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

                    //Overkill (Slayer passive) - a true always-on passive: excess damage from a killing blow rolls
                    //into the nearest enemy. Has to fire here too, before OnDeath removes the monster from the map
                    //and its position becomes unavailable for the nearby-enemy scan.
                    if ((source is Aisling overkillAisling) && (overkillAisling.UserStatSheet.BaseClass == BaseClass.Slayer))
                    {
                        var excessDamage = damage - monsterHpBeforeHit;

                        if (excessDamage > 0)
                        {
                            var nearestEnemy = monster.MapInstance
                                                      .GetEntitiesWithinRange<Monster>(monster, OverkillRange)
                                                      .Where(nearby => nearby.IsAlive && (nearby != monster))
                                                      .ClosestOrDefault(monster);

                            if (nearestEnemy != null)
                                ApplyDamage(source, nearestEnemy, script, excessDamage);
                        }
                    }

                    monster.Script.OnDeath();
                    source.Trackers.LastKillTime = DateTime.UtcNow;
                    source.Trackers.LastKilledMonsterMaxHp = Convert.ToInt32(monster.StatSheet.EffectiveMaximumHp);
                }

                break;
            case Merchant merchant:
                merchant.Script.OnAttacked(source, damage);

                break;
        }

        //Slayer lifesteal (Cold Blood / Crimson Harvest) - heals the source for a percentage of the damage just
        //dealt, regardless of target type. The two tags stack additively if both happen to be active at once.
        if ((source is Aisling lifestealAisling) && lifestealAisling.IsAlive)
        {
            var lifestealPct = 0;

            if (lifestealAisling.Trackers.Tags.TryGetValue(ColdBloodEffect.LifestealTag, out var coldBloodPctStr) && int.TryParse(coldBloodPctStr, out var coldBloodPct))
                lifestealPct += coldBloodPct;

            if (lifestealAisling.Trackers.Tags.TryGetValue(CrimsonHarvestEffect.LifestealTag, out var harvestPctStr) && int.TryParse(harvestPctStr, out var harvestPct))
                lifestealPct += harvestPct;

            if (lifestealPct > 0)
            {
                var healAmount = Convert.ToInt32(damage * (lifestealPct / 100m));

                if (healAmount > 0)
                {
                    lifestealAisling.StatSheet.AddHp(healAmount);
                    lifestealAisling.Client.SendAttributes(StatUpdateType.Vitality);
                }
            }
        }

        return damage;
    }

    public static IApplyDamageScript Create() => FunctionalScriptRegistry.Instance.Get<IApplyDamageScript>(Key);
}