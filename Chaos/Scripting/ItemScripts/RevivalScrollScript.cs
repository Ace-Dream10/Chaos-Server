#region
using Chaos.DarkAges.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.ItemScripts.Abstractions;
#endregion

namespace Chaos.Scripting.ItemScripts;

/// <summary>
///     A consumable that revives a fallen (skulled) Aisling directly in front of the user. Cannot be used on
///     yourself - meant to be used by another player standing over a fallen ally.
/// </summary>
public class RevivalScrollScript : ConfigurableItemScriptBase
{
    /// <inheritdoc />
    public RevivalScrollScript(Item subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(Aisling source)
    {
        var targetPoint = source.DirectionalOffset(source.Direction);

        var target = source.MapInstance
                            .GetEntitiesAtPoints<Creature>(targetPoint)
                            .TopOrDefault();

        if ((target is not Aisling aislingTarget) || !aislingTarget.IsDead)
        {
            source.SendOrangeBarMessage("You must stand in front of a fallen ally to use this.");

            return;
        }

        aislingTarget.IsDead = false;
        aislingTarget.StatSheet.SetHealthPct(ReviveHpPct);
        aislingTarget.Refresh(true);

        source.Inventory.RemoveQuantityByTemplateKey(Subject.Template.TemplateKey, 1);

        if (Animation != null)
            aislingTarget.Animate(Animation, source.Id);

        if (Sound.HasValue)
            source.MapInstance.PlaySound(Sound.Value, targetPoint);

        aislingTarget.SendOrangeBarMessage($"You have been revived by {source.Name}!");
        source.SendOrangeBarMessage($"You revived {aislingTarget.Name}.");
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the revived target
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The percentage of max HP the target is revived with
    /// </summary>
    public int ReviveHpPct { get; init; } = 25;

    /// <summary>
    ///     Sound played on revive
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
