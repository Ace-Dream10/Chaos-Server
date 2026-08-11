#region
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
///     One of Assassin's 5 evolving abilities, and a direct build - nothing existing matched "sacrifice a large
///     portion of your health to deal devastating percentage damage". Tiers per the locked design ("lower health
///     sacrifice -&gt; higher percentage damage -&gt; improved risk/reward -&gt; endgame survivability while
///     casting") mapped to Assassin's own floor arc (Floor6 intro, Floor7, Floor8, Floor9 max) via the same "Level
///     ≈ 2×Floor" ratio used throughout tonight - see <see cref="GetTierValues" />. The self-sacrifice is clamped
///     to leave the caster at a minimum of 1 HP rather than allowing a self-kill - a judgment call, not explicit in
///     the locked design, flagged here rather than silently assumed. Tier IV's "survivability while casting" is a
///     brief absorb shield (<see cref="DeathsConvictionShieldEffect" />) granted immediately after the sacrifice,
///     the moment of highest risk. All placeholder values, not balance-tested.
/// </summary>
public class DeathsConvictionScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public DeathsConvictionScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var tier = GetTierValues();

        var targetPoint = source.DirectionalOffset(source.Direction, Range);
        var target = map.GetEntitiesAtPoints<Creature>(targetPoint).TopOrDefault();

        if ((target == null) || !Filter.IsValidTarget(source, target))
            return;

        source.AnimateBody(BodyAnimation);

        //sacrifice a portion of the caster's own current HP, clamped so this can never be a self-kill
        var sacrificeAmount = Math.Max(0, Convert.ToInt32(source.StatSheet.CurrentHp * tier.SacrificePct) - 1);
        sacrificeAmount = Math.Min(sacrificeAmount, source.StatSheet.CurrentHp - 1);

        if (sacrificeAmount > 0)
        {
            source.StatSheet.SubtractHp(sacrificeAmount);

            if (source is Aisling sourceAisling)
                sourceAisling.Client.SendAttributes(StatUpdateType.Vitality);
        }

        var damage = Convert.ToInt32(target.StatSheet.CurrentHp * tier.DamagePct);

        if (damage > 0)
            ApplyDamageScript.ApplyDamage(source, target, this, damage);

        if (tier.GrantsShield)
        {
            var shield = new DeathsConvictionShieldEffect();
            source.Effects.Apply(source, shield, this);
        }

        if (Animation != null)
            target.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, targetPoint);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor6(Level&lt;=12)=I(intro,30% sac/40% dmg),
    ///     Floor7(&lt;=14)=II(25%/55%), Floor8(&lt;=16)=III(20%/70%), Floor9+(&gt;16)=IV(max,15%/85%+shield).
    /// </summary>
    private (decimal SacrificePct, decimal DamagePct, bool GrantsShield) GetTierValues() =>
        Subject.Level switch
        {
            <= 12 => (0.30m, 0.40m, false),
            <= 14 => (0.25m, 0.55m, false),
            <= 16 => (0.20m, 0.70m, false),
            _     => (0.15m, 0.85m, true)
        };

    #region ScriptVars
    /// <summary>
    ///     The animation played on the target on hit
    /// </summary>
    public Animation? Animation { get; init; }

    public IApplyDamageScript ApplyDamageScript { get; init; }

    /// <summary>
    ///     The body animation played by the caster when the skill is used
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine whether the tile directly in front of the caster holds a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The range, in tiles, at which the target is checked
    /// </summary>
    public int Range { get; init; } = 1;

    /// <summary>
    ///     Sound played on hit
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
