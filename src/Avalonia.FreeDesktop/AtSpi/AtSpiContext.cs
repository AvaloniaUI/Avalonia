using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Automation.Peers;
using Avalonia.Reactive;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace Avalonia.FreeDesktop.AtSpi;

internal class AtSpiContext
{
    // Reference: https://github.com/GNOME/gtk/blob/main/gtk/a11y/gtkatspiprivate.h
    enum AtspiRole : uint
    {
        ATSPI_ROLE_INVALID,
        ATSPI_ROLE_ACCELERATOR_LABEL,
        ATSPI_ROLE_ALERT,
        ATSPI_ROLE_ANIMATION,
        ATSPI_ROLE_ARROW,
        ATSPI_ROLE_CALENDAR,
        ATSPI_ROLE_CANVAS,
        ATSPI_ROLE_CHECK_BOX,
        ATSPI_ROLE_CHECK_MENU_ITEM,
        ATSPI_ROLE_COLOR_CHOOSER,
        ATSPI_ROLE_COLUMN_HEADER,
        ATSPI_ROLE_COMBO_BOX,
        ATSPI_ROLE_DATE_EDITOR,
        ATSPI_ROLE_DESKTOP_ICON,
        ATSPI_ROLE_DESKTOP_FRAME,
        ATSPI_ROLE_DIAL,
        ATSPI_ROLE_DIALOG,
        ATSPI_ROLE_DIRECTORY_PANE,
        ATSPI_ROLE_DRAWING_AREA,
        ATSPI_ROLE_FILE_CHOOSER,
        ATSPI_ROLE_FILLER,
        ATSPI_ROLE_FOCUS_TRAVERSABLE,
        ATSPI_ROLE_FONT_CHOOSER,
        ATSPI_ROLE_FRAME,
        ATSPI_ROLE_GLASS_PANE,
        ATSPI_ROLE_HTML_CONTAINER,
        ATSPI_ROLE_ICON,
        ATSPI_ROLE_IMAGE,
        ATSPI_ROLE_INTERNAL_FRAME,
        ATSPI_ROLE_LABEL,
        ATSPI_ROLE_LAYERED_PANE,
        ATSPI_ROLE_LIST,
        ATSPI_ROLE_LIST_ITEM,
        ATSPI_ROLE_MENU,
        ATSPI_ROLE_MENU_BAR,
        ATSPI_ROLE_MENU_ITEM,
        ATSPI_ROLE_OPTION_PANE,
        ATSPI_ROLE_PAGE_TAB,
        ATSPI_ROLE_PAGE_TAB_LIST,
        ATSPI_ROLE_PANEL,
        ATSPI_ROLE_PASSWORD_TEXT,
        ATSPI_ROLE_POPUP_MENU,
        ATSPI_ROLE_PROGRESS_BAR,
        ATSPI_ROLE_PUSH_BUTTON,
        ATSPI_ROLE_RADIO_BUTTON,
        ATSPI_ROLE_RADIO_MENU_ITEM,
        ATSPI_ROLE_ROOT_PANE,
        ATSPI_ROLE_ROW_HEADER,
        ATSPI_ROLE_SCROLL_BAR,
        ATSPI_ROLE_SCROLL_PANE,
        ATSPI_ROLE_SEPARATOR,
        ATSPI_ROLE_SLIDER,
        ATSPI_ROLE_SPIN_BUTTON,
        ATSPI_ROLE_SPLIT_PANE,
        ATSPI_ROLE_STATUS_BAR,
        ATSPI_ROLE_TABLE,
        ATSPI_ROLE_TABLE_CELL,
        ATSPI_ROLE_TABLE_COLUMN_HEADER,
        ATSPI_ROLE_TABLE_ROW_HEADER,
        ATSPI_ROLE_TEAROFF_MENU_ITEM,
        ATSPI_ROLE_TERMINAL,
        ATSPI_ROLE_TEXT,
        ATSPI_ROLE_TOGGLE_BUTTON,
        ATSPI_ROLE_TOOL_BAR,
        ATSPI_ROLE_TOOL_TIP,
        ATSPI_ROLE_TREE,
        ATSPI_ROLE_TREE_TABLE,
        ATSPI_ROLE_UNKNOWN,
        ATSPI_ROLE_VIEWPORT,
        ATSPI_ROLE_WINDOW,
        ATSPI_ROLE_EXTENDED,
        ATSPI_ROLE_HEADER,
        ATSPI_ROLE_FOOTER,
        ATSPI_ROLE_PARAGRAPH,
        ATSPI_ROLE_RULER,
        ATSPI_ROLE_APPLICATION,
        ATSPI_ROLE_AUTOCOMPLETE,
        ATSPI_ROLE_EDITBAR,
        ATSPI_ROLE_EMBEDDED,
        ATSPI_ROLE_ENTRY,
        ATSPI_ROLE_CHART,
        ATSPI_ROLE_CAPTION,
        ATSPI_ROLE_DOCUMENT_FRAME,
        ATSPI_ROLE_HEADING,
        ATSPI_ROLE_PAGE,
        ATSPI_ROLE_SECTION,
        ATSPI_ROLE_REDUNDANT_OBJECT,
        ATSPI_ROLE_FORM,
        ATSPI_ROLE_LINK,
        ATSPI_ROLE_INPUT_METHOD_WINDOW,
        ATSPI_ROLE_TABLE_ROW,
        ATSPI_ROLE_TREE_ITEM,
        ATSPI_ROLE_DOCUMENT_SPREADSHEET,
        ATSPI_ROLE_DOCUMENT_PRESENTATION,
        ATSPI_ROLE_DOCUMENT_TEXT,
        ATSPI_ROLE_DOCUMENT_WEB,
        ATSPI_ROLE_DOCUMENT_EMAIL,
        ATSPI_ROLE_COMMENT,
        ATSPI_ROLE_LIST_BOX,
        ATSPI_ROLE_GROUPING,
        ATSPI_ROLE_IMAGE_MAP,
        ATSPI_ROLE_NOTIFICATION,
        ATSPI_ROLE_INFO_BAR,
        ATSPI_ROLE_LEVEL_BAR,
        ATSPI_ROLE_TITLE_BAR,
        ATSPI_ROLE_BLOCK_QUOTE,
        ATSPI_ROLE_AUDIO,
        ATSPI_ROLE_VIDEO,
        ATSPI_ROLE_DEFINITION,
        ATSPI_ROLE_ARTICLE,
        ATSPI_ROLE_LANDMARK,
        ATSPI_ROLE_LOG,
        ATSPI_ROLE_MARQUEE,
        ATSPI_ROLE_MATH,
        ATSPI_ROLE_RATING,
        ATSPI_ROLE_TIMER,
        ATSPI_ROLE_STATIC,
        ATSPI_ROLE_MATH_FRACTION,
        ATSPI_ROLE_MATH_ROOT,
        ATSPI_ROLE_SUBSCRIPT,
        ATSPI_ROLE_SUPERSCRIPT,
        ATSPI_ROLE_DESCRIPTION_LIST,
        ATSPI_ROLE_DESCRIPTION_TERM,
        ATSPI_ROLE_DESCRIPTION_VALUE,
        ATSPI_ROLE_FOOTNOTE,
        ATSPI_ROLE_CONTENT_DELETION,
        ATSPI_ROLE_CONTENT_INSERTION,
        ATSPI_ROLE_MARK,
        ATSPI_ROLE_SUGGESTION,
        ATSPI_ROLE_LAST_DEFINED,
    }

    class RootCache : OrgA11yAtspiCache
    {
        // Reference from https://github.com/KDE/qtatspi/blob/master/src/struct_marshallers.h#L67
        /* QSpiAccessibleCacheArray */
        /*---------------------------------------------------------------------------*/
        // struct QSpiAccessibleCacheItem
        // {
        //     QSpiObjectReference         path;
        //     QSpiObjectReference         application;
        //     QSpiObjectReference         parent;
        //     QList <QSpiObjectReference> children;
        //     QStringList                 supportedInterfaces;
        //     QString                     name;
        //     uint                        role;
        //     QString                     description;
        //     QSpiUIntList                state;
        // };

        public override Connection Connection { get; }

        protected override
            ValueTask<((string, ObjectPath), (string, ObjectPath), (string, ObjectPath)[], string[], string, uint,
                string, uint[])[]> OnGetItemsAsync()
        {
            return default;
        }
    }

    class RootAccessible : OrgA11yAtspiAccessible
    {
        public override Connection Connection { get; }

        protected override async ValueTask<(string, ObjectPath)> OnGetChildAtIndexAsync(int index)
        {
            return default;
        }

        protected override async ValueTask<(string, ObjectPath)[]> OnGetChildrenAsync()
        {
            return default;
        }

        protected override async ValueTask<int> OnGetIndexInParentAsync()
        {
            return default;
        }

        protected override async ValueTask<(uint, (string, ObjectPath)[])[]> OnGetRelationSetAsync()
        {
            return default;
        }

        protected override async ValueTask<uint> OnGetRoleAsync()
        {
            return default;
        }

        protected override async ValueTask<string> OnGetRoleNameAsync()
        {
            return default;
        }

        protected override async ValueTask<string> OnGetLocalizedRoleNameAsync()
        {
            return default;
        }

        protected override async ValueTask<uint[]> OnGetStateAsync()
        {
            return default;
        }

        protected override async ValueTask<Dictionary<string, string>> OnGetAttributesAsync()
        {
            return default;
        }

        protected override async ValueTask<(string, ObjectPath)> OnGetApplicationAsync()
        {
            return default;
        }

        protected override async ValueTask<string[]> OnGetInterfacesAsync()
        {
            return ["org.a11y.atspi.Accessible", "org.a11y.atspi.Application"];
        }
    }

    class RootApplication : OrgA11yAtspiApplication
    {
        public RootApplication()
        {
            AtspiVersion = _AtspiVersion;
            ToolkitName = "Avalonia";
            Id = 0;
            Version = typeof(RootApplication).Assembly.GetName().Version?.ToString();
        }

        private const string _AtspiVersion = "2.1";

        public override Connection Connection { get; }

        protected override async ValueTask<string> OnGetLocaleAsync(uint lctype)
        {
            return Environment.GetEnvironmentVariable("LANG") ?? string.Empty;
        }
    }

    private static bool s_instanced;
    private static RootCache cache;
    private static string? serviceName;
    private Connection _connection;
    // private   OrgA11yAtspiRegistry _atspiRegistry;

    public AtSpiContext(Connection connection)
    {
        _connection = connection;

        var ac0 = new RootAccessible();
        var ac1 = new RootApplication();
        var path = "/org/a11y/atspi/accessible/root";
        var pathHandler = new PathHandler(path);

        pathHandler.Add(ac0);
        pathHandler.Add(ac1);

        _connection.AddMethodHandler(pathHandler);
        var socket = new OrgA11yAtspiSocket(_connection, "org.a11y.atspi.Registry", RootPath);

        var res = socket.EmbedAsync((serviceName, new ObjectPath(RootPath))!).GetAwaiter().GetResult();

        if (res is { } && res.Item1.StartsWith(":1.") && res.Item2.ToString() == RootPath)
        {
        }
    }


    public const string RootPath = "/org/a11y/atspi/accessible/root";

    public static AtSpiContext? Instance { get; private set; }

    public Connection Connection => _connection;

    public void RegisterRootAutomationPeer(AutomationPeer peer)
    {
    }

    public static async void Initialize()
    {
        if (s_instanced || DBusHelper.Connection is not { } sessionConnection) return;

        var bus1 = new OrgA11yBus(sessionConnection, "org.a11y.Bus", "/org/a11y/bus");

        var address = await bus1.GetAddressAsync();

        if (DBusHelper.TryCreateNewConnection(address) is not { } a11YConnection) return;

        await a11YConnection.ConnectAsync();


        cache = new RootCache();

        var cachePathHandler = new PathHandler("/org/a11y/atspi/cache");

        cachePathHandler.Add(cache);

        a11YConnection.AddMethodHandler(cachePathHandler);

        serviceName = a11YConnection.UniqueName;
        Instance = new AtSpiContext(a11YConnection);


        s_instanced = true;
    }
}
