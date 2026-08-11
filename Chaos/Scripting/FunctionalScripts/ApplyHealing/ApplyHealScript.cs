using Chaos.DarkAges.Definitions;
using Chaos.Formulae;
using Chaos.Formulae.Abstractions;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.Abstractions;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.FunctionalScripts.Abstractions;

namespace Chaos.Scripting.FunctionalScripts.ApplyHealing;

public class ApplyHealScript : ScriptBase, IApplyHealScript
{
    /// <summary>
    ///     Bloomkeeper (Mystic passive) - the percentage healing-effectiveness bonus granted per active Blooming
    ///     Life the caster currently has out (see BloomingLifeEffect.ActiveCountCounterKey). Placeholder, not
    ///     balance-tested.
    /// </summary>
    private const decimal BloomkeeperPctPerActiveBloomingLife = 0.05m;

    /// <summary>
    ///     Encore (Bard passive) - the chance a beneficial heal repeats at reduced effectiveness, and the
    ///     multiplier applied to the repeat. Placeholder, not balance-tested.
    /// </summary>
    private const double EncoreProcChance = 0.25;

    private const decimal EncoreRepeatMultiplier = 0.4m;

    /// <summary>
    ///     Soul Tether (Mystic active) - the percentage of a heal that also flows through to the tethered partner.
    ///     Placeholder, not balance-tested.
    /// </summary>
    private const decimal SoulTetherSharePct = 0.3m;

    /// <inheritdoc />
    public IHealFormula HealFormula { get; set; } = HealFormulae.Default;

    public static string Key { get; } = GetScriptKey(typeof(ApplyHealScript));

    /// <inheritdoc />
    public virtual void ApplyHeal(
        Creature source,
        Creature target,
        IScript script,
        int healing)
        => ApplyHeal(source, target, script, healing, allowEncore: true, allowTether: true);

    private void ApplyHeal(
        Creature source,
        Creature target,
        IScript script,
        int healing,
        bool allowEncore,
        bool allowTether)
    {
        healing = HealFormula.Calculate(
            source,
            target,
            script,
            healing);

        //Bloomkeeper (Mystic passive) - a true always-on passive: each active Blooming Life the caster currently
        //has out increases their healing effectiveness, read directly off the counter BloomingLifeEffect maintains
        //on the caster
        if ((source is Aisling bloomkeeperAisling)
            && (bloomkeeperAisling.UserStatSheet.BaseClass == BaseClass.Mystic)
            && bloomkeeperAisling.SkillBook.TryGetObjectByTemplateKey("bloomkeeper", out _)
            && bloomkeeperAisling.Trackers.Counters.TryGetValue(BloomingLifeEffect.ActiveCountCounterKey, out var activeBlooms)
            && (activeBlooms > 0))
            healing = Convert.ToInt32(healing * (1 + (activeBlooms * BloomkeeperPctPerActiveBloomingLife)));

        if (healing <= 0)
            return;

        //Spirit Overflow (Mystic passive) - a true always-on passive: any healing received while at full Health is
        //converted into Mana instead of being wasted as overheal
        if ((target is Aisling overflowAisling)
            && (overflowAisling.UserStatSheet.BaseClass == BaseClass.Mystic)
            && overflowAisling.SkillBook.TryGetObjectByTemplateKey("spirit_overflow", out _)
            && (overflowAisling.StatSheet.CurrentHp >= overflowAisling.StatSheet.EffectiveMaximumHp))
        {
            overflowAisling.StatSheet.AddMp(healing);
            overflowAisling.Client.SendAttributes(StatUpdateType.Vitality);
            overflowAisling.ShowHealth();
            overflowAisling.Script.OnHealed(source, healing);

            return;
        }

        switch (target)
        {
            case Aisling aisling:
                aisling.StatSheet.AddHp(healing);
                aisling.Client.SendAttributes(StatUpdateType.Vitality);
                aisling.ShowHealth();
                aisling.Script.OnHealed(source, healing);

                break;
            case Monster monster:
                monster.StatSheet.AddHp(healing);
                monster.ShowHealth();
                monster.Script.OnHealed(source, healing);

                break;
            case Merchant merchant:
                merchant.Script.OnHealed(source, healing);

                break;
        }

        //Encore (Bard passive) - a true always-on passive: this heal has a chance to repeat at reduced
        //effectiveness. allowEncore:false on the repeat call itself, so a proc can't chain into more procs.
        if (allowEncore
            && (source is Aisling encoreAisling)
            && (encoreAisling.UserStatSheet.BaseClass == BaseClass.Bard)
            && encoreAisling.SkillBook.TryGetObjectByTemplateKey("encore", out _)
            && (Random.Shared.NextDouble() < EncoreProcChance))
            ApplyHeal(source, target, script, Convert.ToInt32(healing * EncoreRepeatMultiplier), allowEncore: false, allowTether: allowTether);

        //Soul Tether (Mystic active) - a percentage of healing the target receives also heals their tethered
        //partner. allowTether:false on the share call itself, since the partner's own Soul Tether effect points
        //right back at this target - without the guard, a single heal would ping-pong between the two forever.
        if (allowTether
            && target.Effects.TryGetEffect("Soul Tether", out var tetherEffect)
            && (tetherEffect is SoulTetherEffect { Partner: { IsAlive: true } partner }))
            ApplyHeal(source, partner, script, Convert.ToInt32(healing * SoulTetherSharePct), allowEncore: false, allowTether: false);
    }

    /// <inheritdoc />
    public static IApplyHealScript Create() => FunctionalScriptRegistry.Instance.Get<IApplyHealScript>(Key);
}
