using Chaos.DarkAges.Definitions;
using Chaos.Formulae;
using Chaos.Formulae.Abstractions;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;

namespace Chaos.Scripting.FunctionalScripts.ApplyHealing;

public class ApplyHealScript : ScriptBase, IApplyHealScript
{
    /// <summary>
    ///     Encore (Bard passive) - the chance a beneficial heal repeats at reduced effectiveness, and the
    ///     multiplier applied to the repeat. Placeholder, not balance-tested.
    /// </summary>
    private const double EncoreProcChance = 0.25;

    private const decimal EncoreRepeatMultiplier = 0.4m;

    /// <inheritdoc />
    public IHealFormula HealFormula { get; set; } = HealFormulae.Default;

    public static string Key { get; } = GetScriptKey(typeof(ApplyHealScript));

    /// <inheritdoc />
    public virtual void ApplyHeal(
        Creature source,
        Creature target,
        IScript script,
        int healing)
        => ApplyHeal(source, target, script, healing, allowEncore: true);

    private void ApplyHeal(
        Creature source,
        Creature target,
        IScript script,
        int healing,
        bool allowEncore)
    {
        healing = HealFormula.Calculate(
            source,
            target,
            script,
            healing);

        if (healing <= 0)
            return;

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
            ApplyHeal(source, target, script, Convert.ToInt32(healing * EncoreRepeatMultiplier), allowEncore: false);
    }

    /// <inheritdoc />
    public static IApplyHealScript Create() => FunctionalScriptRegistry.Instance.Get<IApplyHealScript>(Key);
}
