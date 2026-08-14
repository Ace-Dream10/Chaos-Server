#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     One of Assassin's 5 evolving abilities. Threshold tiers per the locked design ("15% -&gt; 20% -&gt; 25% -&gt;
///     30% HP threshold") mapped to Assassin's own floor arc (Floor2 intro, Floor3, Floor4, Floor5 max) via the
///     same "Level ≈ 2×Floor" ratio used throughout tonight - see <see cref="GetTierValues" />. Placeholder
///     brackets, not balance-tested.
/// </summary>
/// <remarks>
///     The template description says "non-boss enemies" but there is no Boss <c>CreatureType</c> or equivalent
///     flag anywhere in this engine yet - that phrasing is aspirational, not backed by an actual exemption check.
///     Flagging rather than guessing at what "boss" should mean here; this can execute anything below threshold
///     right now, monster or otherwise.
/// </remarks>
public class ExecuteScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public ExecuteScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var executeThresholdPct = GetTierValues();

        source.AnimateBody(BodyAnimation);

        var targetPoint = source.DirectionalOffset(source.Direction);

        var creature = map.GetEntitiesAtPoints<Creature>(targetPoint)
                          .TopOrDefault();

        if ((creature == null) || !Filter.IsValidTarget(source, creature))
            return;

        //below-threshold branch must be a GUARANTEED kill, not just "usually enough" - the raw currentHp*3 value
        //still goes through the shared damage pipeline's normal AC mitigation like any other hit, so against a
        //high-AC target it could land for less than currentHp and leave them alive at a sliver of HP even though
        //Execute's animation already played. Confirmed root cause of "Execute sometimes doesn't kill". Using the
        //same defense-ignore pre-compensation Slayer's Cruel Thrust/Precision/Measured Slice already established,
        //at 100% ignore, so this specific hit lands fully unmitigated regardless of the target's AC.
        var damage = creature.StatSheet.HealthPercent <= executeThresholdPct
            ? DefenseIgnoreHelper.ApplyIgnoreDefense(creature, creature.StatSheet.CurrentHp * 3, 1.0m)
            : CalculateDamage(source);

        if (damage > 0)
            ApplyDamageScript.ApplyDamage(source, creature, this, damage);

        //Bloodlust's execute-chaining reset lives in AssassinFrenzyScript, on the tick after this kill is
        //observed - doing it here was a no-op, since Skill.Use() unconditionally calls BeginCooldown() right
        //after OnUse returns, which immediately re-establishes a fresh cooldown over anything set here.

        if (Animation != null)
            creature.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, Point.From(creature));
    }

    private int CalculateDamage(Creature source)
    {
        var damage = BaseDamage ?? 0;

        if (!DamageStat.HasValue)
            return damage;

        var statValue = source.StatSheet.GetEffectiveStat(DamageStat.Value);

        damage += DamageStatMultiplier.HasValue ? Convert.ToInt32(statValue * DamageStatMultiplier.Value) : statValue;

        return damage;
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor2(Level&lt;=4)=I(intro,15%), Floor3(&lt;=6)=II(20%),
    ///     Floor4(&lt;=8)=III(25%), Floor5+(&gt;8)=IV(max,30%).
    /// </summary>
    private decimal GetTierValues() =>
        Subject.Level switch
        {
            <= 4 => 15m,
            <= 6 => 20m,
            <= 8 => 25m,
            _    => 30m
        };

    #region ScriptVars
    /// <summary>
    ///     The animation played on the target on hit
    /// </summary>
    public Animation? Animation { get; init; }

    public IApplyDamageScript ApplyDamageScript { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.BaseDamage" />
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.DamageStat" />
    public Stat? DamageStat { get; init; }

    /// <inheritdoc cref="Chaos.Scripting.Components.AbilityComponents.DamageAbilityComponent.IDamageComponentOptions.DamageStatMultiplier" />
    public decimal? DamageStatMultiplier { get; init; }

    /// <summary>
    ///     The filter used to determine whether the creature directly in front is a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     Sound played on hit
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
