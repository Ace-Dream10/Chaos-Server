#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Menu;
using Chaos.Models.World;
using Chaos.Scripting.DialogScripts.Abstractions;
#endregion

namespace Chaos.Scripting.DialogScripts;

/// <summary>
///     Permanently binds a Martial Artist to a chosen <see cref="BeastFormType" />. Once set, the choice cannot be
///     changed - see the Spirit Guide's initial dialog for the one-time selection flow.
/// </summary>
public class ChooseBeastFormScript : ConfigurableDialogScriptBase
{
    /// <inheritdoc />
    public ChooseBeastFormScript(Dialog subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnDisplaying(Aisling source)
    {
        if (source.Trackers.Enums.TryGetValue<BeastFormType>(out var existingForm) && (existingForm != BeastFormType.None))
        {
            source.SendOrangeBarMessage("Your spirit is already bound.");

            return;
        }

        source.Trackers.Enums.Set(Form);
        source.SendOrangeBarMessage($"The spirit of the {Form} binds to your soul.");
    }

    #region ScriptVars
    /// <summary>
    ///     The beast form to permanently bind the player to
    /// </summary>
    public BeastFormType Form { get; init; }
    #endregion
}
