#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Menu;
using Chaos.Models.World;
using Chaos.Scripting.DialogScripts.Abstractions;
#endregion

namespace Chaos.Scripting.DialogScripts;

public class SetGenderScript : ConfigurableDialogScriptBase
{
    /// <inheritdoc />
    public SetGenderScript(Dialog subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnDisplaying(Aisling source)
    {
        source.Gender = Gender;
        source.BodySprite = Gender == Gender.Male ? BodySprite.Male : BodySprite.Female;
        source.Refresh(true);
    }

    #region ScriptVars
    /// <summary>
    ///     The gender to apply to the Aisling
    /// </summary>
    public Gender Gender { get; init; }
    #endregion
}
