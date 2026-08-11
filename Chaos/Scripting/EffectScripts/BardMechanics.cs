#region
using Chaos.Models.World.Abstractions;
#endregion

namespace Chaos.Scripting.EffectScripts;

/// <summary>
///     Shared helper for Bard's Stacia's Blessing Tier IV "CC Resistance" - scoped to just
///     <see cref="RootEffect" />/<see cref="SlowEffect" /> (the two clearest "crowd control" effects already in the
///     game) rather than every debuff, since there's no shared "ApplyDebuff" choke point analogous to
///     ApplyAttackDamageScript/ApplyHealScript to hook a fully general version into. Flagged as a scoped
///     simplification, not an attempt at a comprehensive CC-resistance system.
/// </summary>
public static class BardMechanics
{
    /// <summary>
    ///     Rolls the target's CC-resist chance (from Stacia's Blessing Tier IV, if active) and returns true if the
    ///     incoming crowd-control effect should be resisted (not applied)
    /// </summary>
    public static bool TryResistCc(Creature target)
    {
        if (!target.Effects.TryGetEffect("Stacia's Blessing", out var rawEffect) || (rawEffect is not StaciasBlessingEffect blessing))
            return false;

        if (blessing.CcResistPct <= 0)
            return false;

        return Random.Shared.NextDouble() < (blessing.CcResistPct / 100d);
    }
}
