#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.World;
using Chaos.Scripting.AislingScripts.Abstractions;
using Chaos.Scripting.EffectScripts;
#endregion

namespace Chaos.Scripting.AislingScripts;

/// <summary>
///     Crescendo (Bard passive) - a true always-on passive: "casting spells builds Crescendo. At maximum stacks,
///     your next spell is empowered." Watches <see cref="Chaos.Collections.Trackers.LastSpellUse" /> for a change
///     (same event-diff-detection pattern <see cref="AssassinFrenzyScript" />/<see cref="TricksterIllusionScript" />
///     established for observing an event this script doesn't own), incrementing a stack count on every cast. At
///     <see cref="MaxStacks" />, applies <see cref="CrescendoEffect" /> and resets to 0 - see that effect's doc
///     comment for why "empowered" is a generic self-buff rather than a per-spell hook. If <see cref="CrescendoEffect" />
///     is already active and another spell is cast, it's consumed (terminated) here rather than waiting out its
///     own duration, so it genuinely reads as "your NEXT spell", not "your next several seconds of spells".
///     Placeholder stack count, not balance-tested.
/// </summary>
public class CrescendoScript : AislingScriptBase
{
    private const int MaxStacks = 5;

    private int Stacks;
    private DateTime? LastObservedSpellUse;
    private bool SkipNextConsume;

    /// <inheritdoc />
    public CrescendoScript(Aisling subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (Subject.UserStatSheet.BaseClass != BaseClass.Bard)
        {
            LastObservedSpellUse = null;
            Stacks = 0;

            return;
        }

        if (!Subject.SkillBook.TryGetObjectByTemplateKey("crescendo", out _))
            return;

        var lastSpellUse = Subject.Trackers.LastSpellUse;
        var castDetected = lastSpellUse.HasValue && (lastSpellUse != LastObservedSpellUse);

        if (!castDetected)
            return;

        LastObservedSpellUse = lastSpellUse;

        //consume an already-active Crescendo buff on this new cast, rather than the buff's own duration expiring
        //naturally - the cast that just happened is the "empowered" one
        if (Subject.Effects.Contains("Crescendo"))
        {
            if (SkipNextConsume)
                SkipNextConsume = false;
            else
            {
                Subject.Effects.Terminate("Crescendo");

                return;
            }
        }

        Stacks++;

        if (Stacks < MaxStacks)
            return;

        Stacks = 0;
        SkipNextConsume = true;
        Subject.Effects.Apply(Subject, new CrescendoEffect(), this);
    }
}
