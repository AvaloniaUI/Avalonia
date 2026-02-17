using System.Collections.Generic;
using System.Globalization;

namespace Avalonia.FreeDesktop.AtSpi
{
    /// <summary>
    /// AT-SPI2 role IDs from atspi-constants.h.
    /// </summary>
    internal enum AtSpiRole
    {
        Invalid = 0,
        Accelerator = 1,
        Alert = 2,
        Animation = 3,
        Arrow = 4,
        Calendar = 5,
        Canvas = 6,
        CheckBox = 7,
        CheckMenuItem = 8,
        ColorChooser = 9,
        ColumnHeader = 10,
        ComboBox = 11,
        DateEditor = 12,
        DesktopIcon = 13,
        DesktopFrame = 14,
        Dial = 15,
        Dialog = 16,
        DirectoryPane = 17,
        DrawingArea = 18,
        FileChooser = 19,
        Filler = 20,
        FocusTraversable = 21,
        Frame = 23,
        GlassPane = 24,
        HtmlContainer = 25,
        Icon = 26,
        Image = 27,
        InternalFrame = 28,
        Label = 29,
        LayeredPane = 30,
        List = 31,
        ListItem = 32,
        Menu = 33,
        MenuBar = 34,
        MenuItem = 35,
        OptionPane = 36,
        PageTab = 37,
        PageTabList = 38,
        Panel = 39,
        PasswordText = 40,
        PopupMenu = 41,
        ProgressBar = 42,
        PushButton = 43,
        RadioButton = 44,
        RadioMenuItem = 45,
        RootPane = 46,
        RowHeader = 47,
        ScrollBar = 48,
        ScrollPane = 49,
        Separator = 50,
        Slider = 51,
        SpinButton = 52,
        SplitPane = 53,
        StatusBar = 54,
        Table = 55,
        TableCell = 56,
        TableColumnHeader = 57,
        TableRowHeader = 58,
        TearoffMenuItem = 59,
        Terminal = 60,
        Text = 61,
        ToggleButton = 62,
        ToolBar = 63,
        ToolTip = 64,
        Tree = 65,
        TreeTable = 66,
        Unknown = 67,
        Viewport = 68,
        Window = 69,
        Extended = 70,
        Header = 71,
        Footer = 72,
        Paragraph = 73,
        Ruler = 74,
        Application = 75,
        AutoComplete = 76,
        EditBar = 77,
        Embedded = 78,
        Entry = 79,
        Heading = 81,
        Page = 82,
        Document = 83,
        Section = 84,
        RedundantObject = 85,
        Form = 86,
        Link = 87,
        InputMethodWindow = 88,
        TableRow = 89,
        TreeItem = 90,
        DocumentSpreadsheet = 91,
        DocumentPresentation = 92,
        DocumentText = 93,
        DocumentWeb = 94,
        DocumentEmail = 95,
        Comment = 96,
        ListBox = 97,
        Grouping = 98,
        ImageMap = 99,
        Notification = 100,
        InfoBar = 101,
        LevelBar = 102,
        TitleBar = 103,
        BlockQuote = 104,
        Audio = 105,
        Video = 106,
        Definition = 107,
        Article = 108,
        Landmark = 109,
        Log = 110,
        Marquee = 111,
        Math = 112,
        Rating = 113,
        Timer = 114,
        Static = 116,
        MathFraction = 117,
        MathRoot = 118,
        Subscript = 119,
        Superscript = 120,
    }

    /// <summary>
    /// AT-SPI2 state IDs from atspi-constants.h.
    /// </summary>
    internal enum AtSpiState : uint
    {
        Invalid = 0,
        Active = 1,
        Armed = 2,
        Busy = 3,
        Checked = 4,
        Collapsed = 5,
        Defunct = 6,
        Editable = 7,
        Enabled = 8,
        Expandable = 9,
        Expanded = 10,
        Focusable = 11,
        Focused = 12,
        HasToolTip = 13,
        Horizontal = 14,
        Iconified = 15,
        Modal = 16,
        MultiLine = 17,
        MultiSelectable = 18,
        Opaque = 19,
        Pressed = 20,
        Resizable = 21,
        Selectable = 22,
        Selected = 23,
        Sensitive = 24,
        Showing = 25,
        SingleLine = 26,
        Stale = 27,
        Transient = 28,
        Vertical = 29,
        Visible = 30,
        ManagesDescendants = 31,
        Indeterminate = 32,
        Required = 33,
        Truncated = 34,
        Animated = 35,
        InvalidEntry = 36,
        SupportsAutoCompletion = 37,
        SelectableText = 38,
        IsDefault = 39,
        Visited = 40,
        Checkable = 41,
        HasPopup = 42,
        ReadOnly = 43,
    }

    internal static class AtSpiConstants
    {
        // D-Bus paths
        internal const string RootPath = "/org/a11y/atspi/accessible/root";
        internal const string CachePath = "/org/a11y/atspi/cache";
        internal const string NullPath = "/org/a11y/atspi/null";
        internal const string AppPathPrefix = "/org/avalonia/a11y";
        internal const string RegistryPath = "/org/a11y/atspi/registry";

        // Interface names
        internal const string IfaceAccessible = "org.a11y.atspi.Accessible";
        internal const string IfaceApplication = "org.a11y.atspi.Application";
        internal const string IfaceComponent = "org.a11y.atspi.Component";
        internal const string IfaceAction = "org.a11y.atspi.Action";
        internal const string IfaceValue = "org.a11y.atspi.Value";
        internal const string IfaceEventObject = "org.a11y.atspi.Event.Object";
        internal const string IfaceCache = "org.a11y.atspi.Cache";

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
        internal const uint CacheVersion = 1;

        internal static List<uint> BuildStateSet(IReadOnlyCollection<AtSpiState> states)
        {
            if (states == null || states.Count == 0)
                return new List<uint> { 0u, 0u };

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

            return new List<uint> { low, high };
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
            return typeof(AtSpiConstants).Assembly.GetName().Version?.ToString() ?? "0";
        }
    }
}
