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
///     before. Adds the other half of the locked description ("briefly stuns the target"): applies BOTH
///     <see cref="BlackoutEffect" /> (prevents attacking/casting) AND <see cref="RootEffect" /> (prevents moving)
///     together for a real, full stun. Per playtest feedback ("Pinpoint Shot doesn't actually stop/stun the enemy
///     at all"), Blackout alone was the confirmed gap - it only blocks the target's own attacks/casts, so a
///     "stunned" monster could still walk right up to and past the player, which doesn't read as a stun at all.
///     Neither effect alone is a full stun in this engine - see builder.md's CC gotchas.
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
            var blackoutEffect = new BlackoutEffect();
            blackoutEffect.SetDuration(TimeSpan.FromMilliseconds(StunDurationMs));
            target.Effects.Apply(source, blackoutEffect, this);

            var rootEffect = new RootEffect();
            rootEffect.SetDuration(TimeSpan.FromMilliseconds(StunDurationMs));
            target.Effects.Apply(source, rootEffect, this);
        }
    }
}
