#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Focus with an explicit Archer "focus" MP cost and message, layered on top of the shared
///     <see cref="ApplyEffectScript" /> logic. See
///     <see cref="Chaos.Scripting.SpellScripts.FireShotScript" /> for why this is a subclass rather than the generic
///     manaCost scriptVar.
/// </summary>
public class ArcherFocusScript : ApplyEffectScript
{
    /// <inheritdoc />
    public ArcherFocusScript(Skill subject, IEffectFactory effectFactory)
        : base(subject, effectFactory) { }

    /// <summary>
    ///     The MP cost to use this skill
    /// </summary>
    public int ManaCost { get; init; }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;

        if (!source.StatSheet.TrySubtractMp(ManaCost))
        {
            if (source is Aisling manaAisling)
                manaAisling.SendOrangeBarMessage("Not enough focus.");

            return;
        }

        if (source is Aisling attackerAisling)
            attackerAisling.Client.SendAttributes(StatUpdateType.Vitality);

        base.OnUse(context);
    }
}
