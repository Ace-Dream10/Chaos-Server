#region
using Chaos.Models.World;
using Chaos.Scripting.MonsterScripts.Abstractions;
#endregion

namespace Chaos.Scripting.MonsterScripts;

public class DecoyExpirationScript : ConfigurableMonsterScriptBase
{
    private TimeSpan Elapsed;

    /// <inheritdoc />
    public DecoyExpirationScript(Monster subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        Elapsed += delta;

        if (Elapsed >= TimeSpan.FromMilliseconds(DurationMs))
            Subject.MapInstance.RemoveEntity(Subject);
    }

    #region ScriptVars
    /// <summary>
    ///     The number of milliseconds this decoy will exist before being removed from the map
    /// </summary>
    public int DurationMs { get; set; } = 2500;
    #endregion
}
