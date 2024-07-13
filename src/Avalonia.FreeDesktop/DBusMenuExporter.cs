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
            DBusHelper.DefaultConnection is {} conn ?  new DBusMenuExporterImpl(conn, xid) : null;

        public static INativeMenuExporter TryCreateDetachedNativeMenu(string path, Connection currentConnection) =>
            new DBusMenuExporterImpl(currentConnection, path);

        public static string GenerateDBusMenuObjPath => $"/net/avaloniaui/dbusmenu/{Guid.NewGuid():N}";

        private class DBusMenuExporterImpl : ComCanonicalDbusmenu, ITopLevelNativeMenuExporter, IDisposable
        {
            private readonly Dictionary<int, NativeMenuItemBase> _idsToItems = new();
            private readonly Dictionary<NativeMenuItemBase, int> _itemsToIds = new();
            private readonly HashSet<NativeMenu> _menus = new();
            private readonly PathHandler _pathHandler;
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
                Version = 4;
                Connection = connection;
                _xid = (uint)xid.ToInt32();
                _pathHandler = new PathHandler(GenerateDBusMenuObjPath);
                _pathHandler.Add(this);
                SetNativeMenu(new NativeMenu());
                _ = InitializeAsync();
            }

            public DBusMenuExporterImpl(Connection connection, string path)
            {
                Version = 4;
                Connection = connection;
                _appMenu = false;
                _pathHandler = new PathHandler(path);
                _pathHandler.Add(this);
                SetNativeMenu(new NativeMenu());
                _ = InitializeAsync();
            }

            public override Connection Connection { get; }

            protected override ValueTask<(uint Revision, (int, Dictionary<string, Variant>, Variant[]) Layout)> OnGetLayoutAsync(int parentId, int recursionDepth, string[] propertyNames)
            {
                var menu = GetMenu(parentId);
                var layout = GetLayout(menu.item, menu.menu, recursionDepth, propertyNames);
                if (!IsNativeMenuExported)
                {
                    IsNativeMenuExported = true;
                    OnIsNativeMenuExportedChanged?.Invoke(this, EventArgs.Empty);
                }

                return new ValueTask<(uint, (int, Dictionary<string, Variant>, Variant[]))>((_revision, layout));
            }

            protected override ValueTask<(int, Dictionary<string, Variant>)[]> OnGetGroupPropertiesAsync(int[] ids, string[] propertyNames)
                => new(ids.Select(id => (id, GetProperties(GetMenu(id), propertyNames))).ToArray());

            protected override ValueTask<Variant> OnGetPropertyAsync(int id, string name) =>
                new(GetProperty(GetMenu(id), name) ?? new Variant(0));

            protected override ValueTask OnEventAsync(int id, string eventId, VariantValue data, uint timestamp)
            {
                HandleEvent(id, eventId);
                return new ValueTask();
            }

            protected override ValueTask<int[]> OnEventGroupAsync((int, string, VariantValue, uint)[] events)
            {
                foreach (var e in events)
                    HandleEvent(e.Item1, e.Item2);
                return new ValueTask<int[]>(Array.Empty<int>());
            }

            protected override ValueTask<bool> OnAboutToShowAsync(int id) => new(false);

            protected override ValueTask<(int[] UpdatesNeeded, int[] IdErrors)> OnAboutToShowGroupAsync(int[] ids) =>
                new((Array.Empty<int>(), Array.Empty<int>()));

            private async Task InitializeAsync()
            {
                Connection.AddMethodHandler(_pathHandler);
                if (!_appMenu)
                    return;

                _registrar = new ComCanonicalAppMenuRegistrar(Connection, "com.canonical.AppMenu.Registrar", "/com/canonical/AppMenu/Registrar");
                try
                {
                    if (!_disposed)
                        await _registrar.RegisterWindowAsync(_xid, _pathHandler.Path);
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
                _pathHandler.Remove(this);
                Connection.RemoveMethodHandler(_pathHandler.Path);
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

            private static Variant? GetProperty((NativeMenuItemBase? item, NativeMenu? menu) i, string name)
            {
                var (it, menu) = i;

                if (it is NativeMenuItemSeparator)
                {
                    if (name == "type")
                        return new Variant("separator");
                }
                else if (it is NativeMenuItem item)
                {
                    if (name == "type")
                        return null;
                    if (name == "label")
                        return new Variant(item.Header ?? "<null>");
                    if (name == "enabled")
                    {
                        if (item.Menu is not null && item.Menu.Items.Count == 0)
                            return new Variant(false);
                        if (!item.IsEnabled)
                            return new Variant(false);
                        return null;
                    }

                    if (name == "visible")
                        return new Variant(item.IsVisible);

                    if (name == "shortcut")
                    {
                        if (item.Gesture is null)
                            return null;
                        if (item.Gesture.KeyModifiers == 0)
                            return null;
                        var lst = new Array<Variant>();
                        var mod = item.Gesture;
                        if (mod.KeyModifiers.HasAllFlags(KeyModifiers.Control))
                            lst.Add(new Variant("Control"));
                        if (mod.KeyModifiers.HasAllFlags(KeyModifiers.Alt))
                            lst.Add(new Variant("Alt"));
                        if (mod.KeyModifiers.HasAllFlags(KeyModifiers.Shift))
                            lst.Add(new Variant("Shift"));
                        if (mod.KeyModifiers.HasAllFlags(KeyModifiers.Meta))
                            lst.Add(new Variant("Super"));
                        lst.Add(new Variant(item.Gesture.Key.ToString()));
                        return Variant.FromArray(new Array<Array<Variant>>(new[] { lst }));
                    }

                    if (name == "toggle-type")
                    {
                        if (item.ToggleType == NativeMenuItemToggleType.CheckBox)
                            return new Variant("checkmark");
                        if (item.ToggleType == NativeMenuItemToggleType.Radio)
                            return new Variant("radio");
                    }

                    if (name == "toggle-state" && item.ToggleType != NativeMenuItemToggleType.None)
                        return new Variant(item.IsChecked ? 1 : 0);

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
                                return Variant.FromArray(new Array<byte>(ms.ToArray()));
                            }
                        }
                    }

                    if (name == "children-display")
                    {
                        if (menu is not null)
                            return new Variant("submenu");
                        return null;
                    }
                }

                return null;
            }

            private static Dictionary<string, Variant> GetProperties((NativeMenuItemBase? item, NativeMenu? menu) i, string[] names)
            {
                if (names.Length == 0)
                    names = s_allProperties;
                var properties = new Dictionary<string, Variant>();
                foreach (var n in names)
                {
                    var v = GetProperty(i, n);
                    if (v.HasValue)
                        properties.Add(n, v.Value);
                }

                return properties;
            }

            private (int, Dictionary<string, Variant>, Variant[]) GetLayout(NativeMenuItemBase? item, NativeMenu? menu, int depth, string[] propertyNames)
            {
                var id = item is null ? 0 : GetId(item);
                var props = GetProperties((item, menu), propertyNames);
                var children = depth == 0 || menu is null ? Array.Empty<Variant>() : new Variant[menu.Items.Count];
                if (menu is not null)
                {
                    for (var c = 0; c < children.Length; c++)
                    {
                        var ch = menu.Items[c];
                        var layout = GetLayout(ch, (ch as NativeMenuItem)?.Menu, depth == -1 ? -1 : depth - 1, propertyNames);
                        children[c] = Variant.FromStruct(Struct.Create(layout.Item1, new Dict<string, Variant>(layout.Item2), new Array<Variant>(layout.Item3)));
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
