#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Menu;
using Chaos.Models.World;
using Chaos.Scripting.DialogScripts.Abstractions;
#endregion

namespace Chaos.Scripting.DialogScripts;

public class SetHairColorScript : ConfigurableDialogScriptBase
{
    /// <inheritdoc />
    public SetHairColorScript(Dialog subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnDisplaying(Aisling source)
    {
        source.HairColor = HairColor;
        source.Refresh(true);
    }

    #region ScriptVars
    /// <summary>
    ///     The hair color to apply to the Aisling
    /// </summary>
    public DisplayColor HairColor { get; init; }
    #endregion
}
