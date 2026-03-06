#pragma warning disable CS0618 // TODO: Temporary workaround until Tmds is replaced.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.DBus;
using Avalonia.FreeDesktop.DBusXml;
using Avalonia.Input;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.FreeDesktop
{
    internal class DBusMenuExporter
    {
        public static ITopLevelNativeMenuExporter? TryCreateTopLevelNativeMenu(IntPtr xid) =>
            DBusHelper.DefaultConnection is { } conn ? new DBusMenuExporterImpl(conn, xid) : null;

        public static INativeMenuExporter TryCreateDetachedNativeMenu(string path, DBusConnection currentConnection) =>
            new DBusMenuExporterImpl(currentConnection, path);

        public static string GenerateDBusMenuObjPath => $"/net/avaloniaui/dbusmenu/{Guid.NewGuid():N}";

        private sealed class DBusMenuExporterImpl : IComCanonicalDbusmenu, ITopLevelNativeMenuExporter, IDisposable
        {
            private const string InterfaceName = "com.canonical.dbusmenu";
            private readonly DBusConnection _connection;
            private readonly Dictionary<int, NativeMenuItemBase> _idsToItems = new();
            private readonly Dictionary<NativeMenuItemBase, int> _itemsToIds = new();
            private readonly HashSet<NativeMenu> _menus = [];
            private readonly string _path;
            private readonly uint _xid;
            private readonly bool _appMenu = true;
            private ComCanonicalAppMenuRegistrarProxy? _registrar;
            private NativeMenu? _menu;
            private bool _disposed;
            private uint _revision = 1;
            private bool _resetQueued;
            private int _nextId = 1;
            private IDisposable? _registration;
            private readonly AvaloniaSynchronizationContext _synchronizationContext = new(DispatcherPriority.Input);

            public DBusMenuExporterImpl(DBusConnection connection, IntPtr xid)
            {
                Version = 4;
                _connection = connection;
                _xid = (uint)xid.ToInt32();
                _path = GenerateDBusMenuObjPath;
                SetNativeMenu([]);
                _ = InitializeAsync();
            }

            public DBusMenuExporterImpl(DBusConnection connection, string path)
            {
                Version = 4;
                _connection = connection;
                _appMenu = false;
                _path = path;
                SetNativeMenu([]);
                _ = InitializeAsync();
            }

            // IComCanonicalDbusmenu properties
            public uint Version { get; }
            public string TextDirection { get; } = "ltr";
            public string Status { get; } = "normal";
            public List<string> IconThemePath { get; } = [];

            // IComCanonicalDbusmenu methods
            public ValueTask<(uint Revision, MenuLayout Layout)> GetLayoutAsync(int parentId, int recursionDepth, List<string> propertyNames)
            {
                var menu = GetMenu(parentId);
                var layout = GetLayout(menu.item, menu.menu, recursionDepth, propertyNames?.ToArray() ?? []);
                if (!IsNativeMenuExported)
                {
                    IsNativeMenuExported = true;
                    OnIsNativeMenuExportedChanged?.Invoke(this, EventArgs.Empty);
                }

                return new ValueTask<(uint, MenuLayout)>((_revision, layout));
            }

            public ValueTask<List<MenuItemProperties>> GetGroupPropertiesAsync(List<int> ids, List<string> propertyNames)
            {
                var names = propertyNames?.ToArray() ?? [];
                var result = ids.Select(id => new MenuItemProperties(id, GetProperties(GetMenu(id), names))).ToList();
                return new ValueTask<List<MenuItemProperties>>(result);
            }

            public ValueTask<DBusVariant> GetPropertyAsync(int id, string name) =>
                new(GetProperty(GetMenu(id), name) ?? new DBusVariant(0));

            public ValueTask EventAsync(int id, string eventId, DBusVariant data, uint timestamp)
            {
                HandleEvent(id, eventId);
                return new ValueTask();
            }

            public ValueTask<List<int>> EventGroupAsync(List<MenuEvent> events)
            {
                foreach (var e in events)
                    HandleEvent(e.Id, e.EventId);
                return new ValueTask<List<int>>([]);
            }

            public ValueTask<bool> AboutToShowAsync(int id) => new(false);

            public ValueTask<(List<int> UpdatesNeeded, List<int> IdErrors)> AboutToShowGroupAsync(List<int> ids) =>
                new((new List<int>(), new List<int>()));

            private async Task InitializeAsync()
            {
                try
                {
                    _registration = await _connection.RegisterObjects(
                        (DBusObjectPath)_path,
                        new object[] { this },
                        _synchronizationContext);
                }
                catch (Exception e)
                {
                    Logger.TryGet(LogEventLevel.Error, "DBUS")
                        ?.Log(this, "Failed to register dbusmenu handler: {Exception}", e);
                    return;
                }

                if (!_appMenu)
                    return;

                _registrar = new ComCanonicalAppMenuRegistrarProxy(_connection, "com.canonical.AppMenu.Registrar", new DBusObjectPath("/com/canonical/AppMenu/Registrar"));
                try
                {
                    if (!_disposed)
                        await _registrar.RegisterWindowAsync(_xid, (DBusObjectPath)_path);
                }
                catch
                {
                    // It's not really important if this code succeeds,
                    // and it's not important to know if it succeeds
                    // since even if we register the window it's not guaranteed that
                    // menu will be actually exported
                    _registrar = null;
                }
            }

            public void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;
                // Fire and forget
                _ = _registrar?.UnregisterWindowAsync(_xid);
                _registration?.Dispose();
                _registration = null;
            }

            public bool IsNativeMenuExported { get; private set; }

            public event EventHandler? OnIsNativeMenuExportedChanged;

            public void SetNativeMenu(NativeMenu? menu)
            {
                menu ??= [];

                if (_menu is not null)
                    ((INotifyCollectionChanged)_menu.Items).CollectionChanged -= OnMenuItemsChanged;
                _menu = menu;
                ((INotifyCollectionChanged)_menu.Items).CollectionChanged += OnMenuItemsChanged;

                DoLayoutReset();
            }

            /*
                 This is basic initial implementation, so we don't actually track anything and
                 just reset the whole layout on *ANY* change

                 This is not how it should work and will prevent us from implementing various features,
                 but that's the fastest way to get things working, so...
             */
            private void DoLayoutReset()
            {
                _resetQueued = false;
                foreach (var i in _idsToItems.Values)
                    i.PropertyChanged -= OnItemPropertyChanged;
                foreach(var menu in _menus)
                    ((INotifyCollectionChanged)menu.Items).CollectionChanged -= OnMenuItemsChanged;
                _menus.Clear();
                _idsToItems.Clear();
                _itemsToIds.Clear();
                _revision++;
                EmitLayoutUpdated(_revision, 0);
            }

            private void EmitLayoutUpdated(uint revision, int parent)
            {
                var message = DBusMessage.CreateSignal(
                    (DBusObjectPath)_path,
                    InterfaceName,
                    "LayoutUpdated",
                    revision, parent);
                _ = _connection.SendMessageAsync(message);
            }

            private void QueueReset()
            {
                if(_resetQueued)
                    return;
                _resetQueued = true;
                Dispatcher.UIThread.Post(DoLayoutReset, DispatcherPriority.Background);
            }

            private (NativeMenuItemBase? item, NativeMenu? menu) GetMenu(int id)
            {
                if (id == 0)
                    return (null, _menu);
                _idsToItems.TryGetValue(id, out var item);
                return (item, (item as NativeMenuItem)?.Menu);
            }

            private void EnsureSubscribed(NativeMenu? menu)
            {
                if (menu is not null && _menus.Add(menu))
                    ((INotifyCollectionChanged)menu.Items).CollectionChanged += OnMenuItemsChanged;
            }

            private int GetId(NativeMenuItemBase item)
            {
                if (_itemsToIds.TryGetValue(item, out var id))
                    return id;
                id = _nextId++;
                _idsToItems[id] = item;
                _itemsToIds[item] = id;
                item.PropertyChanged += OnItemPropertyChanged;
                if (item is NativeMenuItem nmi)
                    EnsureSubscribed(nmi.Menu);
                return id;
            }

            private void OnMenuItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => QueueReset();

            private void OnItemPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => QueueReset();

            private static readonly string[] s_allProperties = ["type", "label", "enabled", "visible", "shortcut", "toggle-type", "children-display", "toggle-state", "icon-data"];

            private static DBusVariant? GetProperty((NativeMenuItemBase? item, NativeMenu? menu) i, string name)
            {
                var (it, menu) = i;

                if (it is NativeMenuItemSeparator)
                {
                    if (name == "type")
                        return new DBusVariant("separator");
                }
                else if (it is NativeMenuItem item)
                {
                    if (name == "type")
                        return null;
                    if (name == "label")
                        return new DBusVariant(item.Header ?? "<null>");
                    if (name == "enabled")
                    {
                        if (item.Menu is not null && item.Menu.Items.Count == 0)
                            return new DBusVariant(false);
                        if (!item.IsEnabled)
                            return new DBusVariant(false);
                        return null;
                    }

                    if (name == "visible")
                        return new DBusVariant(item.IsVisible);

                    if (name == "shortcut")
                    {
                        if (item.Gesture is null)
                            return null;
                        if (item.Gesture.KeyModifiers == 0)
                            return null;
                        var lst = new List<string>();
                        var mod = item.Gesture;
                        if (mod.KeyModifiers.HasAllFlags(KeyModifiers.Control))
                            lst.Add("Control");
                        if (mod.KeyModifiers.HasAllFlags(KeyModifiers.Alt))
                            lst.Add("Alt");
                        if (mod.KeyModifiers.HasAllFlags(KeyModifiers.Shift))
                            lst.Add("Shift");
                        if (mod.KeyModifiers.HasAllFlags(KeyModifiers.Meta))
                            lst.Add("Super");
                        lst.Add(item.Gesture.Key.ToString());
                        return new DBusVariant(new List<List<string>> { lst });
                    }

                    if (name == "toggle-type")
                    {
                        if (item.ToggleType == MenuItemToggleType.CheckBox)
                            return new DBusVariant("checkmark");
                        if (item.ToggleType == MenuItemToggleType.Radio)
                            return new DBusVariant("radio");
                    }

                    if (name == "toggle-state" && item.ToggleType != MenuItemToggleType.None)
                        return new DBusVariant(item.IsChecked ? 1 : 0);

                    if (name == "icon-data")
                    {
                        if (item.Icon is not null)
                        {
                            var loader = AvaloniaLocator.Current.GetService<IPlatformIconLoader>();

                            if (loader is not null)
                            {
                                var icon = loader.LoadIcon(item.Icon.PlatformImpl.Item);
                                using var ms = new MemoryStream();
                                icon.Save(ms);
                                return new DBusVariant(new List<byte>(ms.ToArray()));
                            }
                        }
                    }

                    if (name == "children-display")
                    {
                        if (menu is not null)
                            return new DBusVariant("submenu");
                        return null;
                    }
                }

                return null;
            }

            private static Dictionary<string, DBusVariant> GetProperties((NativeMenuItemBase? item, NativeMenu? menu) i, string[] names)
            {
                if (names.Length == 0)
                    names = s_allProperties;
                var properties = new Dictionary<string, DBusVariant>();
                foreach (var n in names)
                {
                    var v = GetProperty(i, n);
                    if (v is not null)
                        properties.Add(n, v);
                }

                return properties;
            }

            private MenuLayout GetLayout(NativeMenuItemBase? item, NativeMenu? menu, int depth, string[] propertyNames)
            {
                var id = item is null ? 0 : GetId(item);
                var props = GetProperties((item, menu), propertyNames);
                var children = depth == 0 || menu is null ? new List<DBusVariant>() : new List<DBusVariant>(menu.Items.Count);
                if (menu is not null && depth != 0)
                {
                    for (var c = 0; c < menu.Items.Count; c++)
                    {
                        var ch = menu.Items[c];
                        var layout = GetLayout(ch, (ch as NativeMenuItem)?.Menu, depth == -1 ? -1 : depth - 1, propertyNames);
                        children.Add(new DBusVariant(layout.ToDbusStruct()));
                    }
                }

                return new MenuLayout(id, props, children);
            }

            private void HandleEvent(int id, string eventId)
            {
                if (eventId == "clicked")
                {
                    var item = GetMenu(id).item;
                    if (item is NativeMenuItem { IsEnabled: true } and INativeMenuItemExporterEventsImplBridge bridge)
                        bridge.RaiseClicked();
                }
            }
        }
    }
}
