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
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     Renamed from Trapper's Net - kept its exact root+slow-on-hit mechanic. One of Fletcher's 5 evolving
///     abilities (evolution specifics weren't detailed in the locked design beyond "evolves into a small AoE
///     snare" - filled in here, flagged as such rather than left unbuilt). Tiers mapped to Fletcher's own floor arc
///     (Floor4 intro, Floor5, Floor6, Floor7 max) via the same "Level ≈ 2×Floor" ratio used throughout tonight -
///     see <see cref="GetTierValues" />. All placeholder values, not balance-tested.
/// </summary>
public class WardensNetScript : ConfigurableSpellScriptBase
{
    /// <inheritdoc />
    public WardensNetScript(Spell subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override bool CanUse(SpellContext context)
    {
        if (!context.Source.IsAlive)
            return false;

        if ((context.TargetCreature is not { IsAlive: true } target) || !Filter.IsValidTarget(context.Source, target))
        {
            context.SourceAisling?.SendOrangeBarMessage("You must select a valid target.");

            return false;
        }

        if (context.SourcePoint.ManhattanDistanceFrom(context.TargetPoint) > Range)
        {
            context.SourceAisling?.SendOrangeBarMessage("Your target is too far away.");

            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var target = context.TargetCreature!;
        var map = context.TargetMap;
        var tier = GetTierValues();

        if (!source.StatSheet.TrySubtractMp(ManaCost))
        {
            context.SourceAisling?.SendOrangeBarMessage("Not enough mana.");

            return;
        }

        context.SourceAisling?.Client.SendAttributes(StatUpdateType.Vitality);

        source.AnimateBody(BodyAnimation);

        var snared = tier.AoeRadius <= 0
            ? new List<Creature> { target }
            : map.GetEntitiesWithinRange<Creature>(context.TargetPoint, tier.AoeRadius)
                 .Where(creature => Filter.IsValidTarget(source, creature))
                 .ToList();

        foreach (var creature in snared)
            Snare(source, creature, tier.DurationMs);

        if (HitAnimation != null)
            target.Animate(HitAnimation, source.Id);

        if (OverlayAnimation != null)
            map.ShowAnimation(OverlayAnimation.GetPointAnimation(context.TargetPoint, source.Id));

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.TargetPoint);
    }

    private void Snare(Creature source, Creature target, int durationMs)
    {
        var rootEffect = new RootEffect();
        rootEffect.SetDuration(TimeSpan.FromMilliseconds(durationMs));
        target.Effects.Apply(source, rootEffect, this);

        var slowEffect = new SlowEffect { SlowAmount = SlowAmount };
        slowEffect.SetDuration(TimeSpan.FromMilliseconds(durationMs));
        target.Effects.Apply(source, slowEffect, this);

        if (BaseDamage is > 0)
            ApplyDamageScript.ApplyDamage(source, target, this, BaseDamage.Value);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor4(Level&lt;=8)=I(intro,single,3s),
    ///     Floor5(&lt;=10)=II(single,5s,"longer"), Floor6(&lt;=12)=III(AoE radius 1,5s), Floor7+(&gt;12)=IV(max,
    ///     AoE radius 2,6s,"small AoE snare").
    /// </summary>
    private (int DurationMs, int AoeRadius) GetTierValues() =>
        Subject.Level switch
        {
            <= 8  => (3000, 0),
            <= 10 => (5000, 0),
            <= 12 => (5000, 1),
            _     => (6000, 2)
        };

    #region ScriptVars
    public IApplyDamageScript ApplyDamageScript { get; init; }

    /// <summary>
    ///     Flat damage dealt on hit, for immediate feedback alongside the root/slow
    /// </summary>
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine which creatures are valid targets
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The animation played on the target on hit
    /// </summary>
    public Animation? HitAnimation { get; init; }

    /// <summary>
    ///     The MP cost to use this spell
    /// </summary>
    public int ManaCost { get; init; }

    /// <summary>
    ///     The animation played as an overlay, centered on the target's tile
    /// </summary>
    public Animation? OverlayAnimation { get; init; }

    /// <summary>
    ///     The maximum distance, in tiles, a target can be selected from
    /// </summary>
    public int Range { get; init; }

    /// <summary>
    ///     The amount added to a snared creature's MovementSpeedPct - passed through to SlowEffect on apply
    /// </summary>
    public int SlowAmount { get; init; } = 250;

    /// <summary>
    ///     The sound played on hit
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
