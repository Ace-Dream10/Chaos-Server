#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Menu;
using Chaos.Models.World;
using Chaos.Scripting.DialogScripts.Abstractions;
#endregion

namespace Chaos.Scripting.DialogScripts;

/// <summary>
///     Permanently sets a Martial Artist's Floor-2 specialization (Fighter/Tank/RangedChi = Beast/Ironscale/
///     Tempest). Once set, the choice cannot be changed - see the Spirit Guide's initial dialog for the one-time
///     selection flow. Direct replacement for the old ChooseBeastFormScript (BeastFormType-flavor choice, now
///     retired) - same shape, keyed off <see cref="Chaos.DarkAges.Definitions.AdvClass" /> instead, which was
///     already reserved for exactly this purpose (Fighter=12/Tank=13/RangedChi=14, see AdvClass's own doc comment)
///     but never wired up until now.
/// </summary>
public class ChooseSpecializationScript : ConfigurableDialogScriptBase
{
    /// <inheritdoc />
    public ChooseSpecializationScript(Dialog subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnDisplaying(Aisling source)
    {
        if (source.UserStatSheet.AdvClass != AdvClass.None)
        {
            source.SendOrangeBarMessage("Your path is already chosen.");

            return;
        }

        source.UserStatSheet.SetAdvClass(Specialization);
        source.Client.SendAttributes(StatUpdateType.Full);
        source.SendOrangeBarMessage($"You have chosen the path of {DisplayName}.");
    }

    #region ScriptVars
    /// <summary>
    ///     The display name shown in the confirmation message
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    ///     The specialization to permanently set
    /// </summary>
    public AdvClass Specialization { get; init; }
    #endregion
}
