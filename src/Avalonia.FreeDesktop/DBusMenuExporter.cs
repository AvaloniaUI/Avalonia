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
using Avalonia.Logging;
using Tmds.DBus.Protocol;
using Avalonia.FreeDesktop.DBus;

namespace Avalonia.FreeDesktop
{
    internal class DBusMenuExporter
    {
        public static ITopLevelNativeMenuExporter? TryCreateTopLevelNativeMenu(IntPtr xid) =>
            DBusHelper.DefaultConnection is {} conn ?  new DBusMenuExporterImpl(conn, xid) : null;

        public static INativeMenuExporter TryCreateDetachedNativeMenu(string path, DBusConnection currentConnection) =>
            new DBusMenuExporterImpl(currentConnection, path);

        public static string GenerateDBusMenuObjPath => $"/net/avaloniaui/dbusmenu/{Guid.NewGuid():N}";

        private sealed class DBusMenuExporterImpl : DBusHandler, IdbusmenuHandler, IdbusmenuProperties, ITopLevelNativeMenuExporter, IDisposable
        {
            private readonly Dictionary<int, NativeMenuItemBase> _idsToItems = new();
            private readonly Dictionary<NativeMenuItemBase, int> _itemsToIds = new();
            private readonly HashSet<NativeMenu> _menus = [];
            private readonly uint _xid;
            private readonly bool _appMenu = true;
            private Registrar? _registrar;
            private NativeMenu? _menu;
            private bool _disposed;
            private uint _revision = 1;
            private bool _resetQueued;
            private int _nextId = 1;

            public DBusMenuExporterImpl(DBusConnection connection, IntPtr xid)
                : base(connection, GenerateDBusMenuObjPath, handlesChildPaths: false)
            {
                _xid = (uint)xid.ToInt32();
                SetNativeMenu([]);
                _ = InitializeAsync();
            }

            public DBusMenuExporterImpl(DBusConnection connection, string path)
                : base(connection, path, handlesChildPaths: false)
            {
                _appMenu = false;
                SetNativeMenu([]);
                _ = InitializeAsync();
            }

            uint IdbusmenuProperties.Version => 4;
            string IdbusmenuProperties.TextDirection => "ltr";
            string IdbusmenuProperties.Status => "normal";
            string[] IdbusmenuProperties.IconThemePath => [];

            ValueTask IdbusmenuHandler.HandleGetPropertyAsync(IdbusmenuHandler.GetPropertyContext context)
                => context.Handle(this);

            ValueTask IdbusmenuHandler.HandleGetAllPropertiesAsync(IdbusmenuHandler.GetAllPropertiesContext context)
                => context.Handle(this);

            ValueTask<(uint Revision, (int, Dictionary<string, VariantValue>, VariantValue[]) Layout)> IdbusmenuHandler.GetLayoutAsync(int parentId, int recursionDepth, string[] propertyNames)
            {
                var menu = GetMenu(parentId);
                var layout = GetLayout(menu.item, menu.menu, recursionDepth, propertyNames);
                if (!IsNativeMenuExported)
                {
                    IsNativeMenuExported = true;
                    OnIsNativeMenuExportedChanged?.Invoke(this, EventArgs.Empty);
                }

                return new ValueTask<(uint, (int, Dictionary<string, VariantValue>, VariantValue[]))>((_revision, layout));
            }

            ValueTask<(int, Dictionary<string, VariantValue>)[]> IdbusmenuHandler.GetGroupPropertiesAsync(int[] ids, string[] propertyNames)
                => new(ids.Select(id => (id, GetProperties(GetMenu(id), propertyNames))).ToArray());

            ValueTask<VariantValue> IdbusmenuHandler.GetPropertyAsync(int id, string name) =>
                new(GetProperty(GetMenu(id), name) ?? VariantValue.Int32(0));

            ValueTask IdbusmenuHandler.EventAsync(int id, string eventId, VariantValue data, uint timestamp)
            {
                HandleEvent(id, eventId);
                return new ValueTask();
            }

            ValueTask<int[]> IdbusmenuHandler.EventGroupAsync((int, string, VariantValue, uint)[] events)
            {
                foreach (var e in events)
                    HandleEvent(e.Item1, e.Item2);
                return new ValueTask<int[]>([]);
            }

            ValueTask<bool> IdbusmenuHandler.AboutToShowAsync(int id) => new(false);

            ValueTask<(int[] UpdatesNeeded, int[] IdErrors)> IdbusmenuHandler.AboutToShowGroupAsync(int[] ids) =>
                new(([], []));

            private async Task InitializeAsync()
            {
                Connection.AddMethodHandler(this);
                if (!_appMenu)
                    return;

                _registrar = new Registrar(Connection, "com.canonical.AppMenu.Registrar", "/com/canonical/AppMenu/Registrar");
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
                _ = _registrar?.UnregisterWindowAsync(_xid)?.ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        Logger.TryGet(LogEventLevel.Warning, LogArea.Platform)
                            ?.Log(this, "DBusMenu UnregisterWindowAsync failed: {Exception}", t.Exception);
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);
                Connection.RemoveMethodHandler(Path);
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
                Connection.EmitLayoutUpdated(Path, _revision, 0);
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

            private static VariantValue? GetProperty((NativeMenuItemBase? item, NativeMenu? menu) i, string name)
            {
                var (it, menu) = i;

                if (it is NativeMenuItemSeparator)
                {
                    if (name == "type")
                        return VariantValue.String("separator");
                }
                else if (it is NativeMenuItem item)
                {
                    if (name == "type")
                        return null;
                    if (name == "label")
                        return VariantValue.String(item.Header ?? "<null>");
                    if (name == "enabled")
                    {
                        if (item.Menu is not null && item.Menu.Items.Count == 0)
                            return VariantValue.Bool(false);
                        if (!item.IsEnabled)
                            return VariantValue.Bool(false);
                        return null;
                    }

                    if (name == "visible")
                        return VariantValue.Bool(item.IsVisible);

                    if (name == "shortcut")
                    {
                        if (item.Gesture is null)
                            return null;
                        if (item.Gesture.KeyModifiers == 0)
                            return null;
                        var lst = new Array<string>();
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
                        return new Array<Array<string>> { lst }.AsVariantValue();
                    }

                    if (name == "toggle-type")
                    {
                        if (item.ToggleType == MenuItemToggleType.CheckBox)
                            return VariantValue.String("checkmark");
                        if (item.ToggleType == MenuItemToggleType.Radio)
                            return VariantValue.String("radio");
                    }

                    if (name == "toggle-state" && item.ToggleType != MenuItemToggleType.None)
                        return VariantValue.Int32(item.IsChecked ? 1 : 0);

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
                                return VariantValue.Array(ms.ToArray());
                            }
                        }
                    }

                    if (name == "children-display")
                    {
                        if (menu is not null)
                            return VariantValue.String("submenu");
                        return null;
                    }
                }

                return null;
            }

            private static Dictionary<string, VariantValue> GetProperties((NativeMenuItemBase? item, NativeMenu? menu) i, string[] names)
            {
                if (names.Length == 0)
                    names = s_allProperties;
                var properties = new Dictionary<string, VariantValue>();
                foreach (var n in names)
                {
                    var v = GetProperty(i, n);
                    if (v.HasValue)
                        properties.Add(n, v.Value);
                }

                return properties;
            }

            private (int, Dictionary<string, VariantValue>, VariantValue[]) GetLayout(NativeMenuItemBase? item, NativeMenu? menu, int depth, string[] propertyNames)
            {
                var id = item is null ? 0 : GetId(item);
                var props = GetProperties((item, menu), propertyNames);
                var children = depth == 0 || menu is null ? [] : new VariantValue[menu.Items.Count];
                if (menu is not null)
                {
                    for (var c = 0; c < children.Length; c++)
                    {
                        var ch = menu.Items[c];
                        var layout = GetLayout(ch, (ch as NativeMenuItem)?.Menu, depth == -1 ? -1 : depth - 1, propertyNames);
                        children[c] = VariantValue.Struct(
                            VariantValue.Int32(layout.Item1),
                            new Dict<string, VariantValue>(layout.Item2).AsVariantValue(),
                            VariantValue.ArrayOfVariant(layout.Item3));
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
