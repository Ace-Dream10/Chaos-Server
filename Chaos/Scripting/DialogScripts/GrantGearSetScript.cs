#region
using Chaos.Models.Menu;
using Chaos.Models.World;
using Chaos.Scripting.DialogScripts.Abstractions;
using Chaos.Services.Factories.Abstractions;
#endregion

namespace Chaos.Scripting.DialogScripts;

/// <summary>
///     GM testing tool: grants a full fl1-fl{FloorCount} run of a class's helm and/or armor set (gendered,
///     "fl{floor}_{helm|armor}_{ClassKey}_{Gender}"), plus any floored weapon prefixes ("{prefix}_{floor}", e.g.
///     "lancer_wep" or the WeaponMaster-subclass prefixes like "wm_blunt") and any one-off non-floored extras (e.g.
///     Archer's "rook_bow"). Silently skips any templateKey that doesn't exist or any grant that fails because the
///     inventory is full, and reports how many items actually landed.
/// </summary>
public class GrantGearSetScript : ConfigurableDialogScriptBase
{
    private readonly IItemFactory ItemFactory;

    /// <inheritdoc />
    public GrantGearSetScript(Dialog subject, IItemFactory itemFactory)
        : base(subject)
        => ItemFactory = itemFactory;

    /// <inheritdoc />
    public override void OnDisplaying(Aisling source)
    {
        var granted = 0;
        var attempted = 0;

        if (GrantHelm)
            (granted, attempted) = Accumulate(GrantFloored(source, $"fl{{0}}_helm_{ClassKey}_{Gender}"), granted, attempted);

        if (GrantArmor)
            (granted, attempted) = Accumulate(GrantFloored(source, $"fl{{0}}_armor_{ClassKey}_{Gender}"), granted, attempted);

        foreach (var prefix in WeaponKeyPrefixes)
            (granted, attempted) = Accumulate(GrantFloored(source, $"{prefix}_{{0}}"), granted, attempted);

        foreach (var key in ExtraItemKeys)
        {
            attempted++;

            if (TryGrant(source, key))
                granted++;
        }

        source.SendOrangeBarMessage(
            (attempted == 0)
                ? "Nothing to grant."
                : $"Granted {granted}/{attempted} items{(granted < attempted ? " (inventory may be full)" : "")}.");
    }

    private static (int Granted, int Attempted) Accumulate((int Granted, int Attempted) result, int granted, int attempted)
        => (granted + result.Granted, attempted + result.Attempted);

    private (int Granted, int Attempted) GrantFloored(Aisling source, string patternWithFloorPlaceholder)
    {
        var granted = 0;

        for (var floor = 1; floor <= FloorCount; floor++)
            if (TryGrant(source, string.Format(patternWithFloorPlaceholder, floor)))
                granted++;

        return (granted, FloorCount);
    }

    private bool TryGrant(Aisling source, string templateKey)
    {
        try
        {
            var item = ItemFactory.Create(templateKey);

            return source.Inventory.TryAddToNextSlot(item);
        } catch
        {
            //template doesn't exist - skip gracefully
            return false;
        }
    }

    #region ScriptVars
    /// <summary>
    ///     The class slug used in "fl{floor}_helm_{ClassKey}_{Gender}" / "fl{floor}_armor_{ClassKey}_{Gender}"
    ///     (e.g. "lancer", "martial_artist")
    /// </summary>
    public string ClassKey { get; init; } = string.Empty;

    /// <summary>
    ///     Non-floored one-off items to grant a single copy of (e.g. Archer's "rook_bow")
    /// </summary>
    public ICollection<string> ExtraItemKeys { get; init; } = [];

    /// <summary>
    ///     The highest floor tier to grant (grants fl1 through this value)
    /// </summary>
    public int FloorCount { get; init; } = 10;

    /// <summary>
    ///     "m" or "f" - used in the helm/armor templateKey pattern
    /// </summary>
    public string Gender { get; init; } = "m";

    /// <summary>
    ///     Whether to grant the fl1-fl{FloorCount} armor set for this class/gender
    /// </summary>
    public bool GrantArmor { get; init; }

    /// <summary>
    ///     Whether to grant the fl1-fl{FloorCount} helm set for this class/gender
    /// </summary>
    public bool GrantHelm { get; init; }

    /// <summary>
    ///     Weapon templateKey prefixes to grant fl1-fl{FloorCount} of (e.g. "lancer_wep", "lancer_shield",
    ///     "wm_blunt" for Berserker, "wm_polearm" for Valkyrie, "wm_twohanded" for Slayer)
    /// </summary>
    public ICollection<string> WeaponKeyPrefixes { get; init; } = [];
    #endregion
}
