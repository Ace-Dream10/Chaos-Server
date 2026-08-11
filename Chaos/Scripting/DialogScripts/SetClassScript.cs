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
        //Flat single-step selection (class-flatten project): every class sets BaseClass directly and AdvClass.None -
        //AdvClass is chosen later and separately, only for Sorcerer (elemental specialization, Floor 3/5/7) and
        //MartialArtist (Fighter/Tank/RangedChi, Floor 2). The old two-step "commit to a path, then pick a subclass"
        //flow (Guardian/Strider/Magus intermediate menus, WeaponMaster-with-no-subclass-yet) is gone, which is also
        //why there's no longer a bare "WeaponMaster, AdvClass.None" state reachable from character selection.
        var baseClass = Subject.Template.TemplateKey.ToLower() switch
        {
            "class_selector_set_bastion"       => BaseClass.Bastion,
            "class_selector_set_berserker"     => BaseClass.Berserker,
            "class_selector_set_slayer"        => BaseClass.Slayer,
            "class_selector_set_valkyrie"      => BaseClass.Valkyrie,
            "class_selector_set_assassin"      => BaseClass.Assassin,
            "class_selector_set_trickster"     => BaseClass.Trickster,
            "class_selector_set_fletcher"      => BaseClass.Fletcher,
            "class_selector_set_sorcerer"      => BaseClass.Sorcerer,
            "class_selector_set_mystic"        => BaseClass.Mystic,
            "class_selector_set_bard"          => BaseClass.Bard,
            "class_selector_set_martialartist" => BaseClass.MartialArtist,
            _                                  => (BaseClass?)null
        };

        if (baseClass is not { } resolvedBaseClass)
            return;

        source.UserStatSheet.SetBaseClass(resolvedBaseClass);
        source.UserStatSheet.SetAdvClass(AdvClass.None);

        source.Client.SendAttributes(StatUpdateType.Full);
        source.Client.SendUserId();
    }
}
