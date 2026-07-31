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

public class ManaBurstScript : ConfigurableSkillScriptBase
{
    private readonly IApplyDamageScript ApplyDamageScript;

    /// <inheritdoc />
    public ManaBurstScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        var currentMp = source.StatSheet.CurrentMp;
        var damage = (BaseDamage ?? 0) + Convert.ToInt32(currentMp * BurstMultiplier);

        var targets = map.GetEntitiesWithinRange<Monster>(source, Range)
                        .Where(monster => Filter.IsValidTarget(source, monster))
                        .ToArray();

        foreach (var monster in targets)
        {
            ApplyDamageScript.ApplyDamage(source, monster, this, damage);

            if (Animation != null)
                monster.Animate(Animation, source.Id);
        }

        source.StatSheet.SetMp(0);

        if (source is Aisling sourceAisling)
            sourceAisling.Client.SendAttributes(StatUpdateType.Vitality);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each hit monster
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt, before the MP-scaled bonus
    /// </summary>
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The multiplier applied to the caster's current MP when calculating bonus damage
    /// </summary>
    public decimal BurstMultiplier { get; init; } = 2;

    /// <summary>
    ///     The filter used to determine which nearby creatures are hit
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The radius around the caster affected by the burst
    /// </summary>
    public int Range { get; init; } = 2;

    /// <summary>
    ///     The sound played at the caster's position on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
