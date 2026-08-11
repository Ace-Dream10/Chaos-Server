#region
using Chaos.Collections;
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.MonsterScripts;
using Chaos.Scripting.SpellScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     Spawns a Stacia's Judgment shrine at a hostile target's location - same pattern as
///     <see cref="StaciasShrineScript" />, but the spawned shrine damages nearby hostiles instead of healing allies.
///     One of Mystic's 5 evolving abilities, part of the core trio (alongside Stacia's Shrine and Spirit Rend) that
///     spans the whole game. The locked design gives Judgment its own detailed 5-stage arc (Lv.1 single impact,
///     Lv.25 larger impact, Lv.50 higher damage, Lv.75 lingering spirit field, Lv.100 double-strike) laid over the
///     same 4-checkpoint floor schedule as the rest of the trio - "larger impact" and "higher damage" are folded
///     together into Tier II (the same 5-named-stages-onto-4-checkpoints squeeze used repeatedly for Bard). Tier IV
///     ("Judgment strikes TWICE - falls, booms, then a second boom ~2 seconds later") is implemented as a delayed
///     second shrine spawn, tracked via <see cref="Update" /> the same way <see cref="StaciasPulseScript" /> tracks
///     its own delayed hits.
/// </summary>
public class StaciasJudgmentScript : ConfigurableSpellScriptBase
{
    private readonly IMonsterFactory MonsterFactory;
    private readonly List<PendingSecondStrike> PendingSecondStrikes = [];

    /// <inheritdoc />
    public StaciasJudgmentScript(Spell subject, IMonsterFactory monsterFactory)
        : base(subject)
        => MonsterFactory = monsterFactory;

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

        return true;
    }

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var target = context.TargetCreature!;
        var map = context.TargetMap;
        var spawnPoint = Point.From(target);
        var tier = GetTierValues();

        source.AnimateBody(BodyAnimation);

        SpawnShrine(source, map, spawnPoint, tier);

        if (tier.DoubleStrike)
            PendingSecondStrikes.Add(new PendingSecondStrike(TimeSpan.FromMilliseconds(SecondStrikeDelayMs), source, map, spawnPoint, tier));

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, spawnPoint);
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (PendingSecondStrikes.Count == 0)
            return;

        for (var i = PendingSecondStrikes.Count - 1; i >= 0; i--)
        {
            var pending = PendingSecondStrikes[i];
            pending.Remaining -= delta;

            if (pending.Remaining > TimeSpan.Zero)
                continue;

            PendingSecondStrikes.RemoveAt(i);
            SpawnShrine(pending.Source, pending.Map, pending.Point, pending.Tier);

            if (Sound.HasValue)
                pending.Map.PlaySound(Sound.Value, pending.Point);
        }
    }

    private void SpawnShrine(Creature source, MapInstance map, Point spawnPoint, (int DamageMultiplierPct, int DamageRangeBonus, int DurationBonusMs, bool DoubleStrike) tier)
    {
        var shrine = MonsterFactory.Create(ShrineTemplateKey, map, spawnPoint);
        map.AddEntity(shrine, spawnPoint);

        if (shrine.Script.Is<StaciasJudgmentPulseScript>(out var pulseScript))
        {
            pulseScript.Caster = source;
            pulseScript.Filter = Filter;
            pulseScript.BaseDamage = Convert.ToInt32(BaseDamage * (tier.DamageMultiplierPct / 100m));
            pulseScript.DamageStat = DamageStat;
            pulseScript.DamageStatMultiplier = DamageStatMultiplier;
            pulseScript.DamageRange = DamageRange + tier.DamageRangeBonus;
            pulseScript.DurationMs += tier.DurationBonusMs;
            pulseScript.Animation = Animation;
        }
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor1(Level&lt;=2)=I(obtain,Lv.1,single impact,baseline),
    ///     Floor4(&lt;=8)=II(Lv.25+Lv.50 folded together,larger impact+higher damage), Floor7(&lt;=14)=III(Lv.75,
    ///     +lingering spirit field via extended DurationMs), Floor10+(&gt;14)=IV(max,Lv.100,+double-strike).
    /// </summary>
    private (int DamageMultiplierPct, int DamageRangeBonus, int DurationBonusMs, bool DoubleStrike) GetTierValues() =>
        Subject.Level switch
        {
            <= 2  => (100, 0, 0, false),
            <= 8  => (150, 1, 0, false),
            <= 14 => (150, 1, 5000, false),
            _     => (150, 1, 5000, true)
        };

    private sealed class PendingSecondStrike(
        TimeSpan remaining,
        Creature source,
        MapInstance map,
        Point point,
        (int DamageMultiplierPct, int DamageRangeBonus, int DurationBonusMs, bool DoubleStrike) tier)
    {
        public MapInstance Map { get; } = map;
        public Point Point { get; } = point;
        public TimeSpan Remaining { get; set; } = remaining;
        public Creature Source { get; } = source;
        public (int DamageMultiplierPct, int DamageRangeBonus, int DurationBonusMs, bool DoubleStrike) Tier { get; } = tier;
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the shrine on each pulse
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt per pulse at Tier I - scaled up at higher tiers
    /// </summary>
    public int BaseDamage { get; init; } = 40;

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The radius around the shrine damaged on each pulse at Tier I - expanded at higher tiers
    /// </summary>
    public int DamageRange { get; init; } = 3;

    /// <summary>
    ///     The stat used to scale bonus damage
    /// </summary>
    public Stat DamageStat { get; init; } = Stat.WIS;

    /// <summary>
    ///     The multiplier applied to DamageStat when calculating bonus damage
    /// </summary>
    public decimal DamageStatMultiplier { get; init; } = 2;

    /// <summary>
    ///     The filter used to determine both the initial target's validity and which nearby monsters the shrine
    ///     damages
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     How long, in milliseconds, after the first shrine that the Tier IV second shrine spawns
    /// </summary>
    public int SecondStrikeDelayMs { get; init; } = 2000;

    /// <summary>
    ///     The templateKey of the shrine monster to spawn
    /// </summary>
    public string ShrineTemplateKey { get; init; } = string.Empty;

    /// <summary>
    ///     Sound played at the spawn point when a shrine is summoned
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
