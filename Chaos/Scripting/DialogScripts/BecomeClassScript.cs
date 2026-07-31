#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Menu;
using Chaos.Models.World;
using Chaos.Scripting.DialogScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
using Chaos.Utilities;
#endregion

namespace Chaos.Scripting.DialogScripts;

/// <summary>
///     Fully resets an Aisling into a fresh copy of the given class: sets Base/AdvClass, wipes skills, spells,
///     equipment, and inventory, then grants the full skill/spell kit and floor-1 gear for that class. Meant for
///     class-testing NPCs, not real gameplay - it bypasses normal learning requirements entirely. Any gear
///     templateKey that doesn't exist yet is skipped silently rather than throwing, since several classes still have
///     placeholder weapon templates pending.
/// </summary>
public class BecomeClassScript : ConfigurableDialogScriptBase
{
    private const int ResourceMpFloor = 100;
    private const string ResourceMpFloorTag = "ResourceMpFloorBonus";

    private readonly IItemFactory ItemFactory;
    private readonly ISkillFactory SkillFactory;
    private readonly ISpellFactory SpellFactory;

    /// <inheritdoc />
    public BecomeClassScript(Dialog subject, IItemFactory itemFactory, ISkillFactory skillFactory, ISpellFactory spellFactory)
        : base(subject)
    {
        ItemFactory = itemFactory;
        SkillFactory = skillFactory;
        SpellFactory = spellFactory;
    }

    /// <inheritdoc />
    public override void OnDisplaying(Aisling source)
    {
        //a. set class
        if (BaseClass.HasValue)
            source.UserStatSheet.SetBaseClass(BaseClass.Value);

        source.UserStatSheet.SetAdvClass(AdvClass ?? Chaos.DarkAges.Definitions.AdvClass.None);

        //b. clear all skills
        foreach (var skill in source.SkillBook.ToArray())
            source.SkillBook.Remove(skill.Slot);

        //c. clear all spells
        foreach (var spell in source.SpellBook.ToArray())
            source.SpellBook.Remove(spell.Slot);

        //c2. clear all active effects (buffs/forms from the old class shouldn't carry over)
        foreach (var effect in source.Effects.ToArray())
            source.Effects.Terminate(effect.Name);

        //c3. reset mp to 0 - several classes repurpose mp as a class-specific resource (kill energy, rage, chi,
        //fury, etc), so leftover mp from the old class must not carry over and be treated as already-earned resource
        source.StatSheet.SetMp(0);

        //c4. Assassin/Berserker/Valkyrie hard-cap their resource at 100 directly via SetMp (see
        //AssassinFrenzyScript/BerserkerRageScript/ValkyrieFuryScript), bypassing the normal max-mp clamp entirely -
        //but the client is sent both CurrentMp and MaximumMp (see AislingMapperProfile), and visually clamps/display
        //-glitches whenever CurrentMp exceeds MaximumMp. So max mp also needs a floor of 100 for these classes,
        //purely so the bar displays correctly - removed and recomputed on every class change so it never stacks.
        if (source.Trackers.Tags.TryRemove(ResourceMpFloorTag, out var previousBonusStr)
            && int.TryParse(previousBonusStr, out var previousBonus))
            source.StatSheet.SubtractBonus(new Attributes { MaximumMp = previousBonus });

        var advClassValue = AdvClass ?? Chaos.DarkAges.Definitions.AdvClass.None;

        if (advClassValue is Chaos.DarkAges.Definitions.AdvClass.Assassin
            or Chaos.DarkAges.Definitions.AdvClass.Berserker
            or Chaos.DarkAges.Definitions.AdvClass.Valkyrie)
        {
            var shortfall = ResourceMpFloor - (int)source.StatSheet.EffectiveMaximumMp;

            if (shortfall > 0)
            {
                source.StatSheet.AddBonus(new Attributes { MaximumMp = shortfall });
                source.Trackers.Tags[ResourceMpFloorTag] = shortfall.ToString();
            }
        }

        //d. remove weapon/armor/helmet/shield only - accessories are handled by the separate Equipment Vendor NPC and
        //are left untouched here (whether currently equipped or not)
        source.Equipment.Remove((byte)EquipmentSlot.Weapon);
        source.Equipment.Remove((byte)EquipmentSlot.Armor);
        source.Equipment.Remove((byte)EquipmentSlot.Helmet);
        source.Equipment.Remove((byte)EquipmentSlot.Shield);

        //e. clear all inventory items, except persistent utility items (essence pouch, GM teleporter, etc.)
        foreach (var item in source.Inventory.ToArray())
            if (!item.Template.NoTrade)
                source.Inventory.Remove(item.Slot);

        //f. grant all skills/spells for this class
        foreach (var templateKey in SkillTemplateKeys)
            TryGrantSkill(source, templateKey);

        foreach (var templateKey in SpellTemplateKeys)
            TryGrantSpell(source, templateKey);

        //f2. Beast Form skills (Fenrir Claw, Celestial Bolt, Basilisk Bite) stay in the SkillBook at all times but
        //are hidden from the action bar until the matching Beast Form is actually active - learning a skill
        //normally reveals it immediately, so undo that here for any that were just granted
        BeastFormSkillHelper.HideAllFormSkills(source);

        //g. give floor 1 weapon + armor (accessories are handled by the separate Equipment Vendor NPC)
        var armorTemplateKey = source.Gender == Gender.Male ? ArmorTemplateKeyMale : ArmorTemplateKeyFemale;

        foreach (var templateKey in GearTemplateKeys)
            TryGrantAndEquip(source, templateKey);

        TryGrantAndEquip(source, armorTemplateKey);

        //h. completion message
        source.SendOrangeBarMessage($"You have become a {ClassName}. Your journey begins anew.");

        source.Client.SendAttributes(StatUpdateType.Full);
        source.Client.SendUserId();
        source.Refresh(true);
    }

    private void TryGrantAndEquip(Aisling source, string? templateKey)
    {
        if (string.IsNullOrEmpty(templateKey))
            return;

        try
        {
            var item = ItemFactory.Create(templateKey);

            if (item.Template.EquipmentType is { } equipmentType and not EquipmentType.NotEquipment)
            {
                source.Equipment.TryEquip(equipmentType, item, out var displaced);

                if (displaced != null)
                    source.Inventory.TryAddToNextSlot(displaced);
            } else
                source.Inventory.TryAddToNextSlot(item);
        } catch
        {
            //gear template doesn't exist yet - skip gracefully
        }
    }

    private void TryGrantSkill(Aisling source, string templateKey)
    {
        try
        {
            var skill = SkillFactory.Create(templateKey);
            ComplexActionHelper.LearnSkill(source, skill);
        } catch
        {
            //skill template doesn't exist yet - skip gracefully
        }
    }

    private void TryGrantSpell(Aisling source, string templateKey)
    {
        try
        {
            var spell = SpellFactory.Create(templateKey);
            ComplexActionHelper.LearnSpell(source, spell);
        } catch
        {
            //spell template doesn't exist yet - skip gracefully
        }
    }

    #region ScriptVars
    /// <summary>
    ///     The gender-neutral display name of the class, used in the completion message
    /// </summary>
    public string ClassName { get; init; } = string.Empty;

    /// <summary>
    ///     The advanced class to set on the Aisling, if any
    /// </summary>
    public AdvClass? AdvClass { get; init; }

    /// <summary>
    ///     The female armor templateKey to grant
    /// </summary>
    public string? ArmorTemplateKeyFemale { get; init; }

    /// <summary>
    ///     The male armor templateKey to grant
    /// </summary>
    public string? ArmorTemplateKeyMale { get; init; }

    /// <summary>
    ///     The base class to set on the Aisling
    /// </summary>
    public BaseClass? BaseClass { get; init; }

    /// <summary>
    ///     The templateKeys of weapons/other non-armor gear to grant (may include more than one, e.g. Archer's bow)
    /// </summary>
    public ICollection<string> GearTemplateKeys { get; init; } = [];

    /// <summary>
    ///     The templateKeys of skills to grant
    /// </summary>
    public ICollection<string> SkillTemplateKeys { get; init; } = [];

    /// <summary>
    ///     The templateKeys of spells to grant
    /// </summary>
    public ICollection<string> SpellTemplateKeys { get; init; } = [];
    #endregion
}
