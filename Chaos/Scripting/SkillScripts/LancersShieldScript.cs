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

public class LancersShieldScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public LancersShieldScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        if (source.StatSheet.CurrentMp <= 0)
        {
            if (source is Aisling aisling)
                aisling.SendOrangeBarMessage("No shield charge.");

            return;
        }

        source.AnimateBody(BodyAnimation);

        var shieldEffect = new LancerShieldEffect
        {
            DamageToMpRate = DamageToMpRate
        };

        source.Effects.Apply(source, shieldEffect, this);

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
    ///     The multiplier applied to incoming damage to determine the MP cost while the shield is up
    /// </summary>
    public decimal DamageToMpRate { get; init; } = 2.25m;

    /// <summary>
    ///     The filter used to determine valid targets (should stay selfOnly)
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     Sound played on activation
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
