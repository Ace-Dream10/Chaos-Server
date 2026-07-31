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

public class StaciasBlessingScript : ConfigurableSpellScriptBase
{
    /// <inheritdoc />
    public StaciasBlessingScript(Spell subject)
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

            var blessingEffect = new StaciasBlessingEffect
            {
                StrBonus = StrBonus,
                DexBonus = DexBonus,
                IntBonus = IntBonus,
                WisBonus = WisBonus,
                ConBonus = ConBonus,
                HpBonus = HpBonus,
                MpBonus = MpBonus
            };
            aisling.Effects.Apply(source, blessingEffect, this);
        }

        if (Animation != null)
            map.ShowAnimation(Animation.GetPointAnimation(context.SourcePoint, source.Id));

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.SourcePoint);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played at the caster's position
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    public int ConBonus { get; init; } = 5;
    public int DexBonus { get; init; } = 5;

    /// <summary>
    ///     The filter used to determine which Aislings on the map are valid buff targets
    /// </summary>
    public TargetFilter Filter { get; init; }

    public int HpBonus { get; init; } = 500;
    public int IntBonus { get; init; } = 5;
    public int MpBonus { get; init; } = 300;

    /// <summary>
    ///     Sound played at the caster's position on cast
    /// </summary>
    public byte? Sound { get; init; }

    public int StrBonus { get; init; } = 5;
    public int WisBonus { get; init; } = 5;
    #endregion
}
