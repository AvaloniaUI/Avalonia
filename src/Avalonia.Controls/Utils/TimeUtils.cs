using System.Globalization;

namespace Avalonia.Controls.Utils;

internal static class TimeUtils
{
    public static string GetPMDesignator() =>
        !string.IsNullOrEmpty(CultureInfo.CurrentCulture.DateTimeFormat.PMDesignator) ?
            CultureInfo.CurrentCulture.DateTimeFormat.PMDesignator :
            CultureInfo.InvariantCulture.DateTimeFormat.PMDesignator;

    public static string GetAMDesignator() =>
        !string.IsNullOrEmpty(CultureInfo.CurrentCulture.DateTimeFormat.AMDesignator) ?
            CultureInfo.CurrentCulture.DateTimeFormat.AMDesignator :
            CultureInfo.InvariantCulture.DateTimeFormat.AMDesignator;
}
