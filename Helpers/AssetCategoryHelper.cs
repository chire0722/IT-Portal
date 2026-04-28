// Project comment: AssetCategoryHelper.cs is part of the IT Form application source code.

using System.Text;
using IT_Form.Models.Enums;

namespace IT_Form.Helpers;

/// <summary>
/// Defines the AssetCategoryHelper class used by the IT Form application.
/// </summary>
public static class AssetCategoryHelper
{
    /// <summary>
    /// Runs the readonly operation.
    /// </summary>
    private static readonly (AssetType AssetType, string? OtherAssetType)[] DefaultAccessoryCategories =
    [
        (AssetType.Others, "Power Adaptor"),
        (AssetType.Others, "Laptop Bag"),
        (AssetType.Others, "Wireless Mouse"),
        (AssetType.Others, "Wired Mouse")
    ];

    /// <summary>
    /// Runs the readonly operation.
    /// </summary>
    private static readonly (AssetType AssetType, string? OtherAssetType)[] DefaultOnboardingRequestCategories =
    [
        (AssetType.Laptop, null),
        (AssetType.Others, "Power Adaptor"),
        (AssetType.Others, "Laptop Bag"),
        (AssetType.Others, "Wireless Mouse"),
        (AssetType.Others, "Wired Mouse"),
        (AssetType.DesktopPC, null),
        (AssetType.Monitor, null),
        (AssetType.USB, null),
        (AssetType.USBLANAdaptor, null),
        (AssetType.Keyboard, null),
        (AssetType.Headset, null),
        (AssetType.DockingStation, null),
        (AssetType.NetworkCable, null),
        (AssetType.USBDrive, null),
        (AssetType.Webcam, null)
    ];

    private static readonly Dictionary<string, int> CategorySortOrder = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Laptop"] = 10,
        ["Power Adaptor"] = 20,
        ["Laptop Bag"] = 30,
        ["Wireless Mouse"] = 40,
        ["Wired Mouse"] = 45,
        ["Desktop PC"] = 50,
        ["Monitor"] = 60,
        ["USB"] = 70,
        ["USB LAN Adaptor"] = 80,
        ["Keyboard"] = 90,
        ["Headset"] = 100,
        ["Docking Station"] = 110,
        ["Network Cable"] = 120,
        ["USB Drive"] = 130,
        ["Webcam"] = 140
    };

    /// <summary>
    /// Gets the data required by the GetCategoryName operation.
    /// </summary>
    public static string GetCategoryName(AssetType assetType, string? otherAssetType = null)
    {
        if (assetType == AssetType.Others && !string.IsNullOrWhiteSpace(otherAssetType))
        {
            return otherAssetType.Trim();
        }

        return assetType switch
        {
            AssetType.DesktopPC => "Desktop PC",
            AssetType.USBLANAdaptor => "USB LAN Adaptor",
            AssetType.DockingStation => "Docking Station",
            AssetType.NetworkCable => "Network Cable",
            AssetType.USBDrive => "USB Drive",
            _ => SplitPascalCase(assetType.ToString())
        };
    }

    /// <summary>
    /// Gets the data required by the GetCategoryKey operation.
    /// </summary>
    public static string GetCategoryKey(AssetType assetType, string? otherAssetType = null)
        => assetType == AssetType.Others
            ? "Others:" + NormalizeText(otherAssetType)
            : assetType.ToString();

    /// <summary>
    /// Runs the MatchesCategory operation.
    /// </summary>
    public static bool MatchesCategory(
        AssetType leftAssetType,
        string? leftOtherAssetType,
        AssetType rightAssetType,
        string? rightOtherAssetType)
    {
        if (leftAssetType != rightAssetType)
        {
            return false;
        }

        if (leftAssetType != AssetType.Others)
        {
            return true;
        }

        return string.Equals(
            NormalizeText(leftOtherAssetType),
            NormalizeText(rightOtherAssetType),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// Runs the HasSpecifiedCategory operation.
    /// </summary>
    public static bool HasSpecifiedCategory(AssetType assetType, string? otherAssetType, string? description = null)
        => assetType != AssetType.Others
           || !string.IsNullOrWhiteSpace(otherAssetType)
           || !string.IsNullOrWhiteSpace(description);

    /// <summary>
    /// Gets the data required by the GetSortOrder operation.
    /// </summary>
    public static int GetSortOrder(AssetType assetType, string? otherAssetType = null)
    {
        var categoryName = GetCategoryName(assetType, otherAssetType);
        return CategorySortOrder.TryGetValue(categoryName, out var sortOrder)
            ? sortOrder
            : 1000;
    }

    /// <summary>
    /// Gets the data required by the GetRequestSelectionLabel operation.
    /// </summary>
    public static string GetRequestSelectionLabel(
        AssetType assetType,
        string? otherAssetType,
        bool hasSeparateAccessoryCategories)
    {
        if (assetType == AssetType.Laptop && !hasSeparateAccessoryCategories)
        {
            return "Laptop (includes a power adaptor, wired mouse, and laptop bag)";
        }

        if (assetType == AssetType.DesktopPC)
        {
            return "Desktop PC (complete set)";
        }

        return GetCategoryName(assetType, otherAssetType);
    }

    public static IReadOnlyList<(AssetType AssetType, string? OtherAssetType)> GetDefaultOnboardingRequestCategories()
        => DefaultOnboardingRequestCategories;

    /// <summary>
    /// Runs the IsDefaultOnboardingRequestCategory operation.
    /// </summary>
    public static bool IsDefaultOnboardingRequestCategory(AssetType assetType, string? otherAssetType = null)
        => DefaultOnboardingRequestCategories.Any(category =>
            MatchesCategory(category.AssetType, category.OtherAssetType, assetType, otherAssetType));

    /// <summary>
    /// Runs the NormalizeText operation.
    /// </summary>
    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Join(
            ' ',
            value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
    }

    /// <summary>
    /// Runs the SplitPascalCase operation.
    /// </summary>
    private static string SplitPascalCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (i > 0 &&
                char.IsUpper(current) &&
                (char.IsLower(value[i - 1]) || (i + 1 < value.Length && char.IsLower(value[i + 1]))))
            {
                builder.Append(' ');
            }

            builder.Append(current);
        }

        return builder.ToString();
    }
}
