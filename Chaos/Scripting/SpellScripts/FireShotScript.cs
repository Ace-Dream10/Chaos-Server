#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     Fire Shot with an explicit Archer "focus" MP cost and message, layered on top of the shared
///     <see cref="DamageScript" /> logic. A dedicated subclass instead of the generic manaCost scriptVar because
///     <see cref="Chaos.Scripting.Components.AbilityComponents.ManaCostAbilityComponent" /> fails silently on
///     insufficient mana, and that shared component is used by many non-Archer skills/spells that shouldn't get an
///     Archer-specific message.
/// </summary>
public class FireShotScript : DamageScript
{
    /// <inheritdoc />
    public FireShotScript(Spell subject)
        : base(subject) { }

    /// <summary>
    ///     The MP cost to use this spell
    /// </summary>
    public int ManaCost { get; init; }

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;

        if (!source.StatSheet.TrySubtractMp(ManaCost))
        {
            context.SourceAisling?.SendOrangeBarMessage("Not enough focus.");

            return;
        }

        context.SourceAisling?.Client.SendAttributes(StatUpdateType.Vitality);

        base.OnUse(context);
    }
}
