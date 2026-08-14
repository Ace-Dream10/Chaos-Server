#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.MonsterScripts;
using Chaos.Scripting.SpellScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     Reworked per playtest feedback from entity-targeted to ground-targeted casting - "summon a shrine where I
///     click," not "summon a shrine on top of whichever ally I selected." Uses the same
///     <c>groundTargeted</c>/<see cref="SpellContext" /> point-based-target pipeline proven out by
///     <c>test_ground_target.json</c> earlier this session. <see cref="Range" /> now gates cast distance directly
///     (there was no distance check at all before, since the old entity-target flow relied on the target already
///     being a valid selectable creature within normal selection range).
/// </summary>
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

        if (context.SourcePoint.ManhattanDistanceFrom(context.TargetPoint) > Range)
        {
            context.SourceAisling?.SendOrangeBarMessage("Too far away.");

            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var spawnPoint = context.TargetPoint;

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
    ///     The filter used to determine which nearby Aislings the shrine heals
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
    ///     The maximum distance, in tiles, the clicked ground point can be cast at
    /// </summary>
    public int Range { get; init; } = 8;

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
