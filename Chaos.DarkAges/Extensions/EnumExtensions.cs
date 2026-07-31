#region
using System.Text.RegularExpressions;
using Chaos.DarkAges.Definitions;
#endregion

namespace Chaos.DarkAges.Extensions;

/// <summary>
///     Provides extension methods for various enums
/// </summary>
public static partial class EnumExtensions
{
    [GeneratedRegex("(?<=[a-z])(?=[A-Z])")]
    private static partial Regex PascalCaseBoundaryRegex();

    /// <summary>
    ///     Formats a PascalCase enum name into spaced words for display (e.g. "WeaponMaster" -&gt; "Weapon Master")
    /// </summary>
    /// <param name="baseClass">
    ///     A <see cref="BaseClass" /> value
    /// </param>
    public static string GetDisplayName(this BaseClass baseClass) => PascalCaseBoundaryRegex().Replace(baseClass.ToString(), " ");

    /// <summary>
    ///     Formats a PascalCase enum name into spaced words for display
    /// </summary>
    /// <param name="advClass">
    ///     An <see cref="AdvClass" /> value
    /// </param>
    public static string GetDisplayName(this AdvClass advClass) => PascalCaseBoundaryRegex().Replace(advClass.ToString(), " ");

    /// <summary>
    ///     Converts an <see cref="EquipmentType" /> to one or more <see cref="EquipmentSlot" />s
    /// </summary>
    /// <param name="type">
    ///     An <see cref="EquipmentType" /> value
    /// </param>
    /// <returns>
    ///     One or more <see cref="EquipmentSlot" />s associated with the given <see cref="EquipmentType" />
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the given <see cref="EquipmentType" /> is not defined
    /// </exception>
    public static IEnumerable<EquipmentSlot> ToEquipmentSlots(this EquipmentType type)
    {
        switch (type)
        {
            case EquipmentType.NotEquipment:
                yield return EquipmentSlot.None;

                yield break;
            case EquipmentType.Weapon:
                yield return EquipmentSlot.Weapon;

                yield break;
            case EquipmentType.Armor:
                yield return EquipmentSlot.Armor;

                yield break;
            case EquipmentType.OverArmor:
                yield return EquipmentSlot.Overcoat;

                yield break;
            case EquipmentType.Shield:
                yield return EquipmentSlot.Shield;

                yield break;
            case EquipmentType.Helmet:
                yield return EquipmentSlot.Helmet;

                yield break;
            case EquipmentType.OverHelmet:
                yield return EquipmentSlot.OverHelm;

                yield break;
            case EquipmentType.Earrings:
                yield return EquipmentSlot.Earrings;

                yield break;
            case EquipmentType.Necklace:
                yield return EquipmentSlot.Necklace;

                yield break;
            case EquipmentType.Ring:
                yield return EquipmentSlot.LeftRing;
                yield return EquipmentSlot.RightRing;

                yield break;
            case EquipmentType.Gauntlet:
                yield return EquipmentSlot.LeftGaunt;
                yield return EquipmentSlot.RightGaunt;

                yield break;
            case EquipmentType.Belt:
                yield return EquipmentSlot.Belt;

                yield break;
            case EquipmentType.Greaves:
                yield return EquipmentSlot.Greaves;

                yield break;
            case EquipmentType.Boots:
                yield return EquipmentSlot.Boots;

                yield break;
            case EquipmentType.Accessory:
                yield return EquipmentSlot.Accessory1;
                yield return EquipmentSlot.Accessory2;
                yield return EquipmentSlot.Accessory3;

                yield break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}