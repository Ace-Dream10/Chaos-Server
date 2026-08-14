#region
using Chaos.Collections;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     A direct build - the locked design lists Spirit Burst as reusing "an existing built ability," but a
///     thorough repo search (Spells, Skills, and every MonsterScript, across every class) turned up nothing named
///     spirit_burst/spiritburst anywhere - flagging this as a design-doc inaccuracy rather than silently building
///     something else and calling it a match. Built fresh as Mystic's flat damage workhorse: solid but
///     deliberately unremarkable direct damage, so it doesn't compete with Sorcerer's raw DPS role per Mystic's
///     own design philosophy. Not one of the 5 evolving abilities - flat, non-evolving.
/// </summary>
/// <remarks>
///     Reworked per playtest feedback from single-target to a frontal cone AoE, matching Trickster's Crack the
///     Whip - same <see cref="AoeShape.FrontalCone" /> pattern, same NoTarget spellType (no entity selection, hits
///     whatever's in front of the caster).
/// </remarks>
public class SpiritBurstScript : ConfigurableSpellScriptBase
{
    private readonly IApplyDamageScript ApplyDamageScript;

    /// <inheritdoc />
    public SpiritBurstScript(Spell subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        if (!source.StatSheet.TrySubtractMp(ManaCost))
        {
            context.SourceAisling?.SendOrangeBarMessage("Not enough mana.");

            return;
        }

        context.SourceAisling?.Client.SendAttributes(StatUpdateType.Vitality);

        source.AnimateBody(BodyAnimation);

        var options = new AoeShapeOptions
        {
            Source = source,
            Range = Range,
            Direction = source.Direction
        };

        var points = AoeShape.FrontalCone.ResolvePoints(options);
        var damage = (BaseDamage ?? 0) + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.WIS) * (DamageStatMultiplier ?? 1));

        foreach (var point in points)
        {
            var target = map.GetEntitiesAtPoints<Creature>(point).TopOrDefault();

            if ((target == null) || !Filter.IsValidTarget(source, target))
                continue;

            if (damage > 0)
                ApplyDamageScript.ApplyDamage(source, target, this, damage, Element.Darkness);

            if (Animation != null)
                target.Animate(Animation, source.Id);
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each struck target
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt
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
    ///     The filter used to determine whether a given tile holds a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The MP cost to use this spell
    /// </summary>
    public int ManaCost { get; init; }

    /// <summary>
    ///     The range, in tiles, of the frontal cone
    /// </summary>
    public int Range { get; init; }

    /// <summary>
    ///     Sound played once, on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
