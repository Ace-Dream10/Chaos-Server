using Chaos.DarkAges.Definitions;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.MonsterScripts.Abstractions;

namespace Chaos.Scripting.MonsterScripts;

/// <summary>
///     Gives the monster a fixed defense element, for content/testing purposes (no template field for this exists,
///     since no monster content has needed one until now). Also chants the element it was just hit with, for
///     visually testing elemental gear.
/// </summary>
public class SetDefenseElementScript : ConfigurableMonsterScriptBase
{
    /// <inheritdoc />
    public SetDefenseElementScript(Monster subject)
        : base(subject)
        => subject.StatSheet.SetDefenseElement(DefenseElement);

    /// <inheritdoc />
    public override void OnAttacked(Creature source, int damage, int? aggroOverride)
    {
        var element = source.Trackers.LastAttackElement;

        if (element.HasValue && (element.Value != Element.None))
            Subject.Chant($"{element.Value}!");
    }

    #region ScriptVars
    public Element DefenseElement { get; init; }
    #endregion
}
