#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SpellScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     A generic AoE damage spell (any <see cref="AoeShape" />, centered on the caster for NoTarget spells or on
///     the chosen ground point for Targeted-point spells) that additionally knocks each hit hostile back a
///     configurable number of tiles, away from the AoE's center. Optionally also applies a single effect to each
///     hit target.
/// </summary>
public class KnockbackAoeScript : ConfigurableSpellScriptBase
{
    private readonly IEffectFactory EffectFactory;

    /// <inheritdoc />
    public KnockbackAoeScript(Spell subject, IEffectFactory effectFactory)
        : base(subject)
    {
        ApplyDamageScript = ApplyAttackDamageScript.Create();
        EffectFactory = effectFactory;
    }

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var origin = context.TargetPoint;

        source.AnimateBody(BodyAnimation);

        var options = new AoeShapeOptions
        {
            Source = origin,
            Range = Range,
            Direction = source.Direction
        };

        var points = Shape.ResolvePoints(options);

        foreach (var point in points)
        {
            var targets = map.GetEntitiesAtPoints<Creature>(point)
                             .Where(creature => Filter.IsValidTarget(source, creature))
                             .ToArray();

            foreach (var target in targets)
            {
                var damage = (BaseDamage ?? 0)
                             + Convert.ToInt32(source.StatSheet.GetEffectiveStat(DamageStat ?? Stat.INT) * (DamageStatMultiplier ?? 1));

                if (damage > 0)
                    ApplyDamageScript.ApplyDamage(source, target, this, damage, Element);

                if (!string.IsNullOrEmpty(EffectKey) && target.IsAlive)
                {
                    var effect = EffectFactory.Create(EffectKey);

                    if (EffectDurationMs.HasValue)
                        effect.SetDuration(TimeSpan.FromMilliseconds(EffectDurationMs.Value));

                    target.Effects.Apply(source, effect, this);
                }

                if ((KnockbackTiles > 0) && target.IsAlive && (point != origin))
                {
                    var pushDirection = point.DirectionalRelationTo(origin);
                    var landingPoint = point.DirectionalOffset(pushDirection, KnockbackTiles);

                    if (map.IsWalkable(landingPoint, target, false))
                        target.WarpTo(landingPoint);
                }

                if (Animation != null)
                    target.Animate(Animation, source.Id);
            }
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, origin);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each creature struck
    /// </summary>
    public Animation? Animation { get; init; }

    public IApplyDamageScript ApplyDamageScript { get; init; }

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
    ///     Optional effect duration override for <see cref="EffectKey" />
    /// </summary>
    public int? EffectDurationMs { get; init; }

    /// <summary>
    ///     Optional effect key applied to each hit target alongside the damage
    /// </summary>
    public string? EffectKey { get; init; }

    /// <summary>
    ///     The element of the damage dealt
    /// </summary>
    public Element? Element { get; init; }

    /// <summary>
    ///     The filter used to determine which creatures in the AoE are valid targets
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     How many tiles each hit creature is knocked back, away from the AoE's center
    /// </summary>
    public int KnockbackTiles { get; init; }

    /// <summary>
    ///     The radius/reach of the AoE shape
    /// </summary>
    public int Range { get; init; }

    /// <summary>
    ///     The shape of the AoE
    /// </summary>
    public AoeShape Shape { get; init; }

    /// <summary>
    ///     Sound played at the AoE's center on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
