using Chaos.DarkAges.Definitions;
using Chaos.Formulae;
using Chaos.Formulae.Abstractions;
using Chaos.Models.World;
using Chaos.Scripting.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Services.Servers.Options;

namespace Chaos.Scripting.FunctionalScripts.LevelUp;

public class DefaultLevelUpScript : ScriptBase, ILevelUpScript
{
    /// <summary>
    ///     Hardcoded floor 1 level cap. Floor tracking doesn't exist yet - once it does, this should be
    ///     replaced with a per-floor lookup instead of a single constant.
    /// </summary>
    public const int FloorLevelCap = 19;

    public ILevelUpFormula LevelUpFormula { get; set; } = LevelUpFormulae.Default;

    /// <inheritdoc />
    public static string Key { get; } = GetScriptKey(typeof(DefaultLevelUpScript));

    /// <inheritdoc />
    public static ILevelUpScript Create() => FunctionalScriptRegistry.Instance.Get<ILevelUpScript>(Key);

    /// <inheritdoc />
    public virtual void LevelUp(Aisling aisling)
    {
        //defense in depth - the primary guard is in DefaultExperienceDistributionScript.GiveExp,
        //which stops feeding levels once the cap is hit. this guard protects direct callers of LevelUp.
        if (aisling.UserStatSheet.Level >= FloorLevelCap)
            return;

        aisling.UserStatSheet.AddLevel();

        //every level grants +1 to all five stats automatically - no point pool, no player choice
        aisling.UserStatSheet.GivePoints(5);
        aisling.UserStatSheet.IncrementStat(Stat.STR);
        aisling.UserStatSheet.IncrementStat(Stat.DEX);
        aisling.UserStatSheet.IncrementStat(Stat.INT);
        aisling.UserStatSheet.IncrementStat(Stat.WIS);
        aisling.UserStatSheet.IncrementStat(Stat.CON);

        if (aisling.UserStatSheet.Level < WorldOptions.Instance.MaxLevel)
        {
            var newTnl = LevelUpFormula.CalculateTnl(aisling);
            aisling.UserStatSheet.AddTnl(newTnl);
        }

        var levelUpAttribs = LevelUpFormula.CalculateAttributesIncrease(aisling);

        aisling.UserStatSheet.Add(levelUpAttribs);
        aisling.UserStatSheet.SetMaxWeight(LevelUpFormula.CalculateMaxWeight(aisling));

        aisling.Client.SendAttributes(StatUpdateType.Full);
    }
}
