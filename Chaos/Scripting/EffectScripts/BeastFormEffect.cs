#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
using Chaos.Time;
using Chaos.Time.Abstractions;
using Chaos.Utilities;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     While active, transforms a Martial Artist into their chosen beast spirit form (set permanently via the Spirit
///     Guide), changing their sprite and granting form-specific stat bonuses. Drains MP once per second at
///     <c>5 + (MaximumMp x 0.005)</c> - a 5/sec floor for low MP pools that scales up slowly as MaximumMp grows, so
///     stacking MaxMp on gear rewards longer form duration. Terminates itself once MP hits 0 by zeroing its own
///     <see cref="Remaining" /> rather than calling <see cref="Chaos.Collections.EffectsBar.Terminate" /> directly,
///     since that would re-enter the same lock <see cref="Chaos.Collections.EffectsBar.Update" /> is already holding
///     while ticking this effect. Also reveals/hides the caster's form-specific skills (Fenrir Claw, Celestial Bolt,
///     Basilisk Bite - tagged via <see cref="BeastFormSkillHelper" />) in their action bar for the duration - those
///     skills stay in the SkillBook the whole time, only their client-side pane visibility toggles.
/// </summary>
public sealed class BeastFormEffect : IntervalEffectBase
{
    private const string OriginalSpriteTag = "OriginalSprite";

    private static readonly Animation TransformAnimation = new()
    {
        TargetAnimation = 14,
        AnimationSpeed = 100
    };

    private Attributes AppliedBonus = new();
    private BeastFormType Form;

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(10);

    /// <inheritdoc />
    protected override IIntervalTimer Interval { get; } = new IntervalTimer(TimeSpan.FromMilliseconds(1000));

    /// <inheritdoc />
    public override byte Icon => 59;

    /// <inheritdoc />
    public override string Name => "Beast Form";

    /// <inheritdoc />
    public override void OnApplied()
    {
        if (!Subject.Trackers.Enums.TryGetValue<BeastFormType>(out var form) || (form == BeastFormType.None))
        {
            //shouldn't happen, BeastFormScript already checks this before applying
            Remaining = TimeSpan.Zero;

            return;
        }

        Form = form;

        //placeholder: tiering by character Level until floor progression is tracked - once that lands, switch this
        //to source.Trackers.Counters.Get("currentFloor") so tier reflects floor progress instead
        var tier = Subject.StatSheet.Level switch
        {
            <= 7  => 1,
            <= 14 => 2,
            _     => 3
        };

        var sprite = (Form, tier) switch
        {
            (BeastFormType.Fenrir, 1)    => (ushort)425,
            (BeastFormType.Fenrir, 2)    => (ushort)426,
            (BeastFormType.Fenrir, 3)    => (ushort)427,
            (BeastFormType.Celestial, 1) => (ushort)431,
            (BeastFormType.Celestial, 2) => (ushort)432,
            (BeastFormType.Celestial, 3) => (ushort)433,
            (BeastFormType.Basilisk, 1)  => (ushort)428,
            (BeastFormType.Basilisk, 2)  => (ushort)429,
            (BeastFormType.Basilisk, 3)  => (ushort)430,
            _                            => (ushort)0
        };

        AppliedBonus = Form switch
        {
            BeastFormType.Fenrir    => new Attributes { Str = 20, FlatSkillDamage = 25 },
            BeastFormType.Celestial => new Attributes { Wis = 20, Int = 15, FlatSpellDamage = 25 },
            BeastFormType.Basilisk  => new Attributes { Str = 10, Con = 15, Wis = 10 },
            _                       => new Attributes()
        };

        if (AislingSubject is not null)
        {
            AislingSubject.Trackers.Tags[OriginalSpriteTag] = AislingSubject.Sprite.ToString();
            AislingSubject.SetSprite(sprite);
        }

        Subject.StatSheet.AddBonus(AppliedBonus);
        Subject.Animate(TransformAnimation, Subject.Id);
        Subject.Display();

        if (AislingSubject is not null)
            foreach (var skill in AislingSubject.SkillBook)
                if (BeastFormSkillHelper.MatchesForm(skill, Form))
                    AislingSubject.Client.SendAddSkillToPane(skill);
    }

    /// <inheritdoc />
    protected override void OnIntervalElapsed()
    {
        var drainPerSecond = 5 + (Subject.StatSheet.MaximumMp * 0.005);
        Subject.StatSheet.SubtractMp(Convert.ToInt32(drainPerSecond));
        AislingSubject?.Client.SendAttributes(StatUpdateType.Vitality);

        if (Subject.StatSheet.CurrentMp <= 0)
        {
            Remaining = TimeSpan.Zero;

            return;
        }

        if (Form == BeastFormType.Basilisk)
        {
            var healAmount = (int)(Subject.StatSheet.EffectiveCon * 0.1);
            Subject.StatSheet.AddHp(healAmount);
            AislingSubject?.Client.SendAttributes(StatUpdateType.Vitality);
        }
    }

    /// <inheritdoc />
    public override void OnTerminated()
    {
        Subject.StatSheet.SubtractBonus(AppliedBonus);

        if (AislingSubject is not null)
        {
            if (AislingSubject.Trackers.Tags.TryRemove(OriginalSpriteTag, out var originalSpriteStr)
                && ushort.TryParse(originalSpriteStr, out var originalSprite))
                AislingSubject.SetSprite(originalSprite);

            foreach (var skill in AislingSubject.SkillBook)
                if (BeastFormSkillHelper.MatchesForm(skill, Form))
                    AislingSubject.Client.SendRemoveSkillFromPane(skill.Slot);
        }

        AislingSubject?.SendActiveMessage("Your beast form fades.");
        Subject.Display();
    }
}
