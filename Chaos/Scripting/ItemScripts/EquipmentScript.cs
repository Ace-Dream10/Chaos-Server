#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.ItemScripts.Abstractions;
#endregion

namespace Chaos.Scripting.ItemScripts;

public class EquipmentScript(Item subject) : ConfigurableItemScriptBase(subject)
{
    public override void OnUse(Aisling source)
    {
        var template = Subject.Template;

        if (!source.IsAlive)
        {
            source.SendOrangeBarMessage("You can't do that");

            return;
        }

        if (template.EquipmentType is null or EquipmentType.NotEquipment)
        {
            source.SendOrangeBarMessage("You can't equip that");

            return;
        }

        //gender check
        if (template.Gender.HasValue && !template.Gender.Value.HasFlag(source.Gender))
        {
            source.SendOrangeBarMessage($"{Subject.DisplayName} does not seem to fit you");

            return;
        }

        //only the old WeaponMaster cluster (now Berserker/Slayer/Valkyrie individually, post class-flatten) can
        //dual wield - this is a Shield-slot item flagged as an off-hand weapon, not a real shield, so the check is
        //keyed off the flag rather than EquipmentType.Shield itself
        if (IsOffHandWeapon
            && !source.HasClass(BaseClass.Berserker)
            && !source.HasClass(BaseClass.Slayer)
            && !source.HasClass(BaseClass.Valkyrie))
        {
            source.SendOrangeBarMessage("You lack the training to wield two weapons.");

            return;
        }

        if (template.Class.HasValue && !source.HasClass(template.Class.Value))
        {
            source.SendOrangeBarMessage($"{Subject.DisplayName} does not seem to fit you");

            return;
        }

        if (template.AdvClass.HasValue && (template.AdvClass.Value != source.UserStatSheet.AdvClass))
        {
            source.SendOrangeBarMessage($"{Subject.DisplayName} does not seem to fit you");

            return;
        }

        if (template.Level > source.UserStatSheet.Level)
        {
            source.SendOrangeBarMessage($"{Subject.DisplayName} does not seem to fit you, but you could grow into it");

            return;
        }

        if (template.RequiresMaster && !source.UserStatSheet.Master)
        {
            source.SendOrangeBarMessage($"{Subject.DisplayName} does not seem to fit you, but you could grow into it");

            return;
        }

        if (template.AbilityLevel > source.UserStatSheet.AbilityLevel)
        {
            source.SendOrangeBarMessage($"{Subject.DisplayName} does not seem to fit you, but you could grow into it");

            return;
        }

        if (StatRequired.HasValue
            && StatAmountRequired.HasValue
            && (source.StatSheet.GetBaseStat(StatRequired.Value) < StatAmountRequired.Value))
        {
            source.SendOrangeBarMessage($"{Subject.DisplayName} does not seem to fit you, but you could grow into it");

            return;
        }

        source.Equip(template.EquipmentType.Value, Subject);
    }

    #region ScriptVars
    /// <summary>
    ///     Whether this item is a dual-wield off-hand weapon (using the Shield slot so it actually renders)
    ///     rather than a real shield - only Weapon Master can equip these
    /// </summary>
    protected bool IsOffHandWeapon { get; init; }

    protected int? StatAmountRequired { get; init; }
    protected Stat? StatRequired { get; init; }
    #endregion
}