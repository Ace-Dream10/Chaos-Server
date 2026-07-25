#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Menu;
using Chaos.Models.World;
using Chaos.Scripting.DialogScripts.Abstractions;
#endregion

namespace Chaos.Scripting.DialogScripts;

public class SetClassScript : DialogScriptBase
{
    /// <inheritdoc />
    public SetClassScript(Dialog subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnDisplaying(Aisling source)
    {
        //TODO: for now, players are allowed to freely reclass through this NPC. Revisit this once the class system is finalized.
        switch (Subject.Template.TemplateKey.ToLower())
        {
            case "class_selector_set_guardian":
                source.UserStatSheet.SetBaseClass(BaseClass.Lancer);
                source.UserStatSheet.SetAdvClass(AdvClass.None);

                break;
            case "class_selector_set_martialartist":
                source.UserStatSheet.SetBaseClass(BaseClass.MartialArtist);
                source.UserStatSheet.SetAdvClass(AdvClass.None);

                break;
            case "class_selector_set_weaponmaster":
                source.UserStatSheet.SetBaseClass(BaseClass.WeaponMaster);
                source.UserStatSheet.SetAdvClass(AdvClass.None);

                break;
            case "class_selector_set_assassin":
                source.UserStatSheet.SetBaseClass(BaseClass.Strider);
                source.UserStatSheet.SetAdvClass(AdvClass.Assassin);

                break;
            case "class_selector_set_trickster":
                source.UserStatSheet.SetBaseClass(BaseClass.Strider);
                source.UserStatSheet.SetAdvClass(AdvClass.Trickster);

                break;
            case "class_selector_set_archer":
                source.UserStatSheet.SetBaseClass(BaseClass.Strider);
                source.UserStatSheet.SetAdvClass(AdvClass.Archer);

                break;
            case "class_selector_set_sorcerer":
                source.UserStatSheet.SetBaseClass(BaseClass.Sorcerer);
                source.UserStatSheet.SetAdvClass(AdvClass.None);

                break;
            case "class_selector_set_mystic":
                source.UserStatSheet.SetBaseClass(BaseClass.Mystic);
                source.UserStatSheet.SetAdvClass(AdvClass.None);

                break;
            case "class_selector_set_bard":
                source.UserStatSheet.SetBaseClass(BaseClass.Magus);
                source.UserStatSheet.SetAdvClass(AdvClass.Bard);

                break;
            default:
                return;
        }

        source.Client.SendAttributes(StatUpdateType.Full);
        source.Client.SendUserId();
    }
}
