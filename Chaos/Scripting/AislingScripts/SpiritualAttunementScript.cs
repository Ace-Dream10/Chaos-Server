#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.World;
using Chaos.Scripting.AislingScripts.Abstractions;
using Chaos.Scripting.EffectScripts;
#endregion

namespace Chaos.Scripting.AislingScripts;

/// <summary>
///     Spiritual Attunement (Mystic passive) - a true always-on passive: "after casting a healing spell, your next
///     non-healing spell is empowered." Same LastSpellUse-diff-detection pattern as
///     <see cref="Chaos.Scripting.AislingScripts.CrescendoScript" />, applied to a healing-vs-non-healing
///     distinction instead of a stack count: <see cref="HealingSpellTemplateKeys" /> is a scoped, curated set of
///     Mystic's own actually-healing spells (Blooming Life, Stacia's Shrine, Communion Rite, Stacia's Pulse) rather
///     than an attempt at a game-wide "is this spell a heal" flag, same scoping approach as every other curated-list
///     mechanic built tonight. A healing cast applies <see cref="SpiritualAttunementEffect" /> (or refreshes it if
///     already up - healing casts never consume their own buff); the very next NON-healing cast consumes it,
///     regardless of how many healing casts happened in between.
/// </summary>
public class SpiritualAttunementScript : AislingScriptBase
{
    private static readonly HashSet<string> HealingSpellTemplateKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "blooming_life",
        "stacias_shrine",
        "communion_rite",
        "stacias_pulse"
    };

    private DateTime? LastObservedSpellUse;

    /// <inheritdoc />
    public SpiritualAttunementScript(Aisling subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (Subject.UserStatSheet.BaseClass != BaseClass.Mystic)
        {
            LastObservedSpellUse = null;

            return;
        }

        if (!Subject.SkillBook.TryGetObjectByTemplateKey("spiritual_attunement", out _))
            return;

        var lastSpellUse = Subject.Trackers.LastSpellUse;
        var castDetected = lastSpellUse.HasValue && (lastSpellUse != LastObservedSpellUse);

        if (!castDetected)
            return;

        LastObservedSpellUse = lastSpellUse;

        var castSpell = Subject.Trackers.LastUsedSpell;
        var wasHealingCast = (castSpell != null) && HealingSpellTemplateKeys.Contains(castSpell.Template.TemplateKey);

        if (wasHealingCast)
        {
            Subject.Effects.Terminate("Spiritual Attunement");
            Subject.Effects.Apply(Subject, new SpiritualAttunementEffect(), this);

            return;
        }

        //a non-healing spell was just cast - if attuned, this is the empowered cast, so consume the buff now
        if (Subject.Effects.Contains("Spiritual Attunement"))
            Subject.Effects.Terminate("Spiritual Attunement");
    }
}
