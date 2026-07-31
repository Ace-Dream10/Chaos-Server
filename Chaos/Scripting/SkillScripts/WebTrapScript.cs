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
///     Places a web trap reactor tile at the caster's current position. Limited to <see cref="MaxTraps" /> active
///     traps per caster at once, tracked via <see cref="WebTrapReactorScript.TrapCountCounterKey" />.
/// </summary>
public class WebTrapScript : ConfigurableSkillScriptBase
{
    private const string TrapTemplateKey = "web_trap";

    private readonly IReactorTileFactory ReactorTileFactory;

    /// <inheritdoc />
    public WebTrapScript(Skill subject, IReactorTileFactory reactorTileFactory)
        : base(subject)
        => ReactorTileFactory = reactorTileFactory;

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var point = Point.From(source);

        if (source is Aisling aisling)
        {
            var currentCount = aisling.Trackers.Counters.TryGetValue(WebTrapReactorScript.TrapCountCounterKey, out var count) ? count : 0;

            if (currentCount >= MaxTraps)
            {
                aisling.SendOrangeBarMessage("You already have the maximum number of web traps active.");

                return;
            }

            aisling.Trackers.Counters.Set(WebTrapReactorScript.TrapCountCounterKey, currentCount + 1);
        }

        source.AnimateBody(BodyAnimation);

        var trap = ReactorTileFactory.Create(TrapTemplateKey, map, point, null, source, this);
        map.SimpleAdd(trap);

        if (Animation != null)
            map.ShowAnimation(Animation.GetPointAnimation(point, source.Id));

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, point);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played at the trap's position when it's placed
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The maximum number of traps this caster can have active at once
    /// </summary>
    public int MaxTraps { get; init; } = 2;

    /// <summary>
    ///     Sound played at the trap's position when it's placed
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
