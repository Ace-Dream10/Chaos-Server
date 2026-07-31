#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Menu;
using Chaos.Models.World;
using Chaos.Scripting.DialogScripts.Abstractions;
#endregion

namespace Chaos.Scripting.DialogScripts;

/// <summary>
///     Adds the "I have already chosen" close option to the Spirit Guide's initial dialog, but only once the player
///     has already permanently bound a <see cref="BeastFormType" />. Extends the plain <see cref="DialogScriptBase" />
///     rather than <see cref="ConfigurableDialogScriptBase" /> since it has no scriptVars - the configurable base's
///     constructor requires a scriptVars entry to exist and throws otherwise, which was breaking this dialog for
///     every player, not just Martial Artists.
/// </summary>
public class SpiritGuideInitialScript : DialogScriptBase
{
    /// <inheritdoc />
    public SpiritGuideInitialScript(Dialog subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnDisplaying(Aisling source)
    {
        if (source.Trackers.Enums.TryGetValue<BeastFormType>(out var form) && (form != BeastFormType.None))
            Subject.AddOption("I have already chosen", "close");
    }
}
