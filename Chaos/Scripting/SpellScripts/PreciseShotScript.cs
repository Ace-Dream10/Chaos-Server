#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     Precise Shot with an explicit Archer "focus" MP cost and message, layered on top of the shared
///     <see cref="DamageScript" /> logic. See <see cref="FireShotScript" /> for why this is a subclass rather than
///     the generic manaCost scriptVar.
/// </summary>
public class PreciseShotScript : DamageScript
{
    /// <inheritdoc />
    public PreciseShotScript(Spell subject)
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
