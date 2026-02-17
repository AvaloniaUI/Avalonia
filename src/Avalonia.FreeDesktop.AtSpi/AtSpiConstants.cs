using System.Collections.Generic;
using System.Globalization;

namespace Avalonia.FreeDesktop.AtSpi
{
    /// <summary>
    /// Well-known AT-SPI2 D-Bus paths, interface names, and utility methods.
    /// </summary>
    internal static class AtSpiConstants
    {
        // D-Bus paths
        internal const string RootPath = "/org/a11y/atspi/accessible/root";
        internal const string CachePath = "/org/a11y/atspi/cache";
        internal const string NullPath = "/org/a11y/atspi/null";
        internal const string AppPathPrefix = "/net/avaloniaui/a11y";
        internal const string RegistryPath = "/org/a11y/atspi/registry";

        // Interface names
        internal const string IfaceAccessible = "org.a11y.atspi.Accessible";
        internal const string IfaceApplication = "org.a11y.atspi.Application";
        internal const string IfaceComponent = "org.a11y.atspi.Component";
        internal const string IfaceAction = "org.a11y.atspi.Action";
        internal const string IfaceValue = "org.a11y.atspi.Value";
        internal const string IfaceEventObject = "org.a11y.atspi.Event.Object";
        internal const string IfaceEventWindow = "org.a11y.atspi.Event.Window";
        internal const string IfaceCache = "org.a11y.atspi.Cache";
        internal const string IfaceSelection = "org.a11y.atspi.Selection";
        internal const string IfaceImage = "org.a11y.atspi.Image";
        internal const string IfaceText = "org.a11y.atspi.Text";
        internal const string IfaceEditableText = "org.a11y.atspi.EditableText";
        internal const string IfaceCollection = "org.a11y.atspi.Collection";

        // Bus names
        internal const string BusNameRegistry = "org.a11y.atspi.Registry";
        internal const string BusNameA11y = "org.a11y.Bus";
        internal const string PathA11y = "/org/a11y/bus";

        // Interface versions
        internal const uint AccessibleVersion = 1;
        internal const uint ApplicationVersion = 1;
        internal const uint ComponentVersion = 1;
        internal const uint ActionVersion = 1;
        internal const uint ValueVersion = 1;
        internal const uint EventObjectVersion = 1;
        internal const uint EventWindowVersion = 1;
        internal const uint CacheVersion = 1;
        internal const uint ImageVersion = 1;
        internal const uint SelectionVersion = 1;
        internal const uint TextVersion = 1;
        internal const uint EditableTextVersion = 1;
        internal const uint CollectionVersion = 1;

        internal static List<uint> BuildStateSet(IReadOnlyCollection<AtSpiState>? states)
        {
            if (states == null || states.Count == 0)
                return [0u, 0u];

            uint low = 0;
            uint high = 0;
            foreach (var state in states)
            {
                var bit = (uint)state;
                if (bit < 32)
                    low |= 1u << (int)bit;
                else if (bit < 64)
                    high |= 1u << (int)(bit - 32);
            }

            return [low, high];
        }

        internal static string ResolveLocale()
        {
            var culture = CultureInfo.CurrentUICulture.Name;
            if (string.IsNullOrWhiteSpace(culture))
                culture = "en_US";
            return culture.Replace('-', '_');
        }

        internal static string ResolveToolkitVersion()
        {
            // TODO: Better way of doing this?
            return typeof(AtSpiConstants).Assembly.GetName().Version?.ToString() ?? "0";
        }
    }
}
