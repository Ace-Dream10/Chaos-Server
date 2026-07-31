#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Menu;
using Chaos.Models.World;
using Chaos.Scripting.DialogScripts.Abstractions;
#endregion

namespace Chaos.Scripting.DialogScripts;

public class SetBodyColorScript : ConfigurableDialogScriptBase
{
    /// <inheritdoc />
    public SetBodyColorScript(Dialog subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnDisplaying(Aisling source)
    {
        source.BodyColor = BodyColor;
        source.Refresh(true);
    }

    #region ScriptVars
    /// <summary>
    ///     The skin color to apply to the Aisling
    /// </summary>
    public BodyColor BodyColor { get; init; }
    #endregion
}
