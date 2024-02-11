using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Threading;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace Avalonia.FreeDesktop
{
    internal class DBusMenuExporter
    {
        public static ITopLevelNativeMenuExporter? TryCreateTopLevelNativeMenu(IntPtr xid) =>
            DBusHelper.Connection is null ? null : new DBusMenuExporterImpl(DBusHelper.Connection, xid);

        public static INativeMenuExporter TryCreateDetachedNativeMenu(string path, Connection currentConnection) =>
            new DBusMenuExporterImpl(currentConnection, path);

        public static string GenerateDBusMenuObjPath => $"/net/avaloniaui/dbusmenu/{Guid.NewGuid():N}";

        private class DBusMenuExporterImpl : ComCanonicalDbusmenu, ITopLevelNativeMenuExporter, IDisposable
        {
            private readonly Dictionary<int, NativeMenuItemBase> _idsToItems = new();
            private readonly Dictionary<NativeMenuItemBase, int> _itemsToIds = new();
            private readonly HashSet<NativeMenu> _menus = new();
            private readonly uint _xid;
            private readonly bool _appMenu = true;
            private ComCanonicalAppMenuRegistrar? _registrar;
            private NativeMenu? _menu;
            private bool _disposed;
            private uint _revision = 1;
            private bool _resetQueued;
            private int _nextId = 1;

            public DBusMenuExporterImpl(Connection connection, IntPtr xid)
            {
                InitBackingProperties();
                Connection = connection;
                _xid = (uint)xid.ToInt32();
                Path = GenerateDBusMenuObjPath;
                SetNativeMenu(new NativeMenu());
                _ = InitializeAsync();
            }

            public DBusMenuExporterImpl(Connection connection, string path)
            {
                InitBackingProperties();
                Connection = connection;
                _appMenu = false;
                Path = path;
                SetNativeMenu(new NativeMenu());
                _ = InitializeAsync();
            }

            private void InitBackingProperties()
            {
                BackingProperties.Version = 4;
                BackingProperties.Status = string.Empty;
                BackingProperties.TextDirection = string.Empty;
                BackingProperties.IconThemePath = Array.Empty<string>();
            }

            protected override Connection Connection { get; }

            public override string Path { get; }

            protected override ValueTask<(uint revision, (int, Dictionary<string, DBusVariantItem>, DBusVariantItem[]) layout)> OnGetLayoutAsync(int parentId, int recursionDepth, string[] propertyNames)
            {
                var menu = GetMenu(parentId);
                var layout = GetLayout(menu.item, menu.menu, recursionDepth, propertyNames);
                if (!IsNativeMenuExported)
                {
                    IsNativeMenuExported = true;
                    OnIsNativeMenuExportedChanged?.Invoke(this, EventArgs.Empty);
                }

                return new ValueTask<(uint, (int, Dictionary<string, DBusVariantItem>, DBusVariantItem[]))>((_revision, layout));
            }

            protected override ValueTask<(int, Dictionary<string, DBusVariantItem>)[]> OnGetGroupPropertiesAsync(int[] ids, string[] propertyNames)
                => new(ids.Select(id => (id, GetProperties(GetMenu(id), propertyNames))).ToArray());

            protected override ValueTask<DBusVariantItem> OnGetPropertyAsync(int id, string name) =>
                new(GetProperty(GetMenu(id), name) ?? new DBusVariantItem("i", new DBusInt32Item(0)));

            protected override ValueTask OnEventAsync(int id, string eventId, DBusVariantItem data, uint timestamp)
            {
                HandleEvent(id, eventId);
                return new ValueTask();
            }

            protected override ValueTask<int[]> OnEventGroupAsync((int, string, DBusVariantItem, uint)[] events)
            {
                foreach (var e in events)
                    HandleEvent(e.Item1, e.Item2);
                return new ValueTask<int[]>(Array.Empty<int>());
            }

            protected override ValueTask<bool> OnAboutToShowAsync(int id) => new(false);

            protected override ValueTask<(int[] updatesNeeded, int[] idErrors)> OnAboutToShowGroupAsync(int[] ids) =>
                new((Array.Empty<int>(), Array.Empty<int>()));

            private async Task InitializeAsync()
            {
                Connection.AddMethodHandler(this);
                if (!_appMenu)
                    return;

                _registrar = new ComCanonicalAppMenuRegistrar(Connection, "com.canonical.AppMenu.Registrar", "/com/canonical/AppMenu/Registrar");
                try
                {
                    if (!_disposed)
                        await _registrar.RegisterWindowAsync(_xid, Path);
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
            }



            public bool IsNativeMenuExported { get; private set; }

            public event EventHandler? OnIsNativeMenuExportedChanged;

            public void SetNativeMenu(NativeMenu? menu)
            {
                menu ??= new NativeMenu();

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

            private static readonly string[] s_allProperties = {
                "type", "label", "enabled", "visible", "shortcut", "toggle-type", "children-display", "toggle-state", "icon-data"
            };

            private static DBusVariantItem? GetProperty((NativeMenuItemBase? item, NativeMenu? menu) i, string name)
            {
                var (it, menu) = i;

                if (it is NativeMenuItemSeparator)
                {
                    if (name == "type")
                        return new DBusVariantItem("s", new DBusStringItem("separator"));
                }
                else if (it is NativeMenuItem item)
                {
                    if (name == "type")
                        return null;
                    if (name == "label")
                        return new DBusVariantItem("s", new DBusStringItem(item.Header ?? "<null>"));
                    if (name == "enabled")
                    {
                        if (item.Menu is not null && item.Menu.Items.Count == 0)
                            return new DBusVariantItem("b", new DBusBoolItem(false));
                        if (!item.IsEnabled)
                            return new DBusVariantItem("b", new DBusBoolItem(false));
                        return null;
                    }

                    if (name == "visible") {
                        if (!item.IsVisible)
                            return new DBusVariantItem("b", new DBusBoolItem(false));
                        return new DBusVariantItem("b", new DBusBoolItem(true));
                    }

                    if (name == "shortcut")
                    {
                        if (item.Gesture is null)
                            return null;
                        if (item.Gesture.KeyModifiers == 0)
                            return null;
                        var lst = new List<DBusItem>();
                        var mod = item.Gesture;
                        if (mod.KeyModifiers.HasAllFlags(KeyModifiers.Control))
                            lst.Add(new DBusStringItem("Control"));
                        if (mod.KeyModifiers.HasAllFlags(KeyModifiers.Alt))
                            lst.Add(new DBusStringItem("Alt"));
                        if (mod.KeyModifiers.HasAllFlags(KeyModifiers.Shift))
                            lst.Add(new DBusStringItem("Shift"));
                        if (mod.KeyModifiers.HasAllFlags(KeyModifiers.Meta))
                            lst.Add(new DBusStringItem("Super"));
                        lst.Add(new DBusStringItem(item.Gesture.Key.ToString()));
                        return new DBusVariantItem("aas", new DBusArrayItem(DBusType.Array, new[] { new DBusArrayItem(DBusType.String, lst) }));
                    }

                    if (name == "toggle-type")
                    {
                        if (item.ToggleType == NativeMenuItemToggleType.CheckBox)
                            return new DBusVariantItem("s", new DBusStringItem("checkmark"));
                        if (item.ToggleType == NativeMenuItemToggleType.Radio)
                            return new DBusVariantItem("s", new DBusStringItem("radio"));
                    }

                    if (name == "toggle-state" && item.ToggleType != NativeMenuItemToggleType.None)
                        return new DBusVariantItem("i", new DBusInt32Item(item.IsChecked ? 1 : 0));

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
                                return new DBusVariantItem("ay", new DBusByteArrayItem(ms.ToArray()));
                            }
                        }
                    }

                    if (name == "children-display")
                        return menu is not null ? new DBusVariantItem("s", new DBusStringItem("submenu")) : null;
                }

                return null;
            }

            private static Dictionary<string, DBusVariantItem> GetProperties((NativeMenuItemBase? item, NativeMenu? menu) i, string[] names)
            {
                if (names.Length == 0)
                    names = s_allProperties;
                var properties = new Dictionary<string, DBusVariantItem>();
                foreach (var n in names)
                {
                    var v = GetProperty(i, n);
                    if (v is not null)
                        properties.Add(n, v);
                }

                return properties;
            }

            private (int, Dictionary<string, DBusVariantItem>, DBusVariantItem[]) GetLayout(NativeMenuItemBase? item, NativeMenu? menu, int depth, string[] propertyNames)
            {
                var id = item is null ? 0 : GetId(item);
                var props = GetProperties((item, menu), propertyNames);
                var children = depth == 0 || menu is null ? Array.Empty<DBusVariantItem>() : new DBusVariantItem[menu.Items.Count];
                if (menu is not null)
                {
                    for (var c = 0; c < children.Length; c++)
                    {
                        var ch = menu.Items[c];
                        var layout = GetLayout(ch, (ch as NativeMenuItem)?.Menu, depth == -1 ? -1 : depth - 1, propertyNames);
                        children[c] = new DBusVariantItem("(ia{sv}av)", new DBusStructItem(new DBusItem[]
                        {
                            new DBusInt32Item(layout.Item1),
                            new DBusArrayItem(DBusType.DictEntry, layout.Item2.Select(static x => new DBusDictEntryItem(new DBusStringItem(x.Key), x.Value)).ToArray()),
                            new DBusArrayItem(DBusType.Variant, layout.Item3)
                        }));
                    }
                }

                return (id, props, children);
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
