#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     A direct build growing out of the flat "Regrowth" spell (renamed to Blooming Life, its RegenerationEffect
///     replaced by the dedicated, tier-aware <see cref="Chaos.Scripting.EffectScripts.BloomingLifeEffect" />). One of
///     Mystic's 5 evolving abilities - follows the "universal first evolution on Floor 3" schedule, same shape as
///     Bard's Salvation: Floor2 intro, Floor3 first evolution, Floor4, Floor5 max. All placeholder values, not
///     balance-tested.
/// </summary>
public class BloomingLifeScript : ConfigurableSpellScriptBase
{
    /// <inheritdoc />
    public BloomingLifeScript(Spell subject)
        : base(subject) { }

    /// <inheritdoc />
    public override bool CanUse(SpellContext context)
    {
        if (!context.Source.IsAlive)
            return false;

        if ((context.TargetCreature is not { IsAlive: true } target) || !Filter.IsValidTarget(context.Source, target))
        {
            context.SourceAisling?.SendOrangeBarMessage("You must select a valid ally.");

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
            context.SourceAisling?.SendOrangeBarMessage("Not enough focus.");

            return;
        }

        context.SourceAisling?.Client.SendAttributes(StatUpdateType.Vitality);

        source.AnimateBody(BodyAnimation);

        Bloom(source, target, tier);

        if (tier.SplashRadius > 0)
            foreach (var nearbyAlly in map.GetEntitiesWithinRange<Creature>(target, tier.SplashRadius))
            {
                if (nearbyAlly.Equals(target) || !Filter.IsValidTarget(source, nearbyAlly))
                    continue;

                Bloom(source, nearbyAlly, tier);
            }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.TargetPoint);
    }

    private void Bloom(Creature source, Creature target, (int HealPerTick, int DurationMs, int SplashRadius) tier)
    {
        var bloomingLifeEffect = new BloomingLifeEffect
        {
            Caster = source,
            HealPerTick = tier.HealPerTick
        };

        bloomingLifeEffect.SetDuration(TimeSpan.FromMilliseconds(tier.DurationMs));
        target.Effects.Apply(source, bloomingLifeEffect, this);

        if (Animation != null)
            target.Animate(Animation, source.Id);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor2(Level&lt;=4)=I(intro,HoT), Floor3(&lt;=6)=II(stronger
    ///     heal/tick), Floor4(&lt;=8)=III(+splash to nearby allies), Floor5+(&gt;8)=IV(max,longer duration+strongest
    ///     heal/tick).
    /// </summary>
    private (int HealPerTick, int DurationMs, int SplashRadius) GetTierValues() =>
        Subject.Level switch
        {
            <= 4 => (40, 6000, 0),
            <= 6 => (60, 6000, 0),
            <= 8 => (60, 6000, 2),
            _    => (90, 9000, 2)
        };

    #region ScriptVars
    /// <summary>
    ///     The animation played on each blooming target
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine whether a given creature is a valid Blooming Life target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The MP cost to use this spell
    /// </summary>
    public int ManaCost { get; init; }

    /// <summary>
    ///     The maximum distance, in tiles, a target can be selected from
    /// </summary>
    public int Range { get; init; }

    /// <summary>
    ///     Sound played on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
