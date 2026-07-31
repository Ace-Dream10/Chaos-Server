#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.FunctionalScripts.Abstractions;
using Chaos.Scripting.FunctionalScripts.ApplyDamage;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

public class CaptureSpellScript : ConfigurableSpellScriptBase
{
    /// <inheritdoc />
    public CaptureSpellScript(Spell subject)
        : base(subject)
        => ApplyDamageScript = ApplyAttackDamageScript.Create();

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

        if (context.SourcePoint.ManhattanDistanceFrom(context.TargetPoint) > Range)
        {
            context.SourceAisling?.SendOrangeBarMessage("Your target is too far away.");

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

        if (!source.StatSheet.TrySubtractMp(ManaCost))
        {
            context.SourceAisling?.SendOrangeBarMessage("Not enough focus.");

            return;
        }

        context.SourceAisling?.Client.SendAttributes(StatUpdateType.Vitality);

        source.AnimateBody(BodyAnimation);

        var slowEffect = new SlowEffect
        {
            SlowAmount = SlowAmount
        };
        slowEffect.SetDuration(TimeSpan.FromMilliseconds(EffectDurationMs));
        target.Effects.Apply(source, slowEffect, this);

        if (BaseDamage is > 0)
            ApplyDamageScript.ApplyDamage(source, target, this, BaseDamage.Value);

        if (HitAnimation != null)
            target.Animate(HitAnimation, source.Id);

        if (OverlayAnimation != null)
            map.ShowAnimation(OverlayAnimation.GetPointAnimation(context.TargetPoint, source.Id));

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, context.TargetPoint);
    }

    #region ScriptVars
    public IApplyDamageScript ApplyDamageScript { get; init; }

    /// <summary>
    ///     Flat damage dealt on hit, for immediate feedback alongside the slow
    /// </summary>
    public int? BaseDamage { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     How long, in milliseconds, the target is slowed for
    /// </summary>
    public int EffectDurationMs { get; init; } = 4000;

    /// <summary>
    ///     The filter used to determine whether the selected target is valid
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The animation played on the target on hit
    /// </summary>
    public Animation? HitAnimation { get; init; }

    /// <summary>
    ///     The MP cost to use this spell
    /// </summary>
    public int ManaCost { get; init; }

    /// <summary>
    ///     The animation played as an overlay, centered on the target's tile
    /// </summary>
    public Animation? OverlayAnimation { get; init; }

    /// <summary>
    ///     The maximum distance, in tiles, a target can be selected from
    /// </summary>
    public int Range { get; init; }

    /// <summary>
    ///     Whether this spell only ever affects a single target
    /// </summary>
    public bool SingleTarget { get; init; }

    /// <summary>
    ///     The amount added to the target's MovementSpeedPct while slowed - passed through to SlowEffect on apply
    /// </summary>
    public int SlowAmount { get; init; } = 250;

    /// <summary>
    ///     The sound played on hit
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
