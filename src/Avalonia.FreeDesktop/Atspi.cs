using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tmds.DBus;

#nullable enable

[assembly: InternalsVisibleTo(Tmds.DBus.Connection.DynamicAssemblyName)]

namespace Avalonia.FreeDesktop.Atspi
{
    internal enum AtspiRole
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
        ATSPI_ROLE_LAST_DEFINED,
    };

    [DBusInterface("org.a11y.Bus")]
    internal interface IBus : IDBusObject
    {
        Task<string> GetAddressAsync();
    }

    [DBusInterface("org.a11y.atspi.Accessible")]
    internal interface IAccessible : IDBusObject
    {
        Task<(string, ObjectPath)> GetChildAtIndexAsync(int Index);
        Task<(string, ObjectPath)[]> GetChildrenAsync();
        Task<int> GetIndexInParentAsync();
        Task<(uint, (string, ObjectPath)[])[]> GetRelationSetAsync();
        Task<uint> GetRoleAsync();
        Task<string> GetRoleNameAsync();
        Task<string> GetLocalizedRoleNameAsync();
        Task<uint[]> GetStateAsync();
        Task<IDictionary<string, string>> GetAttributesAsync();
        Task<(string, ObjectPath)> GetApplicationAsync();
        Task<object?> GetAsync(string prop);
        Task<AccessibleProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [DBusInterface("org.a11y.atspi.Application")]
    internal interface IApplication : IDBusObject
    {
        Task<string> GetLocaleAsync(uint lcType);
        Task RegisterEventListenerAsync(string Event);
        Task DeregisterEventListenerAsync(string Event);
        Task<object?> GetAsync(string prop);
        Task<ApplicationProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [DBusInterface("org.a11y.atspi.Socket")]
    internal interface ISocket : IDBusObject
    {
        Task<(string socket, ObjectPath)> EmbedAsync((string, ObjectPath) Plug);
        Task UnembedAsync((string, ObjectPath) Plug);
        Task<IDisposable> WatchAvailableAsync(
            Action<(string socket, ObjectPath)> handler,
            Action<Exception> onError = null);
    }

    [Dictionary]
    internal class AccessibleProperties
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public (string, ObjectPath) Parent { get; set; }
        public int ChildCount { get; set; }
        public string Locale { get; set; }
        public string AccessibleId { get; set; }
    }
    [Dictionary]
    internal class ApplicationProperties
    {
        public string ToolkitName { get; set; }
        public string Version { get; set; }
        public string AtspiVersion { get; set; }
        public int Id { get; set; }
    }
}
