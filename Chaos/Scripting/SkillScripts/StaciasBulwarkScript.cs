#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyHealing;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     One of Bastion's 5 evolving abilities (tier scales with the skill's own level, using the same
///     level-bracket convention established elsewhere - level-gating stands in for real floor-gating here,
///     temporary pending the single batched conversion pass once floor maps exist). Per the design's "longer
///     duration and stronger effects with each evolution": duration grows every tier, tier III+ adds a self-heal
///     when the invulnerability window ends (rewarding riding it out), and tier IV additionally pulses a reduced
///     mass-aggro pull (reusing the same <see cref="ChallengingShoutScript" /> technique) so a maxed Bulwark
///     actively holds the room's attention while untouchable, not just survives it.
/// </summary>
public class StaciasBulwarkScript : ConfigurableSkillScriptBase
{
    private readonly List<PendingHeal> PendingHealOnExpiry = [];

    /// <inheritdoc />
    public StaciasBulwarkScript(Skill subject)
        : base(subject)
        => ApplyHealScript = FunctionalScripts.ApplyHealing.ApplyHealScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var tier = GetTierValues();

        source.AnimateBody(BodyAnimation);

        var bulwarkEffect = new StaciasBulwarkEffect();
        bulwarkEffect.SetDuration(TimeSpan.FromMilliseconds(tier.DurationMs));
        source.Effects.Apply(source, bulwarkEffect, this);

        if (tier.HealOnExpiryPct > 0)
            PendingHealOnExpiry.Add(new PendingHeal(source, TimeSpan.FromMilliseconds(tier.DurationMs), tier.HealOnExpiryPct));

        if (tier.TauntPulse)
            foreach (var monster in map.GetEntities<Monster>())
                if (Filter.IsValidTarget(source, monster))
                    monster.AggroList.AddAggro(source, 20000);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (PendingHealOnExpiry.Count == 0)
            return;

        for (var i = PendingHealOnExpiry.Count - 1; i >= 0; i--)
        {
            var pending = PendingHealOnExpiry[i];
            pending.Remaining -= delta;

            if (pending.Remaining > TimeSpan.Zero)
                continue;

            PendingHealOnExpiry.RemoveAt(i);

            if (!pending.Source.IsAlive)
                continue;

            var healAmount = Convert.ToInt32(pending.Source.StatSheet.EffectiveMaximumHp * (pending.HealPct / 100m));

            if (healAmount > 0)
                ApplyHealScript.ApplyHeal(pending.Source, pending.Source, this, healAmount);
        }
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested.
    /// </summary>
    private (int DurationMs, int HealOnExpiryPct, bool TauntPulse) GetTierValues() =>
        Subject.Level switch
        {
            <= 2 => (2000, 0, false),
            <= 4 => (3000, 0, false),
            <= 6 => (4000, 10, false),
            _    => (5000, 15, true)
        };

    private sealed class PendingHeal(Creature source, TimeSpan remaining, int healPct)
    {
        public int HealPct { get; } = healPct;
        public TimeSpan Remaining { get; set; } = remaining;
        public Creature Source { get; } = source;
    }

    #region ScriptVars
    public IApplyHealScript ApplyHealScript { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used for the tier IV taunt pulse - should stay hostileOnly
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     Sound played on activation
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
