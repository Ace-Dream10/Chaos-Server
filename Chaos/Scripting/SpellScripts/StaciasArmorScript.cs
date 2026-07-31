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
///     Applies <see cref="StaciasArmorEffect" /> to every friendly Aisling on the map - same pattern as
///     <see cref="StaciasBlessingScript" />
/// </summary>
public class StaciasArmorScript : ConfigurableSpellScriptBase
{
    /// <inheritdoc />
    public StaciasArmorScript(Spell subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;

        source.AnimateBody(BodyAnimation);

        foreach (var aisling in map.GetEntities<Aisling>())
        {
            if (!Filter.IsValidTarget(source, aisling))
                continue;

            var armorEffect = new StaciasArmorEffect { AcBonus = AcBonus };
            armorEffect.SetDuration(TimeSpan.FromMilliseconds(DurationMs));
            aisling.Effects.Apply(source, armorEffect, this);
        }

        if (Animation != null)
            map.ShowAnimation(Animation.GetPointAnimation(context.SourcePoint, source.Id));

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    #region ScriptVars
    public int AcBonus { get; init; } = -20;

    /// <summary>
    ///     The animation played at the caster's position
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     How long, in milliseconds, the buff lasts
    /// </summary>
    public int DurationMs { get; init; } = 60000;

    /// <summary>
    ///     The filter used to determine which Aislings on the map are valid buff targets
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     Sound played at the caster's position on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
