#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.MonsterScripts;
using Chaos.Scripting.SpellScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

public class StaciasShrineScript : ConfigurableSpellScriptBase
{
    private readonly IMonsterFactory MonsterFactory;

    /// <inheritdoc />
    public StaciasShrineScript(Spell subject, IMonsterFactory monsterFactory)
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

        if (!source.StatSheet.TrySubtractMp(ManaCost))
        {
            context.SourceAisling?.SendOrangeBarMessage("Not enough mana.");

            return;
        }

        context.SourceAisling?.Client.SendAttributes(StatUpdateType.Vitality);

        source.AnimateBody(BodyAnimation);

        var shrine = MonsterFactory.Create(ShrineTemplateKey, map, spawnPoint);
        map.AddEntity(shrine, spawnPoint);

        if (shrine.Script.Is<StaciasShrinePulseScript>(out var pulseScript))
        {
            pulseScript.Caster = source;
            pulseScript.Filter = Filter;
            pulseScript.HealPerTick = HealPerTick;
            pulseScript.HealRange = HealRange;
            pulseScript.Animation = Animation;

            //evolving shrine - the pulse tier is driven by how leveled up the caster's stacias_shrine spell is
            if ((source is Aisling casterAisling) && casterAisling.SpellBook.TryGetObjectByTemplateKey("stacias_shrine", out var shrineSpell))
                pulseScript.SpellLevel = shrineSpell.Level;
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
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine both the initial target's validity and which nearby Aislings the shrine heals
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The amount healed on each pulse
    /// </summary>
    public int HealPerTick { get; init; } = 60;

    /// <summary>
    ///     The radius around the shrine that gets healed on each pulse
    /// </summary>
    public int HealRange { get; init; } = 3;

    /// <summary>
    ///     The MP cost to use this spell
    /// </summary>
    public int ManaCost { get; init; }

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
