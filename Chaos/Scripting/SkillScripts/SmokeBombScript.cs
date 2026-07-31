#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

public class SmokeBombScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public SmokeBombScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var centerPoint = Point.From(source);

        source.AnimateBody(BodyAnimation);

        var scanArea = new Rectangle(centerPoint, (AoeRange * 2) + 1, (AoeRange * 2) + 1);

        var targets = map.GetEntitiesAtPoints<Monster>(scanArea.GetPoints())
                         .Where(monster => Filter.IsValidTarget(source, monster));

        foreach (var monster in targets)
        {
            monster.Target = null;
            monster.AggroList.Clear();
        }

        if (Animation != null)
            map.ShowAnimation(Animation.GetPointAnimation(centerPoint, source.Id));

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, centerPoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The radius, around the caster, affected by the smoke
    /// </summary>
    public int AoeRange { get; init; } = 1;

    /// <summary>
    ///     The animation played at the caster's position
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine which nearby creatures lose their target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     Sound played at the caster's position on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
