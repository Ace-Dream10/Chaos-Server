#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.World;
using Chaos.Scripting.AislingScripts.Abstractions;
using Chaos.Scripting.EffectScripts;
#endregion

namespace Chaos.Scripting.AislingScripts;

/// <summary>
///     Bedrock (Ironscale passive) - a true always-on passive: "gain increasing damage reduction the longer you
///     remain stationary." Watches <see cref="Chaos.Collections.Trackers.LastWalk" /> for a change (same
///     event-diff-detection pattern CrescendoScript/SpiritualAttunementScript already established), accumulating
///     stationary time and building stacks every <see cref="StackIntervalSeconds" /> up to
///     <see cref="MaxStacks" />, applying/refreshing <see cref="BedrockEffect" /> at the current stack level. Moving
///     at all immediately resets the streak to zero. Placeholder values, not balance-tested.
/// </summary>
public class BedrockScript : AislingScriptBase
{
    private const int MaxStacks = 5;
    private const int PctPerStack = 4;
    private const int StackIntervalSeconds = 2;

    private DateTime? LastObservedWalk;
    private TimeSpan StationaryDuration;

    /// <inheritdoc />
    public BedrockScript(Aisling subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (Subject.UserStatSheet.BaseClass != BaseClass.MartialArtist)
        {
            LastObservedWalk = null;
            StationaryDuration = TimeSpan.Zero;

            return;
        }

        if (!Subject.SkillBook.TryGetObjectByTemplateKey("bedrock", out _))
            return;

        var lastWalk = Subject.Trackers.LastWalk;

        if (lastWalk != LastObservedWalk)
        {
            LastObservedWalk = lastWalk;
            StationaryDuration = TimeSpan.Zero;
            Subject.Effects.Terminate("Bedrock");

            return;
        }

        StationaryDuration += delta;

        var stacks = Math.Min(MaxStacks, (int)(StationaryDuration.TotalSeconds / StackIntervalSeconds));

        if (stacks <= 0)
            return;

        var bedrockEffect = new BedrockEffect { DamageReductionPct = stacks * PctPerStack };
        Subject.Effects.Terminate("Bedrock");
        Subject.Effects.Apply(Subject, bedrockEffect, this);
    }
}
