#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.World;
using Chaos.Scripting.AislingScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
using Chaos.Utilities;
#endregion

namespace Chaos.Scripting.AislingScripts;

/// <summary>
///     True always-on Sorcerer passive, same shape as <see cref="BerserkerRageScript" />: no-ops unless the
///     subject is currently a Sorcerer. Handles the two AUTOMATIC (no player choice involved) legs of Sorcerer's
///     4-tier progression - granting Shared Tier I as soon as eligible, and granting the Tier IV signature kit
///     once the player has reached the level threshold AND already resolved both element picks (via the choice
///     dialogs - see <see cref="Chaos.Scripting.DialogScripts" />). Tier II/III are NOT handled here - those are
///     choice-gated and granted directly by the dialog that records the choice, not by a background poll, so a
///     meaningful specialization decision feels like an NPC interaction rather than a silent tick.
/// </summary>
/// <remarks>
///     Throttled to <see cref="CheckInterval" /> rather than checking every frame - this is a one-shot-per-tier
///     event per player, not something that needs per-tick precision, same reasoning as
///     <see cref="BerserkerRageScript" />'s own idle-drain/aura-pulse throttling.
/// </remarks>
public class SorcererProgressionScript : AislingScriptBase
{
    /// <summary>
    ///     The highest structural tier (0-4) this player has already been auto-granted content for, persisted so a
    ///     relog/restart doesn't re-grant. Only Tiers I and IV are ever written here - II and III are recorded
    ///     implicitly by <see cref="SorcererProgressionHelper" />'s Element1/Element2 counters instead.
    /// </summary>
    private const string GrantedTierCounterKey = "sorcererTierGranted";

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);

    private readonly ISorcererModuleProvider ModuleProvider;
    private readonly ISpellFactory SpellFactory;
    private TimeSpan SinceLastCheck = TimeSpan.Zero;

    /// <inheritdoc />
    public SorcererProgressionScript(Aisling subject, ISorcererModuleProvider moduleProvider, ISpellFactory spellFactory)
        : base(subject)
    {
        ModuleProvider = moduleProvider;
        SpellFactory = spellFactory;
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (Subject.UserStatSheet.BaseClass != BaseClass.Sorcerer)
            return;

        SinceLastCheck += delta;

        if (SinceLastCheck < CheckInterval)
            return;

        SinceLastCheck = TimeSpan.Zero;

        var grantedTier = GetGrantedTier();

        if ((grantedTier < 1) && (Subject.StatSheet.Level >= 1))
        {
            SorcererProgressionHelper.GrantSpells(Subject, SpellFactory, ModuleProvider.GetTierISpellKeys());
            SetGrantedTier(1);
        }

        if ((grantedTier < 4)
            && (Subject.StatSheet.Level >= SorcererProgressionHelper.TierIVLevel)
            && SorcererProgressionHelper.TryGetElement1(Subject, out var element1)
            && SorcererProgressionHelper.TryGetElement2(Subject, out var element2))
        {
            var specialization = SorcererProgressionHelper.ResolveSpecialization(element1, element2);
            SorcererProgressionHelper.GrantSpells(Subject, SpellFactory, ModuleProvider.GetTierIVSpellKeys(specialization));
            SetGrantedTier(4);
        }
    }

    private int GetGrantedTier() => Subject.Trackers.Counters.TryGetValue(GrantedTierCounterKey, out var tier) ? tier : 0;

    private void SetGrantedTier(int tier) => Subject.Trackers.Counters.Set(GrantedTierCounterKey, tier);
}
