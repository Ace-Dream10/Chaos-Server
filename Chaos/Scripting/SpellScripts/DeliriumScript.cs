#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     One of Trickster's 5 evolving abilities. Switched from the generic applyEffect script to a dedicated one so
///     higher tiers can go wide (AoE), not just longer - the locked design's tiers ("longer duration -&gt; larger
///     radius -&gt; stronger confusion -&gt; entire groups descend into madness") mapped to Trickster's own floor
///     arc (Floor3 intro, Floor4, Floor5, Floor6 max) via the same "Level ≈ 2×Floor" ratio used throughout tonight
///     - see <see cref="GetTierValues" />. All placeholder values, not balance-tested.
/// </summary>
public class DeliriumScript : ConfigurableSpellScriptBase
{
    /// <inheritdoc />
    public DeliriumScript(Spell subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var tier = GetTierValues();

        source.AnimateBody(BodyAnimation);

        if (tier.AoeRadius <= 0)
        {
            //Tier I/II: single target - scans the path in front of the caster (same convention as Death Mark/
            //Puppeteer), not just the exact tile at max Range
            var endPoint = source.DirectionalOffset(source.Direction, Range);
            var points = source.GetDirectPath(endPoint).Skip(1);

            Monster? target = null;

            foreach (var point in points)
            {
                if (map.IsWall(point) || map.IsBlockingReactor(point))
                    break;

                var entity = map.GetEntitiesAtPoints<Monster>(point).TopOrDefault();

                if (entity != null)
                {
                    if (Filter.IsValidTarget(source, entity))
                        target = entity;

                    break;
                }
            }

            if (target == null)
                return;

            Afflict(source, target, tier.DurationMs);

            if (Sound.HasValue)
                map.PlaySound(Sound.Value, Point.From(target));
        } else
        {
            //Tier III/IV: AoE around the caster ("entire groups descend into madness")
            var centerPoint = Point.From(source);

            var targets = map.GetEntitiesWithinRange<Monster>(centerPoint, tier.AoeRadius)
                             .Where(monster => Filter.IsValidTarget(source, monster))
                             .ToList();

            foreach (var target in targets)
                Afflict(source, target, tier.DurationMs);

            if (Sound.HasValue)
                map.PlaySound(Sound.Value, centerPoint);
        }
    }

    private void Afflict(Creature source, Monster target, int durationMs)
    {
        var deliriumEffect = new DeliriumEffect();
        deliriumEffect.SetDuration(TimeSpan.FromMilliseconds(durationMs));
        target.Effects.Apply(source, deliriumEffect, this);

        if (Animation != null)
            target.Animate(Animation, source.Id);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor3(Level&lt;=6)=I(intro,single,2s),
    ///     Floor4(&lt;=8)=II(single,4s,"longer duration"), Floor5(&lt;=10)=III(AoE radius 2,4s,"larger radius"),
    ///     Floor6+(&gt;10)=IV(max,AoE radius 4,6s,"entire groups descend into madness").
    /// </summary>
    private (int DurationMs, int AoeRadius) GetTierValues() =>
        Subject.Level switch
        {
            <= 6  => (2000, 0),
            <= 8  => (4000, 0),
            <= 10 => (4000, 2),
            _     => (6000, 4)
        };

    #region ScriptVars
    /// <summary>
    ///     The animation played on each afflicted target
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster when the skill is used
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine which targets are valid
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The range, in tiles, used for the single-target tiers (I/II)
    /// </summary>
    public int Range { get; init; } = 6;

    /// <summary>
    ///     Sound played on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
