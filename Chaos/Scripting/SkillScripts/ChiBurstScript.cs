#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Releases all of the Martial Artist's stored Chi (current MP) as a devastating AoE burst, scaling with how
///     much Chi was banked. Empties the caster's MP afterward. Deals bonus damage while Beast Form is active.
/// </summary>
public class ChiBurstScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public ChiBurstScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var currentMp = source.StatSheet.CurrentMp;

        if (currentMp < MinimumMp)
        {
            context.SourceAisling?.SendOrangeBarMessage("Not enough Chi.");

            return;
        }

        source.AnimateBody(BodyAnimation);

        var damage = BaseDamage + Convert.ToInt32(currentMp * ChiMultiplier);

        if (source.Effects.Contains("Beast Form"))
            damage = Convert.ToInt32(damage * BeastFormBonus);

        foreach (var monster in map.GetEntitiesWithinRange<Monster>(source, Range))
        {
            if (!Filter.IsValidTarget(source, monster))
                continue;

            ApplyDamageScript.ApplyDamage(source, monster, this, damage);

            if (Animation != null)
                monster.Animate(Animation, source.Id);
        }

        source.StatSheet.SetMp(0);

        if (source is Aisling aisling)
            aisling.Client.SendAttributes(StatUpdateType.Vitality);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, source);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each hit monster
    /// </summary>
    public Animation? Animation { get; init; }

    public IApplyDamageScript ApplyDamageScript { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt
    /// </summary>
    public int BaseDamage { get; init; }

    /// <summary>
    ///     The bonus damage multiplier applied while Beast Form is active
    /// </summary>
    public decimal BeastFormBonus { get; init; } = 1.5m;

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The multiplier applied to the caster's current MP when calculating bonus damage
    /// </summary>
    public decimal ChiMultiplier { get; init; } = 1.5m;

    /// <summary>
    ///     The filter used to determine which nearby monsters are valid targets
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The minimum amount of current MP (Chi) required to use this skill
    /// </summary>
    public int MinimumMp { get; init; } = 50;

    /// <summary>
    ///     The radius around the caster affected by the burst
    /// </summary>
    public int Range { get; init; } = 2;

    /// <summary>
    ///     Sound played at the caster's position on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
