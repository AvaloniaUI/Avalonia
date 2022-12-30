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

#pragma warning disable 1998

namespace Avalonia.FreeDesktop
{
    public class DBusMenuExporter
    {
        public static ITopLevelNativeMenuExporter? TryCreateTopLevelNativeMenu(IntPtr xid) =>
            DBusHelper.Connection is null ? null : new DBusMenuExporterImpl(DBusHelper.Connection, xid);

        public static INativeMenuExporter TryCreateDetachedNativeMenu(string path, Connection currentConnection) =>
            new DBusMenuExporterImpl(currentConnection, path);

        public static string GenerateDBusMenuObjPath => $"/net/avaloniaui/dbusmenu/{Guid.NewGuid():N}";

        private class DBusMenuExporterImpl : ITopLevelNativeMenuExporter, IMethodHandler, IDisposable
        {
            private readonly Connection _connection;
            private readonly DBusMenuProperties _dBusMenuProperties = new() { Version = 2, Status = "normal" };
            private readonly Dictionary<int, NativeMenuItemBase> _idsToItems = new();
            private readonly Dictionary<NativeMenuItemBase, int> _itemsToIds = new();
            private readonly HashSet<NativeMenu> _menus = new();
            private readonly uint _xid;
            private readonly bool _appMenu = true;
            private Registrar? _registrar;
            private NativeMenu? _menu;
            private bool _disposed;
            private uint _revision = 1;
            private bool _resetQueued;
            private int _nextId = 1;

            public DBusMenuExporterImpl(Connection connection, IntPtr xid)
            {
                _connection = connection;
                _xid = (uint)xid.ToInt32();
                Path = GenerateDBusMenuObjPath;
                SetNativeMenu(new NativeMenu());
                Init();
            }

            public DBusMenuExporterImpl(Connection connection, string path)
            {
                _connection = connection;
                _appMenu = false;
                Path = path;
                SetNativeMenu(new NativeMenu());
                Init();
            }

            public string Path { get; }

            private async void Init()
            {
                _connection.AddMethodHandler(this);
                if (!_appMenu)
                    return;
                var services = await _connection.ListServicesAsync();
                if (!services.Contains("com.canonical.AppMenu.Registrar"))
                    return;
                _registrar = new RegistrarService(_connection, "com.canonical.AppMenu.Registrar")
                    .CreateRegistrar("/com/canonical/AppMenu/Registrar");
                if (!_disposed)
                    await _registrar.RegisterWindowAsync(_xid, Path);
                // It's not really important if this code succeeds,
                // and it's not important to know if it succeeds
                // since even if we register the window it's not guaranteed that
                // menu will be actually exported
            }

            public void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;

                // Fire and forget
                _registrar?.UnregisterWindowAsync(_xid);
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
                EmitUIntIntSignal("LayoutUpdated", _revision, 0);
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

            private void OnMenuItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
            {
                QueueReset();
            }

            private void OnItemPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
            {
                QueueReset();
            }

            private static readonly string[] AllProperties = {
                "type", "label", "enabled", "visible", "shortcut", "toggle-type", "children-display", "toggle-state", "icon-data"
            };

            private object? GetProperty((NativeMenuItemBase? item, NativeMenu? menu) i, string name)
            {
                var (it, menu) = i;

                if (it is NativeMenuItemSeparator)
                {
                    if (name == "type")
                        return "separator";
                }
                else if (it is NativeMenuItem item)
                {
                    if (name == "type")
                        return null;
                    if (name == "label")
                        return item.Header ?? "<null>";
                    if (name == "enabled")
                    {
                        if (item.Menu is not null && item.Menu.Items.Count == 0)
                            return false;
                        if (!item.IsEnabled)
                            return false;
                        return null;
                    }
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
                        return new[] { lst.ToArray() };
                    }

                    if (name == "toggle-type")
                    {
                        if (item.ToggleType == NativeMenuItemToggleType.CheckBox)
                            return "checkmark";
                        if (item.ToggleType == NativeMenuItemToggleType.Radio)
                            return "radio";
                    }

                    if (name == "toggle-state" && item.ToggleType != NativeMenuItemToggleType.None)
                        return item.IsChecked ? 1 : 0;

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
                                return ms.ToArray();
                            }
                        }
                    }

                    if (name == "children-display")
                        return menu is not null ? "submenu" : null;
                }

                return null;
            }

            private Dictionary<string, object> GetProperties((NativeMenuItemBase? item, NativeMenu? menu) i, string[] names)
            {
                if (names.Length == 0)
                    names = AllProperties;
                var properties = new Dictionary<string, object>();
                foreach (var n in names)
                {
                    var v = GetProperty(i, n);
                    if (v is not null)
                        properties.Add(n, v);
                }

                return properties;
            }

            private (int, Dictionary<string, object>, object[]) GetLayout(NativeMenuItemBase? item, NativeMenu? menu, int depth, string[] propertyNames)
            {
                var id = item is null ? 0 : GetId(item);
                var props = GetProperties((item, menu), propertyNames);
                var children = depth == 0 || menu is null ? Array.Empty<object>() : new object[menu.Items.Count];
                if (menu is not null)
                {
                    for (var c = 0; c < children.Length; c++)
                    {
                        var ch = menu.Items[c];
                        children[c] = GetLayout(ch, (ch as NativeMenuItem)?.Menu, depth == -1 ? -1 : depth - 1, propertyNames);
                    }
                }

                return (id, props, children);
            }


            private void HandleEvent(int id, string eventId, object data, uint timestamp)
            {
                if (eventId == "clicked")
                {
                    var item = GetMenu(id).item;
                    if (item is NativeMenuItem { IsEnabled: true } and INativeMenuItemExporterEventsImplBridge bridge)
                        bridge.RaiseClicked();
                }
            }

            public ValueTask HandleMethodAsync(MethodContext context)
            {
                switch (context.Request.InterfaceAsString)
                {
                    case "com.canonical.dbusmenu":
                        switch (context.Request.MemberAsString, context.Request.SignatureAsString)
                        {
                            case ("GetLayout", "iias"):
                            {
                                using var writer = context.CreateReplyWriter("u(ia{sv}av)");
                                var reader = context.Request.GetBodyReader();
                                var parentId = reader.ReadInt32();
                                var recursionDepth = reader.ReadInt32();
                                var propertyNames = reader.ReadArray<string>();
                                var menu = GetMenu(parentId);
                                var layout = GetLayout(menu.item, menu.menu, recursionDepth, propertyNames);
                                writer.WriteUInt32(_revision);
                                writer.WriteStruct(layout);
                                if (!IsNativeMenuExported)
                                {
                                    IsNativeMenuExported = true;
                                    Dispatcher.UIThread.Post(() => OnIsNativeMenuExportedChanged?.Invoke(this, EventArgs.Empty));
                                }

                                context.Reply(writer.CreateMessage());
                                break;
                            }
                            case ("GetGroupProperties", "aias"):
                            {
                                using var writer = context.CreateReplyWriter("a(ia{sv})");
                                var reader = context.Request.GetBodyReader();
                                var ids = reader.ReadArray<int>();
                                var propertyNames = reader.ReadArray<string>();
                                var arrayStart = writer.WriteArrayStart(DBusType.Struct);
                                foreach (var id in ids)
                                {
                                    var item = GetMenu(id);
                                    var props = GetProperties(item, propertyNames);
                                    writer.WriteStruct((id, props));
                                }

                                writer.WriteArrayEnd(arrayStart);
                                context.Reply(writer.CreateMessage());
                                break;
                            }
                            case ("GetProperty", "is"):
                            {
                                using var writer = context.CreateReplyWriter("v");
                                var reader = context.Request.GetBodyReader();
                                var id = reader.ReadInt32();
                                var name = reader.ReadString();
                                writer.WriteVariant(GetProperty(GetMenu(id), name) ?? 0);
                                context.Reply(writer.CreateMessage());
                                break;
                            }
                            case ("Event", "isvu"):
                            {
                                var reader = context.Request.GetBodyReader();
                                var id = reader.ReadInt32();
                                var eventId = reader.ReadString();
                                var data = reader.ReadVariant();
                                var timestamp = reader.ReadUInt32();
                                Dispatcher.UIThread.Post(() => HandleEvent(id, eventId, data, timestamp));
                                break;
                            }
                            case ("EventGroup", "a(isvu)"):
                            {
                                using var writer = context.CreateReplyWriter("ai");
                                var reader = context.Request.GetBodyReader();
                                var events = reader.ReadArray<(int Id, string EventId, object Data, uint Timestamp)>();
                                foreach (var e in events)
                                    Dispatcher.UIThread.Post(() => HandleEvent(e.Id, e.EventId, e.Data, e.Timestamp));
                                writer.WriteArray(Array.Empty<int>());
                                context.Reply(writer.CreateMessage());
                                break;
                            }
                            case ("AboutToShow", "i"):
                            {
                                using var writer = context.CreateReplyWriter("b");
                                writer.WriteBool(false);
                                context.Reply(writer.CreateMessage());
                                break;
                            }
                            case ("AboutToShowGroup", "ai"):
                            {
                                using var writer = context.CreateReplyWriter("aiai");
                                writer.WriteStruct((Array.Empty<int>(), Array.Empty<int>()));
                                context.Reply(writer.CreateMessage());
                                break;
                            }
                        }

                        break;
                    case "org.freedesktop.DBus.Properties":
                        switch (context.Request.MemberAsString, context.Request.SignatureAsString)
                        {
                            case ("Version", "u"):
                            {
                                using var writer = context.CreateReplyWriter("u");
                                writer.WriteUInt32(_dBusMenuProperties.Version);
                                context.Reply(writer.CreateMessage());
                                break;
                            }
                            case ("TextDirection", "s"):
                            {
                                using var writer = context.CreateReplyWriter("s");
                                writer.WriteString(_dBusMenuProperties.TextDirection);
                                context.Reply(writer.CreateMessage());
                                break;
                            }
                            case ("Status", "s"):
                            {
                                using var writer = context.CreateReplyWriter("s");
                                writer.WriteString(_dBusMenuProperties.Status);
                                context.Reply(writer.CreateMessage());
                                break;
                            }
                            case ("IconThemePath", "as"):
                            {
                                using var writer = context.CreateReplyWriter("as");
                                writer.WriteArray(_dBusMenuProperties.IconThemePath);
                                context.Reply(writer.CreateMessage());
                                break;
                            }
                        }

                        break;
                }

                return default;
            }

            public bool RunMethodHandlerSynchronously(Message message) => true;

            private void EmitUIntIntSignal(string member, uint arg0, int arg1)
            {
                using var writer = _connection.GetMessageWriter();
                writer.WriteSignalHeader(null, Path, "com.canonical.dbusmenu", member, "ui");
                writer.WriteUInt32(arg0);
                writer.WriteInt32(arg1);
                _connection.TrySendMessage(writer.CreateMessage());
            }
        }
    }
}
