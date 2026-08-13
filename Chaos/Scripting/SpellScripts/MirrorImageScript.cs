#region
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.MonsterScripts;
using Chaos.Scripting.SpellScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     Renamed in-place to Hall of Mirrors - display name/description only. The templateKey stays
///     <c>mirror_image</c> rather than getting a fresh key like every other rename tonight: a real saved character
///     (<c>Data/Saved/Aislings/hate/skills.json</c>) already has this skill learned, and changing the key would
///     silently orphan that reference with no migration path tonight. Flagged as a deliberate deviation from this
///     session's usual clean-rename convention.
/// </summary>
/// <remarks>
///     One of Trickster's 5 evolving abilities. Tiers per the locked design ("more illusions -&gt; longer duration
///     -&gt; illusions attack -&gt; illusions mimic selected abilities") mapped to Trickster's own floor arc (Floor6
///     intro, Floor7, Floor8, Floor9 max) via the same "Level ≈ 2×Floor" ratio used throughout tonight - see
///     <see cref="GetTierValues" />. "Illusions attack" at Tier III+ is implemented by spawning Assassin's
///     `shadow_clone` monster template (which actually fights back via ShadowCloneAggroScript) instead of the
///     purely-decoy `mirror_image_decoy` template used at Tier I/II. Tier IV's "illusions mimic selected abilities"
///     is simplified to the same strength as spawning more shadow_clones, same "flagged simplification, not
///     silently dropped" treatment as Assassin's Shadow Clone Tier IV. All placeholder values, not balance-tested.
/// </remarks>
public class MirrorImageScript : ConfigurableSpellScriptBase
{
    private const string AttackingDecoyTemplateKey = "shadow_clone";
    private const string PassiveDecoyTemplateKey = "mirror_image_decoy";

    private readonly IMonsterFactory MonsterFactory;

    /// <inheritdoc />
    public MirrorImageScript(Spell subject, IMonsterFactory monsterFactory)
        : base(subject)
        => MonsterFactory = monsterFactory;

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var tier = GetTierValues();

        for (var i = 0; i < tier.IllusionCount; i++)
        {
            if (!TryFindSpawnPoint(context, out var spawnPoint))
                continue;

            var decoyTemplateKey = tier.IllusionsAttack ? AttackingDecoyTemplateKey : PassiveDecoyTemplateKey;
            var decoy = MonsterFactory.Create(decoyTemplateKey, map, spawnPoint);
            map.AddEntity(decoy, spawnPoint);

            //override the decoy's default expiration duration with this tier's - only the passive decoy template
            //has DecoyExpirationScript; the attacking shadow_clone template expires on its own schedule
            if (decoy.Script.As<DecoyExpirationScript>() is { } expirationScript)
                expirationScript.DurationMs = tier.DurationMs;

            //force every nearby monster to switch aggro to the decoy
            foreach (var monster in map.GetEntitiesWithinRange<Monster>(context.SourcePoint, AggroRange))
                monster.AggroList.AddAggro(decoy, 99999);
        }

        if (Animation != null)
            source.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, Point.From(source));
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor6(Level&lt;=12)=I(intro,1 illusion,2.5s,passive),
    ///     Floor7(&lt;=14)=II(1,5s,"longer duration"), Floor8(&lt;=16)=III(1,5s,attacking,"illusions attack"),
    ///     Floor9+(&gt;16)=IV(max,2,7s,attacking,"mimic abilities" - see remarks above).
    /// </summary>
    private (int IllusionCount, int DurationMs, bool IllusionsAttack) GetTierValues() =>
        Subject.Level switch
        {
            <= 12 => (1, 2500, false),
            <= 14 => (1, 5000, false),
            <= 16 => (1, 5000, true),
            _     => (2, 7000, true)
        };

    /// <summary>
    ///     Finds the closest walkable point to the source, spiraling outward, so a spawn point is always found
    ///     regardless of walls or creatures immediately surrounding the caster
    /// </summary>
    private static bool TryFindSpawnPoint(ActivationContext context, out Point spawnPoint)
    {
        var source = context.Source;
        var map = context.TargetMap;

        foreach (var point in Point.From(source)
                                   .SpiralSearch())
        {
            //skip the caster's own tile, which SpiralSearch yields first
            if (point == Point.From(source))
                continue;

            if (map.IsWalkable(point, source, false))
            {
                spawnPoint = point;

                return true;
            }
        }

        spawnPoint = default;

        return false;
    }

    #region ScriptVars
    /// <summary>
    ///     The range, in tiles, within which nearby monsters will have their aggro forced onto the decoy
    /// </summary>
    public int AggroRange { get; init; }

    /// <summary>
    ///     The animation played on the caster when the skill is used
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The sound played at the spawn point when the decoy is summoned
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
