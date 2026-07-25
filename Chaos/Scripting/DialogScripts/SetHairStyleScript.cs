#region
using Chaos.Models.Menu;
using Chaos.Models.World;
using Chaos.Scripting.DialogScripts.Abstractions;
#endregion

namespace Chaos.Scripting.DialogScripts;

public class SetHairStyleScript : ConfigurableDialogScriptBase
{
    /// <inheritdoc />
    public SetHairStyleScript(Dialog subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnDisplaying(Aisling source)
    {
        source.HairStyle = HairStyle;
        source.Refresh(true);
    }

    #region ScriptVars
    /// <summary>
    ///     The hair style index to apply to the Aisling
    /// </summary>
    public int HairStyle { get; init; }
    #endregion
}
