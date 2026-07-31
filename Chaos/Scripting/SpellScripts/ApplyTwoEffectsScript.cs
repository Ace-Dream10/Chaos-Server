#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.SpellScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     Applies two separate effects (with independent durations) to every valid target in one AoE shape, or to a
///     single selected target. Used for "apply X, then Y lingers after X ends" spells - rather than genuinely
///     sequencing the two, both are applied simultaneously with the second effect's duration extended to cover the
///     full combined time, which reads identically since the first effect (e.g. Stasis/Root) already blocks
///     everything the second effect (e.g. Slow) would otherwise be doing anyway.
/// </summary>
public class ApplyTwoEffectsScript : ConfigurableSpellScriptBase
{
    private readonly IEffectFactory EffectFactory;

    /// <inheritdoc />
    public ApplyTwoEffectsScript(Spell subject, IEffectFactory effectFactory)
        : base(subject)
        => EffectFactory = effectFactory;

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var origin = context.TargetPoint;

        source.AnimateBody(BodyAnimation);

        IEnumerable<Creature> targets;

        if (SingleTarget)
        {
            var target = context.TargetCreature;

            targets = (target is { IsAlive: true } && Filter.IsValidTarget(source, target)) ? [target] : [];
        } else
        {
            var options = new AoeShapeOptions
            {
                Source = origin,
                Range = Range,
                Direction = source.Direction
            };

            var points = Shape.ResolvePoints(options);

            targets = map.GetEntitiesAtPoints<Creature>(points)
                         .Where(creature => Filter.IsValidTarget(source, creature));
        }

        foreach (var target in targets)
        {
            ApplyEffect(source, target, EffectKey1, Duration1Ms);
            ApplyEffect(source, target, EffectKey2, Duration2Ms);

            if (Animation != null)
                target.Animate(Animation, source.Id);
        }

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, origin);
    }

    private void ApplyEffect(Creature source, Creature target, string? effectKey, int? durationMs)
    {
        if (string.IsNullOrEmpty(effectKey))
            return;

        var effect = EffectFactory.Create(effectKey);

        if (durationMs.HasValue)
            effect.SetDuration(TimeSpan.FromMilliseconds(durationMs.Value));

        target.Effects.Apply(source, effect, this);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on each affected target
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     Duration override, in milliseconds, for <see cref="EffectKey1" />
    /// </summary>
    public int? Duration1Ms { get; init; }

    /// <summary>
    ///     Duration override, in milliseconds, for <see cref="EffectKey2" />
    /// </summary>
    public int? Duration2Ms { get; init; }

    /// <summary>
    ///     The first effect key applied
    /// </summary>
    public string? EffectKey1 { get; init; }

    /// <summary>
    ///     The second effect key applied
    /// </summary>
    public string? EffectKey2 { get; init; }

    /// <summary>
    ///     The filter used to determine which creatures are valid targets
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The radius/reach of the AoE shape (ignored when <see cref="SingleTarget" /> is true)
    /// </summary>
    public int Range { get; init; }

    /// <summary>
    ///     The shape of the AoE (ignored when <see cref="SingleTarget" /> is true)
    /// </summary>
    public AoeShape Shape { get; init; }

    /// <summary>
    ///     If true, only the explicitly selected target is affected instead of an AoE shape
    /// </summary>
    public bool SingleTarget { get; init; }

    /// <summary>
    ///     Sound played at the AoE's center / target's position on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
