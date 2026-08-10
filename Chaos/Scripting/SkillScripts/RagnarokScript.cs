#region
using Chaos.Collections;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
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
///     The ultimate payoff of building Fury - releases all stored fury in a devastating burst.
/// </summary>
/// <remarks>
///     One of Valkyrie's 6 evolving abilities. Tier scales with the skill's own level - per the locked design's
///     floor schedule (Floor 1 intro, Floor 2 stall, Floor 3 first evolution, Floor 4, Floor 5 max), extrapolated
///     to the same "Level ~= 2x Floor" stand-in ratio <see cref="BastionsChargeScript" /> established
///     (1-2/3-4/5-6/7+ &#8594; tier I-IV) but re-derived for Ragnarok's specific 5-floor arc:
///     Floor1-2(Level&lt;=4)=I, Floor3(&lt;=6)=II, Floor4(&lt;=8)=III, Floor5+(&gt;8)=IV(max, stays). Per "bigger
///     explosion, better Fury scaling, holy aftermath at max": base damage and the fury-scaling multiplier both
///     increase per tier, and Tier IV adds a second, delayed pulse ("holy aftermath") - a concrete, simple
///     realization of that phrase rather than a more elaborate mechanic guessed from nothing.
/// </remarks>
public class RagnarokScript : ConfigurableSkillScriptBase
{
    private const int ScanRange = 2;

    private readonly IApplyDamageScript ApplyDamageScript;
    private readonly List<PendingAftermath> PendingAftermaths = [];

    /// <inheritdoc />
    public RagnarokScript(Skill subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var tier = GetTierValues();

        var currentMp = source.StatSheet.CurrentMp;

        if (currentMp < MinimumMp)
        {
            if (source is Aisling aisling)
                aisling.SendOrangeBarMessage("Not enough fury.");

            return;
        }

        if (CasterAnimation != null)
            source.Animate(CasterAnimation, source.Id);
        else
            source.AnimateBody(BodyAnimation);

        var damage = tier.BaseDamage + Convert.ToInt32(currentMp * tier.FuryMultiplier);

        var targets = map.GetEntitiesWithinRange<Monster>(source, ScanRange)
                        .Where(monster => Filter.IsValidTarget(source, monster))
                        .ToArray();

        foreach (var monster in targets)
        {
            ApplyDamageScript.ApplyDamage(source, monster, this, damage);

            if (Animation != null)
                monster.Animate(Animation, source.Id);
        }

        //Tier IV "holy aftermath" - a second, smaller pulse a moment later, on whatever's still standing/nearby
        if (tier.HasAftermath)
            PendingAftermaths.Add(new PendingAftermath(source, map, Convert.ToInt32(damage * 0.3m), TimeSpan.FromMilliseconds(800)));

        source.StatSheet.SetMp(0);

        if (source is Aisling sourceAisling)
            sourceAisling.Client.SendAttributes(StatUpdateType.Vitality);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (PendingAftermaths.Count == 0)
            return;

        for (var i = PendingAftermaths.Count - 1; i >= 0; i--)
        {
            var pending = PendingAftermaths[i];
            pending.Remaining -= delta;

            if (pending.Remaining > TimeSpan.Zero)
                continue;

            PendingAftermaths.RemoveAt(i);

            if (!pending.Source.IsAlive)
                continue;

            var targets = pending.Map.GetEntitiesWithinRange<Monster>(pending.Source, ScanRange)
                                 .Where(monster => Filter.IsValidTarget(pending.Source, monster))
                                 .ToArray();

            foreach (var monster in targets)
            {
                ApplyDamageScript.ApplyDamage(pending.Source, monster, this, pending.Damage);

                if (Animation != null)
                    monster.Animate(Animation, pending.Source.Id);
            }
        }
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested.
    /// </summary>
    private (int BaseDamage, decimal FuryMultiplier, bool HasAftermath) GetTierValues() =>
        Subject.Level switch
        {
            <= 4 => (100, 3m, false),
            <= 6 => (140, 3.5m, false),
            <= 8 => (180, 4m, false),
            _    => (220, 4.5m, true)
        };

    private sealed class PendingAftermath(Creature source, MapInstance map, int damage, TimeSpan remaining)
    {
        public int Damage { get; } = damage;
        public MapInstance Map { get; } = map;
        public TimeSpan Remaining { get; set; } = remaining;
        public Creature Source { get; } = source;
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each hit monster
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster, used if <see cref="CasterAnimation" /> is not set
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The grand animation played on the caster
    /// </summary>
    public Animation? CasterAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine which nearby creatures are hit
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The minimum MP required to use this skill
    /// </summary>
    public int MinimumMp { get; init; }

    /// <summary>
    ///     The sound played at the caster's position on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
