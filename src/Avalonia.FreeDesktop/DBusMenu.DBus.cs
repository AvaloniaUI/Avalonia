using System;
using Tmds.DBus.Protocol;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avalonia.FreeDesktop
{
    internal record DBusMenuProperties
    {
        public uint Version { get; set; }
        public string TextDirection { get; set; } = default!;
        public string Status { get; set; } = default!;
        public string[] IconThemePath { get; set; } = default!;
    }

    internal class DBusMenu : DBusMenuObject
    {
        private const string Interface = "com.canonical.dbusmenu";
        public DBusMenu(DBusMenuService service, ObjectPath path) : base(service, path)
        { }
        public Task<(uint Revision, (int, Dictionary<string, object>, object[]) Layout)> GetLayoutAsync(int parentId, int recursionDepth, string[] propertyNames)
        {
            return Connection.CallMethodAsync(CreateMessage(), (m, s) => ReadMessage_uriaesvavz(m, (DBusMenuObject)s!), this);
            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "iias",
                    member: "GetLayout");
                writer.WriteInt32(parentId);
                writer.WriteInt32(recursionDepth);
                writer.WriteArray(propertyNames);
                return writer.CreateMessage();
            }
        }
        public Task<(int, Dictionary<string, object>)[]> GetGroupPropertiesAsync(int[] ids, string[] propertyNames)
        {
            return Connection.CallMethodAsync(CreateMessage(), (m, s) => ReadMessage_ariaesvz(m, (DBusMenuObject)s!), this);
            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "aias",
                    member: "GetGroupProperties");
                writer.WriteArray(ids);
                writer.WriteArray(propertyNames);
                return writer.CreateMessage();
            }
        }
        public Task<object> GetPropertyAsync(int id, string name)
        {
            return Connection.CallMethodAsync(CreateMessage(), (m, s) => ReadMessage_v(m, (DBusMenuObject)s!), this);
            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "is",
                    member: "GetProperty");
                writer.WriteInt32(id);
                writer.WriteString(name);
                return writer.CreateMessage();
            }
        }
        public Task EventAsync(int id, string eventId, object data, uint timestamp)
        {
            return Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "isvu",
                    member: "Event");
                writer.WriteInt32(id);
                writer.WriteString(eventId);
                writer.WriteVariant(data);
                writer.WriteUInt32(timestamp);
                return writer.CreateMessage();
            }
        }
        public Task<int[]> EventGroupAsync((int, string, object, uint)[] events)
        {
            return Connection.CallMethodAsync(CreateMessage(), (m, s) => ReadMessage_ai(m, (DBusMenuObject)s!), this);
            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "a(isvu)",
                    member: "EventGroup");
                writer.WriteArray(events);
                return writer.CreateMessage();
            }
        }
        public Task<bool> AboutToShowAsync(int id)
        {
            return Connection.CallMethodAsync(CreateMessage(), (m, s) => ReadMessage_b(m, (DBusMenuObject)s!), this);
            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "i",
                    member: "AboutToShow");
                writer.WriteInt32(id);
                return writer.CreateMessage();
            }
        }
        public Task<(int[] UpdatesNeeded, int[] IdErrors)> AboutToShowGroupAsync(int[] ids)
        {
            return Connection.CallMethodAsync(CreateMessage(), (m, s) => ReadMessage_aiai(m, (DBusMenuObject)s!), this);
            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "ai",
                    member: "AboutToShowGroup");
                writer.WriteArray(ids);
                return writer.CreateMessage();
            }
        }
        public ValueTask<IDisposable> WatchItemsPropertiesUpdatedAsync(Action<Exception?, ((int, Dictionary<string, object>)[] UpdatedProps, (int, string[])[] RemovedProps)> handler, bool emitOnCapturedContext = true)
            => WatchSignalAsync(Service.Destination, Interface, Path, "ItemsPropertiesUpdated", (m, s) => ReadMessage_ariaesvzariasz(m, (DBusMenuObject)s!), handler, emitOnCapturedContext);
        public ValueTask<IDisposable> WatchLayoutUpdatedAsync(Action<Exception?, (uint Revision, int Parent)> handler, bool emitOnCapturedContext = true)
            => WatchSignalAsync(Service.Destination, Interface, Path, "LayoutUpdated", (m, s) => ReadMessage_ui(m, (DBusMenuObject)s!), handler, emitOnCapturedContext);
        public ValueTask<IDisposable> WatchItemActivationRequestedAsync(Action<Exception?, (int Id, uint Timestamp)> handler, bool emitOnCapturedContext = true)
            => WatchSignalAsync(Service.Destination, Interface, Path, "ItemActivationRequested", (m, s) => ReadMessage_iu(m, (DBusMenuObject)s!), handler, emitOnCapturedContext);
        public Task SetVersionAsync(uint value)
        {
            return Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(Interface);
                writer.WriteString("Version");
                writer.WriteSignature("u");
                writer.WriteUInt32(value);
                return writer.CreateMessage();
            }
        }
        public Task SetTextDirectionAsync(string value)
        {
            return Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(Interface);
                writer.WriteString("TextDirection");
                writer.WriteSignature("s");
                writer.WriteString(value);
                return writer.CreateMessage();
            }
        }
        public Task SetStatusAsync(string value)
        {
            return Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(Interface);
                writer.WriteString("Status");
                writer.WriteSignature("s");
                writer.WriteString(value);
                return writer.CreateMessage();
            }
        }
        public Task SetIconThemePathAsync(string[] value)
        {
            return Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(Interface);
                writer.WriteString("IconThemePath");
                writer.WriteSignature("as");
                writer.WriteArray(value);
                return writer.CreateMessage();
            }
        }
        public Task<uint> GetVersionAsync()
            => Connection.CallMethodAsync(CreateGetPropertyMessage(Interface, "Version"), (m, s) => ReadMessage_v_u(m, (DBusMenuObject)s!), this);
        public Task<string> GetTextDirectionAsync()
            => Connection.CallMethodAsync(CreateGetPropertyMessage(Interface, "TextDirection"), (m, s) => ReadMessage_v_s(m, (DBusMenuObject)s!), this);
        public Task<string> GetStatusAsync()
            => Connection.CallMethodAsync(CreateGetPropertyMessage(Interface, "Status"), (m, s) => ReadMessage_v_s(m, (DBusMenuObject)s!), this);
        public Task<string[]> GetIconThemePathAsync()
            => Connection.CallMethodAsync(CreateGetPropertyMessage(Interface, "IconThemePath"), (m, s) => ReadMessage_v_as(m, (DBusMenuObject)s!), this);
        public Task<DBusMenuProperties> GetPropertiesAsync()
        {
            return Connection.CallMethodAsync(CreateGetAllPropertiesMessage(Interface), (m, s) => ReadMessage(m, (DBusMenuObject)s!), this);
            static DBusMenuProperties ReadMessage(Message message, DBusMenuObject _)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }
        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<DBusMenuProperties>> handler, bool emitOnCapturedContext = true)
        {
            return base.WatchPropertiesChangedAsync(Interface, (m, s) => ReadMessage(m, (DBusMenuObject)s!), handler, emitOnCapturedContext);
            static PropertyChanges<DBusMenuProperties> ReadMessage(Message message, DBusMenuObject _)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new(), invalidated = new();
                return new PropertyChanges<DBusMenuProperties>(ReadProperties(ref reader, changed), changed.ToArray(), ReadInvalidated(ref reader));
            }
            static string[] ReadInvalidated(ref Reader reader)
            {
                List<string>? invalidated = null;
                ArrayEnd headersEnd = reader.ReadArrayStart(DBusType.String);
                while (reader.HasNext(headersEnd))
                {
                    invalidated ??= new();
                    var property = reader.ReadString();
                    switch (property)
                    {
                        case "Version": invalidated.Add("Version"); break;
                        case "TextDirection": invalidated.Add("TextDirection"); break;
                        case "Status": invalidated.Add("Status"); break;
                        case "IconThemePath": invalidated.Add("IconThemePath"); break;
                    }
                }
                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }
        private static DBusMenuProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
        {
            var props = new DBusMenuProperties();
            ArrayEnd headersEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(headersEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "Version":
                        reader.ReadSignature("u");
                        props.Version = reader.ReadUInt32();
                        changedList?.Add("Version");
                        break;
                    case "TextDirection":
                        reader.ReadSignature("s");
                        props.TextDirection = reader.ReadString();
                        changedList?.Add("TextDirection");
                        break;
                    case "Status":
                        reader.ReadSignature("s");
                        props.Status = reader.ReadString();
                        changedList?.Add("Status");
                        break;
                    case "IconThemePath":
                        reader.ReadSignature("as");
                        props.IconThemePath = reader.ReadArray<string>();
                        changedList?.Add("IconThemePath");
                        break;
                    default:
                        reader.ReadVariant();
                        break;
                }
            }
            return props;
        }
    }

    internal class DBusMenuService
    {
        public Connection Connection { get; }
        public string Destination { get; }
        public DBusMenuService(Connection connection, string destination)
            => (Connection, Destination) = (connection, destination);
        public DBusMenu CreateDbusmenu(string path) => new DBusMenu(this, path);
    }

    internal class DBusMenuObject
    {
        public DBusMenuService Service { get; }
        public ObjectPath Path { get; }
        protected Connection Connection => Service.Connection;
        protected DBusMenuObject(DBusMenuService service, ObjectPath path)
            => (Service, Path) = (service, path);
        protected MessageBuffer CreateGetPropertyMessage(string @interface, string property)
        {
            using var writer = Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                Service.Destination,
                Path,
                "org.freedesktop.DBus.Properties",
                signature: "ss",
                member: "Get");
            writer.WriteString(@interface);
            writer.WriteString(property);
            return writer.CreateMessage();
        }
        protected MessageBuffer CreateGetAllPropertiesMessage(string @interface)
        {
            using var writer = Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                Service.Destination,
                Path,
                "org.freedesktop.DBus.Properties",
                signature: "s",
                member: "GetAll");
            writer.WriteString(@interface);
            return writer.CreateMessage();
        }
        protected ValueTask<IDisposable> WatchPropertiesChangedAsync<TProperties>(string @interface, MessageValueReader<PropertyChanges<TProperties>> reader, Action<Exception?, PropertyChanges<TProperties>> handler, bool emitOnCapturedContext)
        {
            var rule = new MatchRule
            {
                Type = MessageType.Signal,
                Sender = Service.Destination,
                Path = Path,
                Interface = "org.freedesktop.DBus.Properties",
                Member = "PropertiesChanged",
                Arg0 = @interface
            };
            return Connection.AddMatchAsync(rule, reader,
                                                    (ex, changes, rs, hs) => ((Action<Exception?, PropertyChanges<TProperties>>)hs!).Invoke(ex, changes),
                                                    this, handler, emitOnCapturedContext);
        }
        public ValueTask<IDisposable> WatchSignalAsync<TArg>(string sender, string @interface, ObjectPath path, string signal, MessageValueReader<TArg> reader, Action<Exception?, TArg> handler, bool emitOnCapturedContext)
        {
            var rule = new MatchRule
            {
                Type = MessageType.Signal,
                Sender = sender,
                Path = path,
                Member = signal,
                Interface = @interface
            };
            return Connection.AddMatchAsync(rule, reader,
                                                    (ex, arg, rs, hs) => ((Action<Exception?, TArg>)hs!).Invoke(ex, arg),
                                                    this, handler, emitOnCapturedContext);
        }
        public ValueTask<IDisposable> WatchSignalAsync(string sender, string @interface, ObjectPath path, string signal, Action<Exception?> handler, bool emitOnCapturedContext)
        {
            var rule = new MatchRule
            {
                Type = MessageType.Signal,
                Sender = sender,
                Path = path,
                Member = signal,
                Interface = @interface
            };
            return Connection.AddMatchAsync<object>(rule, (message, state) => null!,
                                                            (ex, v, rs, hs) => ((Action<Exception?>)hs!).Invoke(ex), this, handler, emitOnCapturedContext);
        }
        protected static (uint, (int, Dictionary<string, object>, object[])) ReadMessage_uriaesvavz(Message message, DBusMenuObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadUInt32();
            var arg1 = reader.ReadStruct<int, Dictionary<string, object>, object[]>();
            return (arg0, arg1);
        }
        protected static (int, Dictionary<string, object>)[] ReadMessage_ariaesvz(Message message, DBusMenuObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadArray<(int, Dictionary<string, object>)>();
        }
        protected static object ReadMessage_v(Message message, DBusMenuObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadVariant();
        }
        protected static int[] ReadMessage_ai(Message message, DBusMenuObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadArray<int>();
        }
        protected static bool ReadMessage_b(Message message, DBusMenuObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadBool();
        }
        protected static (int[], int[]) ReadMessage_aiai(Message message, DBusMenuObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadArray<int>();
            var arg1 = reader.ReadArray<int>();
            return (arg0, arg1);
        }
        protected static ((int, Dictionary<string, object>)[], (int, string[])[]) ReadMessage_ariaesvzariasz(Message message, DBusMenuObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadArray<(int, Dictionary<string, object>)>();
            var arg1 = reader.ReadArray<(int, string[])>();
            return (arg0, arg1);
        }
        protected static (uint, int) ReadMessage_ui(Message message, DBusMenuObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadUInt32();
            var arg1 = reader.ReadInt32();
            return (arg0, arg1);
        }
        protected static (int, uint) ReadMessage_iu(Message message, DBusMenuObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadInt32();
            var arg1 = reader.ReadUInt32();
            return (arg0, arg1);
        }
        protected static uint ReadMessage_v_u(Message message, DBusMenuObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("u");
            return reader.ReadUInt32();
        }
        protected static string ReadMessage_v_s(Message message, DBusMenuObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("s");
            return reader.ReadString();
        }
        protected static string[] ReadMessage_v_as(Message message, DBusMenuObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("as");
            return reader.ReadArray<string>();
        }
    }
}
