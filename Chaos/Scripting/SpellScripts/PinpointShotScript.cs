#region
using Chaos.DarkAges.Definitions;
using Chaos.Extensions;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SpellScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SpellScripts;

/// <summary>
///     Renamed from Precise Shot - kept its exact heavy-single-target-hit mechanic (a direct match for the locked
///     design's "heavy single-target damage"), layered on the shared <see cref="DamageScript" /> logic exactly as
///     before. Adds the other half of the locked description ("briefly stuns the target"): reuses
///     <see cref="BlackoutEffect" /> directly for the stun (prevents attacking/casting) rather than inventing a
///     parallel stun-tag system that would also require touching <c>AttackingScript</c>/<c>CastingScript</c> - same
///     reuse decision Trickster's Crack the Whip made tonight for its own "briefly staggering" wording.
/// </summary>
public class PinpointShotScript : DamageScript
{
    /// <inheritdoc />
    public PinpointShotScript(Spell subject)
        : base(subject) { }

    /// <summary>
    ///     The MP cost to use this spell
    /// </summary>
    public int ManaCost { get; init; }

    /// <summary>
    ///     How long, in milliseconds, the target is stunned for - placeholder, not balance-tested
    /// </summary>
    public int StunDurationMs { get; init; } = 1500;

    /// <inheritdoc />
    public override void OnUse(SpellContext context)
    {
        var source = context.Source;

        if (!source.StatSheet.TrySubtractMp(ManaCost))
        {
            context.SourceAisling?.SendOrangeBarMessage("Not enough mana.");

            return;
        }

        context.SourceAisling?.Client.SendAttributes(StatUpdateType.Vitality);

        base.OnUse(context);

        if ((context.TargetCreature is { IsAlive: true } target) && Filter.IsValidTarget(source, target))
        {
            var stunEffect = new BlackoutEffect();
            stunEffect.SetDuration(TimeSpan.FromMilliseconds(StunDurationMs));
            target.Effects.Apply(source, stunEffect, this);
        }
    }
}
