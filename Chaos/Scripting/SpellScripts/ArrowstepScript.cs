#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Common;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     Windrunner (Fletcher passive) hook lives here: if the caster has learned it, using Arrowstep applies
///     <see cref="WindrunnerEffect" /> - see that effect's doc comment for the full mechanic.
/// </summary>
public class ArrowstepScript : ConfigurableSpellScriptBase
{
    /// <inheritdoc />
    public ArrowstepScript(Spell subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        if ((source is Aisling aisling) && !HasBowEquipped(aisling))
        {
            aisling.SendOrangeBarMessage("You need a bow equipped.");

            return;
        }

        if (!source.StatSheet.TrySubtractMp(ManaCost))
        {
            if (source is Aisling manaAisling)
                manaAisling.SendOrangeBarMessage("Not enough mana.");

            return;
        }

        if (source is Aisling attackerAisling)
            attackerAisling.Client.SendAttributes(StatUpdateType.Vitality);

        source.AnimateBody(BodyAnimation);

        var endPoint = source.DirectionalOffset(source.Direction, RushDistance);

        var points = source.GetDirectPath(endPoint)
                            .Skip(1);

        var lastWalkablePoint = Point.From(source);

        foreach (var point in points)
        {
            if (map.IsWall(point) || map.IsBlockingReactor(point))
                break;

            var creature = map.GetEntitiesAtPoints<Creature>(point)
                              .TopOrDefault();

            //stop just before a blocking creature - no attack
            if ((creature != null) && Filter.IsValidTarget(source, creature))
                break;

            if (Animation != null)
                map.ShowAnimation(Animation.GetPointAnimation(point, source.Id));

            lastWalkablePoint = point;
        }

        source.WarpTo(lastWalkablePoint);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, lastWalkablePoint);

        if ((source is Aisling windrunnerAisling) && windrunnerAisling.SpellBook.TryGetObjectByTemplateKey("windrunner", out _))
            windrunnerAisling.Effects.Apply(windrunnerAisling, new WindrunnerEffect(), this);
    }

    private static bool HasBowEquipped(Aisling aisling)
    {
        var weapon = aisling.Equipment[EquipmentSlot.Weapon];

        return (weapon != null) && weapon.Template.Category.EqualsI("bow");
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each tile traversed
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster at the start of the dash
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine which creatures in the path block the dash
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The MP cost to use this skill
    /// </summary>
    public int ManaCost { get; init; }

    /// <summary>
    ///     The maximum number of tiles the caster will dash forward
    /// </summary>
    public int RushDistance { get; init; } = 2;

    /// <summary>
    ///     Sound played at the landing point
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
