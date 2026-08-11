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
using Chaos.Scripting.SpellScripts.Abstractions;
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

    /// <summary>
    ///     Arcane Precision (Sorcerer Shared Tier I passive) - the damage multiplier applied on the first spell a
    ///     given Sorcerer casts against a given target. Placeholder, not balance-tested.
    /// </summary>
    private const decimal ArcanePrecisionMultiplier = 1.25m;

    /// <summary>
    ///     Kindling (Sorcerer, Fire Tier II passive) - the chance a Fire spell hit applies a Burn stack. Placeholder,
    ///     not balance-tested.
    /// </summary>
    private const double KindlingProcChance = 0.3;

    /// <summary>
    ///     Scorch (Sorcerer, Fire Tier III passive) - the damage multiplier against Burned enemies. Placeholder,
    ///     not balance-tested.
    /// </summary>
    private const decimal ScorchMultiplier = 1.2m;

    /// <summary>
    ///     Divine Verdict (Valkyrie passive) - Judgment builds 1:1 with damage taken; at this threshold, holy
    ///     lightning strikes nearby enemies and Judgment resets. Placeholder, not balance-tested.
    /// </summary>
    private const int DivineVerdictThreshold = 300;

    private const int DivineVerdictRange = 3;
    private const int DivineVerdictBurstDamage = 50;
    private const string DivineVerdictCounterKey = "divineVerdictJudgment";

    /// <summary>
    ///     Wings of Stacia (Valkyrie passive) - the HP percentage (0-1) that triggers the shield, and its cooldown
    ///     in seconds (tracked via Trackers.Counters as a ready-at Unix timestamp, not a DateTime tag - simpler to
    ///     compare with the existing int-based Counters API than parsing a stored DateTime string every hit).
    ///     Placeholder, not balance-tested.
    /// </summary>
    private const decimal WingsOfStaciaHpThreshold = 0.25m;

    private const int WingsOfStaciaCooldownSeconds = 45;
    private const string WingsOfStaciaCooldownCounterKey = "wingsOfStaciaReadyAt";

    /// <summary>
    ///     Chooser of the Slain (Valkyrie passive) - the heal percentage (of max HP) and cooldown (seconds, same
    ///     ready-at-timestamp pattern as Wings of Stacia) on landing the killing blow against a target marked by
    ///     Heavenly Strike (see <see cref="Chaos.Scripting.EffectScripts.MarkedForValhallaEffect" />). Placeholder,
    ///     not balance-tested.
    /// </summary>
    private const decimal ChooserOfTheSlainHealPct = 0.15m;

    private const int ChooserOfTheSlainCooldownSeconds = 20;
    private const string ChooserOfTheSlainCooldownCounterKey = "chooserOfTheSlainReadyAt";

    /// <summary>
    ///     Witness Elimination (Assassin passive) - the damage multiplier applied against a target with no other
    ///     hostile monster within range (i.e. "isolated"). Placeholder, not balance-tested.
    /// </summary>
    private const decimal WitnessEliminationMultiplier = 1.3m;

    private const int WitnessEliminationIsolationRange = 4;

    /// <summary>
    ///     Shadowmark (Assassin passive) - every this-many'th damaging ability an Assassin lands on the same target
    ///     applies <see cref="ShadowmarkEffect" /> to it. Per-(attacker, target) hit counter, not a global one, so
    ///     two different Assassins fighting the same target each build toward their own mark independently.
    ///     Placeholder, not balance-tested.
    /// </summary>
    private const int ShadowmarkHitsRequired = 3;

    /// <summary>
    ///     Ghost Step (Assassin) - the damage multiplier applied to the next landed hit after using Ghost Step.
    ///     Placeholder, not balance-tested.
    /// </summary>
    private const decimal GhostStepOpeningStrikeMultiplier = 1.75m;

    /// <summary>
    ///     Psychological Warfare (Trickster passive) - the damage multiplier against a target carrying any of
    ///     Trickster's 4 mental afflictions, applied by a Trickster who has learned this passive. Placeholder, not
    ///     balance-tested.
    /// </summary>
    private const decimal PsychologicalWarfareMultiplier = 1.25m;

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

        //Shadowmark (Assassin passive) - unlike Mark of the Bane, this bonus is owner-specific (stored under a
        //per-attacker tag), so it only benefits the Assassin who actually built up the mark
        if (target.Trackers.Tags.TryGetValue(ShadowmarkEffect.OwnerIdTagPrefix + source.Id, out var shadowmarkPctStr)
            && int.TryParse(shadowmarkPctStr, out var shadowmarkPct))
            damage = Convert.ToInt32(damage * (1 + (shadowmarkPct / 100m)));

        //Psychological Warfare (Trickster passive) - per the locked design's wording ("take increased damage from
        //all allies"), the bonus isn't scoped to the Trickster's own hits - any attacker benefits as long as SOME
        //active affliction on the target was applied by a Trickster who has actually learned this passive (same
        //"has learned this passive" gate Scorch/Kindling use above)
        if (TricksterAfflictions.Factories.Keys.Any(
                afflictionName => target.Effects.TryGetEffect(afflictionName, out var afflictionEffect)
                                   && (afflictionEffect!.Source is Aisling { UserStatSheet.BaseClass: BaseClass.Trickster } warfareAisling)
                                   && warfareAisling.SkillBook.TryGetObjectByTemplateKey("psychological_warfare", out _)))
            damage = Convert.ToInt32(damage * PsychologicalWarfareMultiplier);

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

        //Arcane Precision (Sorcerer Shared Tier I passive) - a true always-on passive: the first spell cast
        //against a given enemy deals increased damage. Tracked per (attacker, target) pair via a tag on the
        //target - the bonus is specific to a given enemy for a given Sorcerer, not a global "first cast ever"
        //flag, so a different Sorcerer (or the same one against a different target) still gets their own bonus.
        //Only applies to spell damage ("the first SPELL cast"), not skills/assail.
        if ((source is Aisling arcanePrecisionAisling)
            && (arcanePrecisionAisling.UserStatSheet.BaseClass == BaseClass.Sorcerer)
            && (script is ISpellScript))
        {
            var arcanePrecisionTag = $"arcane_precision_hit_{source.Id}";

            if (!target.Trackers.Tags.ContainsKey(arcanePrecisionTag))
            {
                damage = Convert.ToInt32(damage * ArcanePrecisionMultiplier);
                target.Trackers.Tags[arcanePrecisionTag] = bool.TrueString;
            }
        }

        //Scorch (Sorcerer, Fire Tier III passive) - deal increased damage to enemies affected by Burn. Gated on
        //having actually learned Scorch (not just BaseClass == Sorcerer), since different Sorcerers have
        //different Tier II/III passives depending on their element picks - see Kindling below for the same gate.
        if ((source is Aisling scorchAisling)
            && (scorchAisling.UserStatSheet.BaseClass == BaseClass.Sorcerer)
            && scorchAisling.SpellBook.ContainsByTemplateKey("scorch")
            && target.Trackers.Tags.ContainsKey(BurnEffect.StacksTag))
            damage = Convert.ToInt32(damage * ScorchMultiplier);

        //Solar Flare (Sorcerer, Fire Tier II active buff) - bonus Fire-element damage while active
        if ((source.Trackers.LastAttackElement == Element.Fire)
            && source.Trackers.Tags.TryGetValue(SolarFlareEffect.FireDamageBonusPctTag, out var solarFlarePctStr)
            && int.TryParse(solarFlarePctStr, out var solarFlarePct))
            damage = Convert.ToInt32(damage * (1 + (solarFlarePct / 100m)));

        //Kindling (Sorcerer, Fire Tier II passive) - Fire spells have a chance to apply Burn. Same "has actually
        //learned this passive" gate as Scorch above. Checked after Scorch reads Burn's presence, so this
        //particular hit's own proc doesn't retroactively grant itself Scorch's bonus. Solar Flare's own bonus
        //chance (if active) adds directly onto Kindling's base roll rather than being a second independent proc.
        if ((source is Aisling kindlingAisling)
            && (kindlingAisling.UserStatSheet.BaseClass == BaseClass.Sorcerer)
            && kindlingAisling.SpellBook.ContainsByTemplateKey("kindling")
            && (source.Trackers.LastAttackElement == Element.Fire))
        {
            var procChance = KindlingProcChance;

            if (source.Trackers.Tags.TryGetValue(SolarFlareEffect.BonusKindlingChanceTag, out var bonusChanceStr)
                && double.TryParse(bonusChanceStr, System.Globalization.CultureInfo.InvariantCulture, out var bonusChance))
                procChance += bonusChance;

            if (Random.Shared.NextDouble() < procChance)
                target.Effects.Apply(source, new BurnEffect(), script);
        }

        //Ghost Step (Assassin) - the caster's next landed hit after using Ghost Step deals bonus damage, then the
        //effect breaks (same "consumed on next hit, then terminate" shape Venom Blade used to use)
        if ((source is Aisling ghostStepAisling) && ghostStepAisling.Trackers.Tags.ContainsKey(GhostStepEffect.ReadyTag))
        {
            damage = Convert.ToInt32(damage * GhostStepOpeningStrikeMultiplier);
            ghostStepAisling.Trackers.Tags.TryRemove(GhostStepEffect.ReadyTag, out _);
            ghostStepAisling.Effects.Terminate("Ghost Step");
        }

        //Killing Intent (Assassin) - the caster's next damaging ability deals bonus damage, then the effect breaks
        if ((source is Aisling killingIntentAisling)
            && killingIntentAisling.Trackers.Tags.TryGetValue(KillingIntentEffect.ReadyTag, out var killingIntentPctStr)
            && int.TryParse(killingIntentPctStr, out var killingIntentPct))
        {
            damage = Convert.ToInt32(damage * (1 + (killingIntentPct / 100m)));
            killingIntentAisling.Trackers.Tags.TryRemove(KillingIntentEffect.ReadyTag, out _);
            killingIntentAisling.Effects.Terminate("Killing Intent");
        }

        //Witness Elimination (Assassin passive) - a true always-on passive: bonus damage against an isolated
        //target (no other hostile monster within range). Only meaningful against monsters - Aislings don't have
        //an "isolated" concept here.
        if ((source is Aisling witnessAisling)
            && (witnessAisling.UserStatSheet.BaseClass == BaseClass.Assassin)
            && (target is Monster witnessTarget))
        {
            var nearbyHostiles = witnessTarget.MapInstance
                                               .GetEntitiesWithinRange<Monster>(witnessTarget, WitnessEliminationIsolationRange)
                                               .Count(nearby => !nearby.Equals(witnessTarget) && nearby.IsAlive);

            if (nearbyHostiles == 0)
                damage = Convert.ToInt32(damage * WitnessEliminationMultiplier);
        }

        //Shadowmark (Assassin passive) - a true always-on passive: every Nth damaging ability an Assassin lands on
        //the same target applies ShadowmarkEffect to it, increasing further damage from that same Assassin. Per-
        //(attacker, target) hit counter (target-side tag, keyed by attacker id), reset once the mark is (re)applied.
        if ((source is Aisling shadowmarkAisling) && (shadowmarkAisling.UserStatSheet.BaseClass == BaseClass.Assassin))
        {
            var shadowmarkCounterKey = $"shadowmarkHits_{source.Id}";
            var hits = target.Trackers.Counters.AddOrIncrement(shadowmarkCounterKey);

            if (hits >= ShadowmarkHitsRequired)
            {
                target.Trackers.Counters.Set(shadowmarkCounterKey, 0);
                target.Effects.Apply(source, new ShadowmarkEffect { OwnerId = source.Id }, script);
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

                //Fire Shield (Sorcerer, Ignis Tier III) - same absorb shape as Stacia's Bubble above, but breaking
                //it (not just its natural expiry) also erupts - see FireShieldEffect.OnTerminated for the eruption
                //itself, which fires either way since Terminate() is called on break here.
                if (aisling.Trackers.Counters.TryGetValue(FireShieldEffect.ShieldCounter, out var fireShield) && (fireShield > 0))
                {
                    var absorbed = Math.Min(fireShield, damage);
                    damage -= absorbed;
                    fireShield -= absorbed;

                    if (fireShield <= 0)
                        aisling.Effects.Terminate("Fire Shield");
                    else
                        aisling.Trackers.Counters.Set(FireShieldEffect.ShieldCounter, fireShield);
                }

                //Divine Intervention (Valkyrie) - same absorb shape as Stacia's Bubble/Fire Shield above, no
                //eruption - pure protection.
                if (aisling.Trackers.Counters.TryGetValue(DivineInterventionShieldEffect.ShieldCounter, out var divineShield) && (divineShield > 0))
                {
                    var absorbed = Math.Min(divineShield, damage);
                    damage -= absorbed;
                    divineShield -= absorbed;

                    if (divineShield <= 0)
                        aisling.Effects.Terminate("Divine Intervention");
                    else
                        aisling.Trackers.Counters.Set(DivineInterventionShieldEffect.ShieldCounter, divineShield);
                }

                //Wings of Stacia (Valkyrie) - same absorb shape as the other shields above; the low-HP trigger
                //itself is further down, after HP is actually subtracted (needs to see the post-hit HP).
                if (aisling.Trackers.Counters.TryGetValue(WingsOfStaciaShieldEffect.ShieldCounter, out var wingsShield) && (wingsShield > 0))
                {
                    var absorbed = Math.Min(wingsShield, damage);
                    damage -= absorbed;
                    wingsShield -= absorbed;

                    if (wingsShield <= 0)
                        aisling.Effects.Terminate("Wings of Stacia");
                    else
                        aisling.Trackers.Counters.Set(WingsOfStaciaShieldEffect.ShieldCounter, wingsShield);
                }

                //Death's Conviction Tier IV (Assassin) - same absorb shape as the other shields above.
                if (aisling.Trackers.Counters.TryGetValue(DeathsConvictionShieldEffect.ShieldCounter, out var convictionShield) && (convictionShield > 0))
                {
                    var absorbed = Math.Min(convictionShield, damage);
                    damage -= absorbed;
                    convictionShield -= absorbed;

                    if (convictionShield <= 0)
                        aisling.Effects.Terminate("Death's Conviction");
                    else
                        aisling.Trackers.Counters.Set(DeathsConvictionShieldEffect.ShieldCounter, convictionShield);
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

                    //Divine Verdict (Valkyrie passive) - a true always-on passive: taking damage builds Judgment
                    //(persisted via Trackers.Counters, same simple stack-counter shape as Severance/Burn stacks
                    //elsewhere), and holy lightning strikes nearby enemies once the threshold is reached.
                    if ((aisling.UserStatSheet.BaseClass == BaseClass.Valkyrie) && aisling.IsAlive)
                    {
                        var judgment = aisling.Trackers.Counters.AddOrIncrement(DivineVerdictCounterKey, damage);

                        if (judgment >= DivineVerdictThreshold)
                        {
                            aisling.Trackers.Counters.Set(DivineVerdictCounterKey, 0);

                            var verdictMap = aisling.MapInstance;
                            var verdictPoint = Point.From(aisling);

                            foreach (var nearby in verdictMap.GetEntitiesWithinRange<Monster>(verdictPoint, DivineVerdictRange))
                            {
                                if (!nearby.IsAlive)
                                    continue;

                                ApplyDamage(aisling, nearby, script, DivineVerdictBurstDamage);

                                nearby.Animate(
                                    new Animation
                                    {
                                        TargetAnimation = 138,
                                        AnimationSpeed = 100
                                    },
                                    aisling.Id);
                            }
                        }
                    }

                    //Wings of Stacia (Valkyrie passive) - a true always-on passive: falling below a health
                    //threshold grants a divine shield, on a cooldown tracked the same ready-at-timestamp way as
                    //Chooser of the Slain below.
                    if ((aisling.UserStatSheet.BaseClass == BaseClass.Valkyrie) && aisling.IsAlive
                                                                                 && !aisling.Trackers.Counters.ContainsKey(WingsOfStaciaShieldEffect.ShieldCounter))
                    {
                        var maxHp = aisling.StatSheet.EffectiveMaximumHp;
                        var hpPct = maxHp <= 0 ? 1m : aisling.StatSheet.CurrentHp / (decimal)maxHp;
                        var nowSeconds = Convert.ToInt32(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                        var readyAtSeconds = aisling.Trackers.Counters.TryGetValue(WingsOfStaciaCooldownCounterKey, out var wingsReady) ? wingsReady : 0;

                        if ((hpPct <= WingsOfStaciaHpThreshold) && (nowSeconds >= readyAtSeconds))
                        {
                            aisling.Effects.Apply(aisling, new WingsOfStaciaShieldEffect(), script);
                            aisling.Trackers.Counters.Set(WingsOfStaciaCooldownCounterKey, nowSeconds + WingsOfStaciaCooldownSeconds);
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

                if (!monster.IsAlive)
                {
                    //Death Mark (Assassin) - if the dying monster is Death-Marked, the heal has to fire here,
                    //before OnDeath removes it from the map. Reworked from the original delayed-detonation
                    //implementation to match the locked design - see DeathMarkEffect's doc comment.
                    if (monster.Trackers.Tags.ContainsKey(DeathMarkEffect.MarkTag))
                        DeathMarkEffect.TriggerDeathMarkHeal(monster);

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

                    //Chooser of the Slain (Valkyrie passive) - a true always-on passive: defeating a MARKED enemy
                    //restores health and empowers you, on a cooldown. The ambiguity flagged when this was first
                    //built (nothing in Valkyrie's kit applied a mark) is now resolved - Heavenly Strike applies
                    //MarkedForValhallaEffect on hit, so this checks for that tag at the moment of death rather
                    //than triggering on any killing blow.
                    if ((source is Aisling chooserAisling)
                        && (chooserAisling.UserStatSheet.BaseClass == BaseClass.Valkyrie)
                        && monster.Trackers.Tags.ContainsKey(MarkedForValhallaEffect.MarkedTag))
                    {
                        var nowSeconds = Convert.ToInt32(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                        var readyAtSeconds = chooserAisling.Trackers.Counters.TryGetValue(ChooserOfTheSlainCooldownCounterKey, out var chooserReady)
                            ? chooserReady
                            : 0;

                        if (nowSeconds >= readyAtSeconds)
                        {
                            var healAmount = Convert.ToInt32(chooserAisling.StatSheet.EffectiveMaximumHp * ChooserOfTheSlainHealPct);
                            chooserAisling.StatSheet.AddHp(healAmount);
                            chooserAisling.Client.SendAttributes(StatUpdateType.Vitality);
                            chooserAisling.Effects.Apply(chooserAisling, new ChooserOfTheSlainEffect(), script);
                            chooserAisling.Trackers.Counters.Set(ChooserOfTheSlainCooldownCounterKey, nowSeconds + ChooserOfTheSlainCooldownSeconds);
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