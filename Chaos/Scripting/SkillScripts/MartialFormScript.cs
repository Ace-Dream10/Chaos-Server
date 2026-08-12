#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Direct replacement for the old BeastFormScript (now retired) - transforms the caster into their chosen
///     specialization's martial form. See <see cref="Chaos.Scripting.EffectScripts.MartialFormEffect" />'s doc
///     comment for the floor-gated tier schedule and the Beast/Ironscale/Tempest split.
/// </summary>
public class MartialFormScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public MartialFormScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        if (source is not Aisling aisling || (aisling.UserStatSheet.BaseClass != BaseClass.MartialArtist))
            return;

        if (aisling.UserStatSheet.AdvClass == AdvClass.None)
        {
            aisling.SendOrangeBarMessage("You have not chosen your path. Seek the Spirit Guide.");

            return;
        }

        if (source.StatSheet.CurrentMp < MinimumMp)
        {
            aisling.SendOrangeBarMessage("Not enough Chi.");

            return;
        }

        if (source.Effects.Contains("Martial Form"))
        {
            aisling.SendOrangeBarMessage("You are already transformed.");

            return;
        }

        source.AnimateBody(BodyAnimation);

        var formEffect = new MartialFormEffect { Tier = GetTier(aisling) };
        source.Effects.Apply(source, formEffect, this);

        if (Animation != null)
            source.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    /// <summary>
    ///     Placeholder floor-gating stand-in (character Level, per this session's established convention) until
    ///     floor progression is tracked. Per the locked Floor Schedule, Meditate and Form are DESYNCED (not
    ///     lockstep): Floor2(Level&lt;=4)=I, Floor4(&lt;=8)=II, Floor7(&lt;=14)=III, Floor10+(&gt;14)=IV (Final
    ///     Form, caps here).
    /// </summary>
    private static int GetTier(Aisling aisling) =>
        aisling.StatSheet.Level switch
        {
            <= 4  => 1,
            <= 8  => 2,
            <= 14 => 3,
            _     => 4
        };

    #region ScriptVars
    /// <summary>
    ///     The animation played on the caster on activation
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster when the skill is used
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The minimum Chi (MP) required to transform
    /// </summary>
    public int MinimumMp { get; init; } = 20;

    /// <summary>
    ///     Sound played on activation
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
