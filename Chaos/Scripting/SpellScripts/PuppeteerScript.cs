#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     Scans the direct line in front of the caster for hostile monsters within range and turns them against
///     their allies for a while via <see cref="PuppeteerEffect" />.
/// </summary>
/// <remarks>
///     One of Trickster's 5 evolving abilities. Tiers per the locked design ("longer control -&gt; stronger
///     controlled targets -&gt; elite enemies -&gt; multiple controlled enemies") mapped to Trickster's own floor
///     arc (Floor4 intro, Floor5, Floor6, Floor7 max) via the same "Level ≈ 2×Floor" ratio used throughout tonight
///     - see <see cref="GetTierValues" />. "Stronger controlled targets"/"elite enemies" at Tier III aren't gated
///     on anything new here - MonsterTemplate has no boss/elite classification in this codebase yet (the same gap
///     Execute's own "non-boss enemies" wording runs into), so Puppeteer already works on any monster regardless
///     of tier; only duration and target count actually change tier-to-tier. Flagging the gap rather than building
///     a new classification system tonight. All placeholder values, not balance-tested.
/// </remarks>
public class PuppeteerScript : ConfigurableSpellScriptBase
{
    /// <inheritdoc />
    public PuppeteerScript(Spell subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var tier = GetTierValues();

        var endPoint = source.DirectionalOffset(source.Direction, Range);

        var points = source.GetDirectPath(endPoint)
                           .Skip(1)
                           .ToList();

        var targets = new List<Monster>();

        foreach (var point in points)
        {
            if (map.IsWall(point) || map.IsBlockingReactor(point))
                break;

            var entity = map.GetEntitiesAtPoints<Monster>(point)
                            .TopOrDefault();

            if ((entity != null) && Filter.IsValidTarget(source, entity))
            {
                targets.Add(entity);

                if (targets.Count >= tier.MaxTargets)
                    break;
            }
        }

        if (targets.Count == 0)
        {
            context.SourceAisling?.SendOrangeBarMessage("No target in range.");

            return;
        }

        source.AnimateBody(BodyAnimation);

        foreach (var target in targets)
        {
            var puppeteerEffect = new PuppeteerEffect();
            puppeteerEffect.SetDuration(TimeSpan.FromMilliseconds(tier.DurationMs));
            target.Effects.Apply(source, puppeteerEffect, this);

            if (Animation != null)
                target.Animate(Animation, source.Id);

            if (Sound.HasValue)
                map.PlaySound(Sound.Value, Point.From(target));
        }
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor4(Level&lt;=8)=I(intro,8s,1 target),
    ///     Floor5(&lt;=10)=II(12s,1,"longer control"), Floor6(&lt;=12)=III(12s,1,"elite enemies" - see remarks),
    ///     Floor7+(&gt;12)=IV(max,12s,2 targets,"multiple controlled enemies").
    /// </summary>
    private (int DurationMs, int MaxTargets) GetTierValues() =>
        Subject.Level switch
        {
            <= 8  => (8000, 1),
            <= 10 => (12000, 1),
            <= 12 => (12000, 1),
            _     => (12000, 2)
        };

    #region ScriptVars
    /// <summary>
    ///     The animation played on the target
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine whether the first creature encountered in the scan is a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The maximum number of tiles scanned in front of the caster for a target
    /// </summary>
    public int Range { get; init; } = 4;

    /// <summary>
    ///     Sound played on the target's position
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
