#region
using System.Text;
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Displays the name, description, and stat modifiers of the item in the caster's first inventory slot in a
///     read-only notepad window. This is a debug/testing utility, not a real gameplay skill.
/// </summary>
public class InspectItemScript : ConfigurableSkillScriptBase
{
    /// <summary>
    ///     An out-of-range inventory slot used to identify this window. Inventory only has 60 real slots, so this
    ///     value can never collide with a real item - if the player edits and "saves" the window, the server just
    ///     no-ops instead of overwriting a real item's notepad text.
    /// </summary>
    private const byte NotepadIdentifier = 255;

    /// <inheritdoc />
    public InspectItemScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        if (context.Source is not Aisling source)
            return;

        if (!source.Inventory.TryGetObject(1, out var item))
        {
            source.SendOrangeBarMessage("Your first inventory slot is empty.");

            return;
        }

        source.Client.SendNotepad(NotepadIdentifier, NotepadType.White, Height, Width, BuildDescription(item));
    }

    private static string BuildDescription(Item item)
    {
        var sb = new StringBuilder();

        sb.AppendLine(item.DisplayName);

        if (!string.IsNullOrWhiteSpace(item.Template.Description))
            sb.AppendLine(item.Template.Description);

        if (item.Template.MaxDurability.HasValue)
            sb.AppendLine($"Durability: {item.CurrentDurability}/{item.Template.MaxDurability}");

        if (item.EnhancementLevel > 0)
            sb.AppendLine($"Enhancement: +{item.EnhancementLevel}");

        var modifiers = item.Modifiers;
        var statLines = new List<string>();

        AppendStat(statLines, "Str", modifiers.Str);
        AppendStat(statLines, "Dex", modifiers.Dex);
        AppendStat(statLines, "Int", modifiers.Int);
        AppendStat(statLines, "Wis", modifiers.Wis);
        AppendStat(statLines, "Con", modifiers.Con);
        AppendStat(statLines, "Ac", modifiers.Ac);
        AppendStat(statLines, "Dmg", modifiers.Dmg);
        AppendStat(statLines, "Hit", modifiers.Hit);
        AppendStat(statLines, "MagicResistance", modifiers.MagicResistance);
        AppendStat(statLines, "MaximumHp", modifiers.MaximumHp);
        AppendStat(statLines, "MaximumMp", modifiers.MaximumMp);
        AppendStat(statLines, "AtkSpeedPct", modifiers.AtkSpeedPct);
        AppendStat(statLines, "SkillDamagePct", modifiers.SkillDamagePct);
        AppendStat(statLines, "SpellDamagePct", modifiers.SpellDamagePct);
        AppendStat(statLines, "FlatSkillDamage", modifiers.FlatSkillDamage);
        AppendStat(statLines, "FlatSpellDamage", modifiers.FlatSpellDamage);

        if (statLines.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine(string.Join(", ", statLines));
        }

        return sb.ToString();
    }

    private static void AppendStat(List<string> statLines, string name, int value)
    {
        if (value != 0)
            statLines.Add($"{name} {(value > 0 ? "+" : string.Empty)}{value}");
    }

    #region ScriptVars
    /// <summary>
    ///     The height of the notepad window
    /// </summary>
    public byte Height { get; init; } = 20;

    /// <summary>
    ///     The width of the notepad window
    /// </summary>
    public byte Width { get; init; } = 40;
    #endregion
}
