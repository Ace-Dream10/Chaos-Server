#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.MonsterScripts;
using Chaos.Scripting.SpellScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     Spawns a Stacia's Judgment shrine at a hostile target's location - same pattern as
///     <see cref="StaciasShrineScript" />, but the spawned shrine damages nearby hostiles instead of healing allies
/// </summary>
public class StaciasJudgmentScript : ConfigurableSpellScriptBase
{
    private readonly IMonsterFactory MonsterFactory;

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

        source.AnimateBody(BodyAnimation);

        var shrine = MonsterFactory.Create(ShrineTemplateKey, map, spawnPoint);
        map.AddEntity(shrine, spawnPoint);

        if (shrine.Script.Is<StaciasJudgmentPulseScript>(out var pulseScript))
        {
            pulseScript.Caster = source;
            pulseScript.Filter = Filter;
            pulseScript.BaseDamage = BaseDamage;
            pulseScript.DamageStat = DamageStat;
            pulseScript.DamageStatMultiplier = DamageStatMultiplier;
            pulseScript.DamageRange = DamageRange;
            pulseScript.Animation = Animation;
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, spawnPoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the shrine on each pulse
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The flat portion of the damage dealt per pulse
    /// </summary>
    public int BaseDamage { get; init; } = 40;

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The radius around the shrine damaged on each pulse
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
    ///     The templateKey of the shrine monster to spawn
    /// </summary>
    public string ShrineTemplateKey { get; init; } = string.Empty;

    /// <summary>
    ///     Sound played at the spawn point when the shrine is summoned
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
