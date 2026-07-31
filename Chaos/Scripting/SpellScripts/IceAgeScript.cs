#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     Applies Slow to every hostile monster on the entire map, regardless of distance from the caster.
/// </summary>
public class IceAgeScript : ConfigurableSpellScriptBase
{
    /// <inheritdoc />
    public IceAgeScript(Spell subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        foreach (var monster in map.GetEntities<Monster>())
        {
            if (!Filter.IsValidTarget(source, monster))
                continue;

            var slowEffect = new SlowEffect
            {
                SlowAmount = SlowAmount
            };

            slowEffect.SetDuration(TimeSpan.FromMilliseconds(DurationMs));
            monster.Effects.Apply(source, slowEffect, this);

            if (Animation != null)
                monster.Animate(Animation, source.Id);
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each affected monster
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     How long, in milliseconds, the slow lasts
    /// </summary>
    public int DurationMs { get; init; } = 5000;

    /// <summary>
    ///     The filter used to determine which monsters on the map are affected
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The amount added to MovementSpeedPct - high enough to be nearly frozen
    /// </summary>
    public int SlowAmount { get; init; } = 400;

    /// <summary>
    ///     The sound played at the caster's position on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
