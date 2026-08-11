#region
using Chaos.Models.Data;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     A direct build - "bind your soul to an ally" had no existing precedent. Applied to BOTH participants
///     (each pointing at the other via <see cref="Partner" />) so either side's heals can flow to the other - see
///     ApplyHealScript's Soul Tether hook for the actual share mechanic and its recursion guard. Not evolving (the
///     locked design only sketches future evolutions, "could share minor buffs or reduce damage taken," as
///     speculative - not built tonight). If either side's effect terminates (recast, death, logout), only that
///     direction breaks - recasting Soul Tether is how the bond is refreshed/re-established, a deliberately simple
///     placeholder rather than a fully symmetric unbind.
/// </summary>
public sealed class SoulTetherEffect : EffectBase
{
    protected override TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    ///     The other half of the bond - the creature this Subject's incoming heals partially share with
    /// </summary>
    public Creature? Partner { get; set; }

    public override byte Icon => 56;
    public override string Name => "Soul Tether";
}
