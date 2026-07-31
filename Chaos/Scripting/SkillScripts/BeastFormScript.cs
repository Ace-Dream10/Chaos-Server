#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

public class BeastFormScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public BeastFormScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        if (source is not Aisling aisling || (aisling.UserStatSheet.BaseClass != BaseClass.MartialArtist))
            return;

        if (!aisling.Trackers.Enums.TryGetValue<BeastFormType>(out var form) || (form == BeastFormType.None))
        {
            aisling.SendOrangeBarMessage("You have not chosen your beast form. Seek the Spirit Guide.");

            return;
        }

        if (source.StatSheet.CurrentMp < MinimumMp)
        {
            aisling.SendOrangeBarMessage("Not enough Chi.");

            return;
        }

        if (source.Effects.Contains("Beast Form"))
        {
            aisling.SendOrangeBarMessage("You are already transformed.");

            return;
        }

        source.AnimateBody(BodyAnimation);

        var beastFormEffect = new BeastFormEffect();
        source.Effects.Apply(source, beastFormEffect, this);

        if (Animation != null)
            source.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

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
    ///     The filter used to determine valid targets (should stay selfOnly)
    /// </summary>
    public TargetFilter Filter { get; init; }

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
