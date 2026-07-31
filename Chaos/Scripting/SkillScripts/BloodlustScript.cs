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

public class BloodlustScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public BloodlustScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        var currentMp = source.StatSheet.CurrentMp;

        if (currentMp < MinimumMp)
        {
            if (source is Aisling aisling)
                aisling.SendOrangeBarMessage("Not enough kill energy.");

            return;
        }

        source.AnimateBody(BodyAnimation);

        var durationMs = currentMp * MsPerMp;

        var frenzyEffect = new BloodlustEffect
        {
            AtkSpeedBonus = AtkSpeedBonus,
            FlatDamageBonus = FlatDamageBonus
        };
        frenzyEffect.SetDuration(TimeSpan.FromMilliseconds(durationMs));

        source.Effects.Apply(source, frenzyEffect, this);

        source.StatSheet.SetMp(0);

        if (source is Aisling sourceAisling)
            sourceAisling.Client.SendAttributes(StatUpdateType.Vitality);

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
    ///     The bonus applied to AtkSpeedPct for the duration of the frenzy
    /// </summary>
    public int AtkSpeedBonus { get; init; } = 50;

    /// <summary>
    ///     The body animation played by the caster when the skill is used
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine valid targets (should stay selfOnly)
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The bonus applied to FlatSkillDamage for the duration of the frenzy
    /// </summary>
    public int FlatDamageBonus { get; init; } = 30;

    /// <summary>
    ///     The minimum MP (kill energy) required to activate
    /// </summary>
    public int MinimumMp { get; init; } = 20;

    /// <summary>
    ///     How many milliseconds of frenzy duration each point of MP spent buys
    /// </summary>
    public int MsPerMp { get; init; } = 100;

    /// <summary>
    ///     Sound played on activation
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
