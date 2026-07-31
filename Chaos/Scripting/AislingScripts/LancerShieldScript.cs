#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Scripting.AislingScripts.Abstractions;
using Chaos.Scripting.EffectScripts;
#endregion

namespace Chaos.Scripting.AislingScripts;

/// <summary>
///     For Lancers, re-plays the shield's visual while Lancer's Shield is active, and terminates the effect once MP
///     (shield charge) has been fully drained. The actual damage-to-MP redirect lives in
///     <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" />, since that's the only
///     point damage is actually applied to HP and can be intercepted before the fact.
/// </summary>
public class LancerShieldScript : AislingScriptBase
{
    private static readonly TimeSpan PulseInterval = TimeSpan.FromMilliseconds(1000);

    private static readonly Animation ShieldAura = new()
    {
        TargetAnimation = 157,
        AnimationSpeed = 100
    };

    private TimeSpan SinceLastPulse = TimeSpan.Zero;

    /// <inheritdoc />
    public LancerShieldScript(Aisling subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (Subject.UserStatSheet.BaseClass != BaseClass.Lancer)
            return;

        if (!Subject.Trackers.Tags.ContainsKey(LancerShieldEffect.ShieldActiveTag))
            return;

        if (Subject.StatSheet.CurrentMp <= 0)
        {
            Subject.Effects.Terminate("Lancer's Shield");

            return;
        }

        SinceLastPulse += delta;

        if (SinceLastPulse < PulseInterval)
            return;

        SinceLastPulse = TimeSpan.Zero;
        Subject.Animate(ShieldAura, Subject.Id);
    }
}
