#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     One of Ironscale's 7 specialization actives, and one of its 3 evolving abilities - see
///     <see cref="Chaos.Scripting.EffectScripts.IronBodyEffect" />'s doc comment. Evolves into "the Ultimate
///     defensive cooldown" per the locked design. All placeholder values, not balance-tested.
/// </summary>
public class IronBodyScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public IronBodyScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var tier = GetTierValues();

        source.AnimateBody(BodyAnimation);

        var ironBodyEffect = new IronBodyEffect
        {
            AggroRadius = AggroRadius,
            AggroPerTick = tier.AggroPerTick,
            ReflectPct = tier.ReflectPct
        };

        ironBodyEffect.SetDuration(TimeSpan.FromMilliseconds(tier.DurationMs));
        source.Effects.Apply(source, ironBodyEffect, this);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Per the locked Floor Schedule, Iron Body doesn't intro
    ///     until Floor 6: Floor6(Level&lt;=12)=I(obtain,4s,20% reflect), Floor7(&lt;=14)=II(5s,30% reflect),
    ///     Floor8(&lt;=16)=III(6s,40% reflect), Floor9+(&gt;16)=IV(max,8s,50% reflect,more threat).
    /// </summary>
    private (int DurationMs, int ReflectPct, int AggroPerTick) GetTierValues() =>
        Subject.Level switch
        {
            <= 12 => (4000, 20, 500),
            <= 14 => (5000, 30, 650),
            <= 16 => (6000, 40, 800),
            _     => (8000, 50, 1000)
        };

    #region ScriptVars
    /// <summary>
    ///     The radius around the caster pulsed for threat each tick
    /// </summary>
    public int AggroRadius { get; init; } = 4;

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     Sound played on activation
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
