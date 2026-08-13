#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Geometry.Abstractions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.MonsterScripts;
using Chaos.Scripting.SpellScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     Spawns one or more stationary, pulsing hazard monsters in a footprint around a center point (the caster's
///     own point for NoTarget spells, or the chosen ground point for Targeted-point spells). Each hazard reuses
///     <see cref="StaciasJudgmentPulseScript" /> (damage pulse) if <see cref="EffectKey" /> isn't set, or
///     <see cref="AreaEffectPulseScript" /> (effect pulse) if it is.
/// </summary>
public class HazardFieldScript : ConfigurableSpellScriptBase
{
    private readonly IMonsterFactory MonsterFactory;

    /// <inheritdoc />
    public HazardFieldScript(Spell subject, IMonsterFactory monsterFactory)
        : base(subject)
        => MonsterFactory = monsterFactory;

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var origin = context.TargetPoint;

        if (!source.StatSheet.TrySubtractMp(ManaCost))
        {
            context.SourceAisling?.SendOrangeBarMessage("Not enough mana.");

            return;
        }

        context.SourceAisling?.Client.SendAttributes(StatUpdateType.Vitality);

        source.AnimateBody(BodyAnimation);

        foreach (var point in ResolveFootprint(origin))
        {
            if (map.IsWall(point))
                continue;

            var hazard = MonsterFactory.Create(HazardTemplateKey, map, point);
            map.AddEntity(hazard, point);

            if (hazard.Script.Is<DecoyExpirationScript>(out var expirationScript))
                expirationScript.DurationMs = DurationMs;

            if (hazard.Script.Is<StaciasJudgmentPulseScript>(out var damagePulse))
            {
                damagePulse.Caster = source;
                damagePulse.Filter = Filter;
                damagePulse.BaseDamage = BaseDamage ?? 0;
                damagePulse.DamageStat = DamageStat ?? Stat.INT;
                damagePulse.DamageStatMultiplier = DamageStatMultiplier ?? 1;
                damagePulse.DamageRange = PulseRange;
                damagePulse.PulseIntervalMs = PulseIntervalMs;
                damagePulse.DurationMs = DurationMs;
                damagePulse.Animation = Animation;
            }

            if (hazard.Script.Is<AreaEffectPulseScript>(out var effectPulse))
            {
                effectPulse.Caster = source;
                effectPulse.Filter = Filter;
                effectPulse.EffectKey = EffectKey;
                effectPulse.EffectDurationMs = EffectDurationMs;
                effectPulse.PulseRange = PulseRange;
                effectPulse.Animation = Animation;
            }

            if (Animation != null)
                map.ShowAnimation(Animation.GetPointAnimation(point, source.Id));
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, origin);
    }

    private List<Point> ResolveFootprint(Point origin)
        => Footprint switch
        {
            "grid2x2" =>
            [
                origin,
                new Point(origin.X + 1, origin.Y),
                new Point(origin.X, origin.Y + 1),
                new Point(origin.X + 1, origin.Y + 1)
            ],
            "grid3x3" => new Rectangle(origin, 3, 3).GetPoints()
                                                    .ToList(),
            "circle2" => new Circle(origin, 2).GetPoints()
                                              .ToList(),
            _ => [origin]
        };

    #region ScriptVars
    /// <summary>
    ///     The animation played on each hazard tile when it's placed, and re-played on each pulse
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt per pulse (damage-mode hazards only)
    /// </summary>
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The stat used to scale bonus damage (damage-mode hazards only)
    /// </summary>
    public Stat? DamageStat { get; init; }

    /// <summary>
    ///     The multiplier applied to <see cref="DamageStat" /> (damage-mode hazards only)
    /// </summary>
    public decimal? DamageStatMultiplier { get; init; }

    /// <summary>
    ///     How long, in milliseconds, each hazard lingers before disappearing
    /// </summary>
    public int DurationMs { get; init; } = 5000;

    /// <summary>
    ///     Optional effect duration override (effect-mode hazards only)
    /// </summary>
    public int? EffectDurationMs { get; init; }

    /// <summary>
    ///     If set, hazards reapply this effect to nearby hostiles instead of dealing damage
    /// </summary>
    public string? EffectKey { get; init; }

    /// <summary>
    ///     The filter used to determine which nearby monsters each hazard affects
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The shape of tiles the hazards are placed on: "single", "grid2x2", "grid3x3", or "circle2"
    /// </summary>
    public string Footprint { get; init; } = "single";

    /// <summary>
    ///     The monster template key used for each hazard tile
    /// </summary>
    public string HazardTemplateKey { get; init; } = "elemental_hazard";

    /// <summary>
    ///     The MP cost to use this spell
    /// </summary>
    public int ManaCost { get; init; }

    /// <summary>
    ///     The number of milliseconds between pulses
    /// </summary>
    public int PulseIntervalMs { get; init; } = 500;

    /// <summary>
    ///     The radius around each hazard affected by its pulse (0 = only the hazard's own tile)
    /// </summary>
    public int PulseRange { get; init; }

    /// <summary>
    ///     Sound played at the center point on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
