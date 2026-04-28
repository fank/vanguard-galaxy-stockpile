using System.Globalization;

namespace VGStockpile.UI;

internal static class CompactNumber
{
    public static string Format(int value)
    {
        if (value <= 0) return "";
        if (value < 1_000) return value.ToString(CultureInfo.InvariantCulture);
        if (value < 1_000_000)
            return (value / 1_000.0).ToString("0.0", CultureInfo.InvariantCulture) + "k";
        return (value / 1_000_000.0).ToString("0.0", CultureInfo.InvariantCulture) + "M";
    }
}
