#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Scripting.EffectScripts.Abstractions;
using Chaos.Time;
using Chaos.Time.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     One of the Martial Artist's 2 shared evolving abilities (alongside Meditate) - transforms the caster into
///     their chosen specialization's martial form (Beast/Ironscale/Tempest, per <see cref="AdvClass" />), floor-
///     gated at I/II/III/IV (Floors 2/4/7/10, per ELYSIUM_CLASS_DESIGN.md's locked Floor Schedule - Form and
///     Meditate are DESYNCED, not moving in lockstep; Form caps at IV "Final Form" on Floor 10). Direct replacement
///     for the old BeastFormEffect (permanent Fenrir/Celestial/Basilisk flavor choice via the Spirit Guide, now
///     retired) - this version is driven by the specialization the player already chose via the SAME Spirit Guide
///     flow (now keyed on AdvClass instead), not a separate transformation-only pick. Unlike the old effect, this
///     one no longer toggles skill-pane visibility - the new specialization actives are permanently available once
///     learned, not form-gated. Drains MP once per second at the same base <c>5 + (MaximumMp x 0.005)</c> formula
///     as the old effect, scaled by <see cref="DrainMultiplier" /> - only Tempest's own evolution note calls out
///     "lower Mana drain" per tier, so that's the only specialization where this isn't 1.0. All three
///     specializations (Beast/Fighter, Ironscale/Tank, Tempest/RangedChi) now have their tier stats/sprites fully
///     designed, completing this effect's per-specialization branches.
/// </summary>
public sealed class MartialFormEffect : IntervalEffectBase
{
    private const string OriginalSpriteTag = "OriginalSprite";

    private static readonly Animation TransformAnimation = new()
    {
        TargetAnimation = 14,
        AnimationSpeed = 100
    };

    private Attributes AppliedBonus = new();

    /// <inheritdoc />
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(10);

    /// <inheritdoc />
    protected override IIntervalTimer Interval { get; } = new IntervalTimer(TimeSpan.FromMilliseconds(1000));

    /// <summary>
    ///     The floor-gated tier (1-4) driving both the sprite and the stat bonus - set by MartialFormScript before
    ///     applying
    /// </summary>
    public int Tier { get; set; } = 1;

    /// <summary>
    ///     Multiplier applied to the base MP drain formula - only Tempest's Form lowers this per-tier (per its own
    ///     evolution note); stays 1.0 for every other specialization
    /// </summary>
    private decimal DrainMultiplier { get; set; } = 1.0m;

    public override byte Icon => 59;
    public override string Name => "Martial Form";

    /// <inheritdoc />
    public override void OnApplied()
    {
        var specialization = AislingSubject?.UserStatSheet.AdvClass ?? AdvClass.None;

        var sprite = (specialization, Tier) switch
        {
            (AdvClass.Fighter, 1) => (ushort)425,
            (AdvClass.Fighter, 2) => (ushort)426,
            (AdvClass.Fighter, 3) => (ushort)427,
            (AdvClass.Fighter, 4) => (ushort)427, //placeholder - no dedicated Tier IV "Final Form" werewolf sprite exists yet
            (AdvClass.Tank, 1)    => (ushort)428,
            (AdvClass.Tank, 2)    => (ushort)429,
            (AdvClass.Tank, 3)    => (ushort)430,
            (AdvClass.Tank, 4)    => (ushort)430, //placeholder - no dedicated Tier IV "Final Form" lizardman sprite exists yet
            (AdvClass.RangedChi, 1) => (ushort)431,
            (AdvClass.RangedChi, 2) => (ushort)432,
            (AdvClass.RangedChi, 3) => (ushort)433,
            (AdvClass.RangedChi, 4) => (ushort)433, //placeholder - no dedicated Tier IV "Final Form" harpy sprite exists yet
            _                       => (ushort)0
        };

        AppliedBonus = (specialization, Tier) switch
        {
            (AdvClass.Fighter, 1) => new Attributes { Str = 10, FlatSkillDamage = 10 },
            (AdvClass.Fighter, 2) => new Attributes { Str = 15, FlatSkillDamage = 20 },
            (AdvClass.Fighter, 3) => new Attributes { Str = 20, Dex = 5, FlatSkillDamage = 30 },
            (AdvClass.Fighter, 4) => new Attributes { Str = 30, Dex = 10, FlatSkillDamage = 45, AtkSpeedPct = 10 },
            (AdvClass.Tank, 1)    => new Attributes { Con = 10, Ac = -5 },
            (AdvClass.Tank, 2)    => new Attributes { Con = 15, Ac = -10 },
            (AdvClass.Tank, 3)    => new Attributes { Con = 20, Str = 5, Ac = -15 },
            (AdvClass.Tank, 4)    => new Attributes { Con = 30, Str = 10, Ac = -25, MaximumHp = 500 },
            (AdvClass.RangedChi, 1) => new Attributes { Dex = 10, Wis = 5, FlatSpellDamage = 10 },
            (AdvClass.RangedChi, 2) => new Attributes { Dex = 15, Wis = 10, FlatSpellDamage = 20 },
            (AdvClass.RangedChi, 3) => new Attributes { Dex = 20, Wis = 15, Ac = -5, FlatSpellDamage = 30, AtkSpeedPct = 10 },
            (AdvClass.RangedChi, 4) => new Attributes { Dex = 30, Wis = 20, Ac = -10, FlatSpellDamage = 45, AtkSpeedPct = 20 },
            _                       => new Attributes()
        };

        //Tempest's own evolution note calls out "lower Mana drain" alongside stat improvements - no other
        //specialization's Form has this, so it's a per-tier multiplier on the base drain formula rather than a
        //universal one. Stays 1.0 (no change) for Beast/Ironscale.
        DrainMultiplier = (specialization, Tier) switch
        {
            (AdvClass.RangedChi, 1) => 1.0m,
            (AdvClass.RangedChi, 2) => 0.85m,
            (AdvClass.RangedChi, 3) => 0.7m,
            (AdvClass.RangedChi, 4) => 0.5m,
            _                       => 1.0m
        };

        if (AislingSubject is not null)
        {
            AislingSubject.Trackers.Tags[OriginalSpriteTag] = AislingSubject.Sprite.ToString();

            if (sprite > 0)
                AislingSubject.SetSprite(sprite);
        }

        Subject.StatSheet.AddBonus(AppliedBonus);
        Subject.Animate(TransformAnimation, Subject.Id);
        Subject.Display();
    }

    /// <inheritdoc />
    protected override void OnIntervalElapsed()
    {
        var drainPerSecond = (5 + (Subject.StatSheet.MaximumMp * 0.005m)) * DrainMultiplier;
        Subject.StatSheet.SubtractMp(Convert.ToInt32(drainPerSecond));
        AislingSubject?.Client.SendAttributes(StatUpdateType.Vitality);

        //terminate by zeroing Remaining rather than calling EffectsBar.Terminate directly, since that would
        //re-enter the same lock EffectsBar.Update is already holding while ticking this effect - same reasoning
        //as the old BeastFormEffect
        if (Subject.StatSheet.CurrentMp <= 0)
            Remaining = TimeSpan.Zero;
    }

    /// <inheritdoc />
    public override void OnTerminated()
    {
        Subject.StatSheet.SubtractBonus(AppliedBonus);

        if (AislingSubject is not null
            && AislingSubject.Trackers.Tags.TryRemove(OriginalSpriteTag, out var originalSpriteStr)
            && ushort.TryParse(originalSpriteStr, out var originalSprite))
            AislingSubject.SetSprite(originalSprite);

        AislingSubject?.SendActiveMessage("Your martial form fades.");
        Subject.Display();
    }
}
