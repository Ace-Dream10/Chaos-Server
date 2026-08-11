#region
using Chaos.DarkAges.Definitions;
using Chaos.Extensions;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.ReactorTileScripts;
using Chaos.Scripting.SkillScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Replaces "Smoke Bomb" per the locked design's own note ("deemed too redundant with Blackout, and too
///     rogue-trap-flavored for Trickster's identity"). Plants a <see cref="DeceiversCacheReactorScript" /> reactor
///     tile at the caster's position - see that script's doc comment for the detonation mechanic.
/// </summary>
/// <remarks>
///     One of Trickster's 5 evolving abilities. Tiers per the locked design ("bigger blast radius, more
///     afflictions applied per detonation, possibly chain-triggering nearby caches") mapped to Trickster's own
///     floor arc (Floor6 intro, Floor7, Floor8, Floor9 max) via the same "Level ≈ 2×Floor" ratio used throughout
///     tonight - see <see cref="GetTierValues" />. All placeholder values, not balance-tested.
/// </remarks>
public class DeceiversCacheScript : ConfigurableSkillScriptBase
{
    private const string CacheTemplateKey = "deceivers_cache";

    private readonly IReactorTileFactory ReactorTileFactory;

    /// <inheritdoc />
    public DeceiversCacheScript(Skill subject, IReactorTileFactory reactorTileFactory)
        : base(subject)
        => ReactorTileFactory = reactorTileFactory;

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var point = Point.From(source);
        var tier = GetTierValues();

        source.AnimateBody(BodyAnimation);

        var cache = ReactorTileFactory.Create(CacheTemplateKey, map, point, null, source, this);
        map.SimpleAdd(cache);

        if (cache.Script.As<DeceiversCacheReactorScript>() is { } reactorScript)
        {
            reactorScript.BlastRadius = tier.BlastRadius;
            reactorScript.AfflictionsPerDetonation = tier.Afflictions;
            reactorScript.ChainTriggersNearbyCaches = tier.ChainTriggers;
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, point);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor6(Level&lt;=12)=I(intro,radius1,1 affliction),
    ///     Floor7(&lt;=14)=II(radius2,1), Floor8(&lt;=16)=III(radius2,2 afflictions), Floor9+(&gt;16)=IV(max,
    ///     radius3,2,chain-triggers nearby caches).
    /// </summary>
    private (int BlastRadius, int Afflictions, bool ChainTriggers) GetTierValues() =>
        Subject.Level switch
        {
            <= 12 => (1, 1, false),
            <= 14 => (2, 1, false),
            <= 16 => (2, 2, false),
            _     => (3, 2, true)
        };

    #region ScriptVars
    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     Sound played at the caster's position when the cache is planted
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
