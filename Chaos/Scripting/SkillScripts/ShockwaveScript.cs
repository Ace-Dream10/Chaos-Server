#region
using Chaos.Collections;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     One of Ironscale's 7 specialization actives, and one of its 3 evolving abilities - "emit a damaging
///     shockwave around you, generating threat and controlling nearby enemies" per the locked design, evolving
///     into "the Titan's signature battlefield control ability." A direct build: damages and pulls aggro from
///     every hostile within radius of the caster, and at Tier III+ additionally roots them in place (reusing
///     RootEffect directly, same as every other CC application this session) - the "battlefield control" the
///     evolution note calls out.
/// </summary>
public class ShockwaveScript : ConfigurableSkillScriptBase
{
    private readonly IApplyDamageScript ApplyDamageScript;

    /// <inheritdoc />
    public ShockwaveScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var tier = GetTierValues();

        source.AnimateBody(BodyAnimation);

        if (Animation != null)
            map.ShowAnimation(Animation.GetPointAnimation(context.SourcePoint, source.Id));

        foreach (var monster in map.GetEntitiesWithinRange<Monster>(source, tier.Radius))
        {
            if (!monster.IsAlive || !Filter.IsValidTarget(source, monster))
                continue;

            var damage = (BaseDamage ?? 0) + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.CON) * (DamageStatMultiplier ?? 1));

            if (damage > 0)
                ApplyDamageScript.ApplyDamage(source, monster, this, damage);

            monster.AggroList.AddAggro(source, tier.AggroPerTick);

            if (tier.AppliesRoot)
            {
                var rootEffect = new RootEffect();
                rootEffect.SetDuration(TimeSpan.FromMilliseconds(tier.RootDurationMs));
                monster.Effects.Apply(source, rootEffect, this);
            }
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Per the locked Floor Schedule, Shockwave doesn't intro
    ///     until Floor 7: Floor7(Level&lt;=14)=I(obtain,radius2,no CC), Floor8(&lt;=16)=II(radius2,threat only),
    ///     Floor9(&lt;=18)=III(radius3,+root,"battlefield control"), Floor10+(&gt;18)=IV(max,radius4,+longer root).
    /// </summary>
    private (int Radius, int AggroPerTick, bool AppliesRoot, int RootDurationMs) GetTierValues() =>
        Subject.Level switch
        {
            <= 14 => (2, 400, false, 0),
            <= 16 => (2, 550, false, 0),
            <= 18 => (3, 700, true, 1500),
            _     => (4, 850, true, 2500)
        };

    #region ScriptVars
    /// <summary>
    ///     The animation played centered on the caster
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt to each hit target
    /// </summary>
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The stat used to scale bonus damage
    /// </summary>
    public Stat? DamageStat { get; init; }

    /// <summary>
    ///     The multiplier applied to <see cref="DamageStat" /> when calculating bonus damage
    /// </summary>
    public decimal? DamageStatMultiplier { get; init; }

    /// <summary>
    ///     The filter used to determine which nearby monsters are valid targets
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     Sound played on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
