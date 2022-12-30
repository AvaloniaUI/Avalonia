using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Avalonia.FreeDesktop
{
    internal record StatusNotifierWatcherProperties
    {
        public string[] RegisteredStatusNotifierItems { get; set; } = default!;
        public bool IsStatusNotifierHostRegistered { get; set; }
        public int ProtocolVersion { get; set; }
    }

    internal class StatusNotifierWatcher : StatusNotifierWatcherObject
    {
        private const string Interface = "org.kde.StatusNotifierWatcher";

        public StatusNotifierWatcher(StatusNotifierWatcherService service, ObjectPath path) : base(service, path) { }

        public Task RegisterStatusNotifierItemAsync(string service)
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "s",
                    member: "RegisterStatusNotifierItem");
                writer.WriteString(service);
                return writer.CreateMessage();
            }
        }

        public Task RegisterStatusNotifierHostAsync(string service)
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "s",
                    member: "RegisterStatusNotifierHost");
                writer.WriteString(service);
                return writer.CreateMessage();
            }
        }

        public ValueTask<IDisposable> WatchStatusNotifierItemRegisteredAsync(Action<Exception?, string> handler, bool emitOnCapturedContext = true) =>
            WatchSignalAsync(Service.Destination, Interface, Path, "StatusNotifierItemRegistered", static (m, s) =>
                ReadMessage_s(m, (StatusNotifierWatcherObject)s!), handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchStatusNotifierItemUnregisteredAsync(Action<Exception?, string> handler, bool emitOnCapturedContext = true)
            => WatchSignalAsync(Service.Destination, Interface, Path, "StatusNotifierItemUnregistered", static (m, s)
                => ReadMessage_s(m, (StatusNotifierWatcherObject)s!), handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchStatusNotifierHostRegisteredAsync(Action<Exception?> handler, bool emitOnCapturedContext = true) =>
            WatchSignalAsync(Service.Destination, Interface, Path, "StatusNotifierHostRegistered", handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchStatusNotifierHostUnregisteredAsync(Action<Exception?> handler, bool emitOnCapturedContext = true) =>
            WatchSignalAsync(Service.Destination, Interface, Path, "StatusNotifierHostUnregistered", handler, emitOnCapturedContext);

        public Task SetRegisteredStatusNotifierItemsAsync(string[] value)
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
                writer.WriteString("RegisteredStatusNotifierItems");
                writer.WriteSignature("as");
                writer.WriteArray(value);
                return writer.CreateMessage();
            }
        }

        public Task SetIsStatusNotifierHostRegisteredAsync(bool value)
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
                writer.WriteString("IsStatusNotifierHostRegistered");
                writer.WriteSignature("b");
                writer.WriteBool(value);
                return writer.CreateMessage();
            }
        }

        public Task SetProtocolVersionAsync(int value)
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
                writer.WriteString("ProtocolVersion");
                writer.WriteSignature("i");
                writer.WriteInt32(value);
                return writer.CreateMessage();
            }
        }

        public Task<string[]> GetRegisteredStatusNotifierItemsAsync() =>
            Connection.CallMethodAsync(CreateGetPropertyMessage(Interface, "RegisteredStatusNotifierItems"), static (m, s)
                => ReadMessage_v_as(m, (StatusNotifierWatcherObject)s!), this);

        public Task<bool> GetIsStatusNotifierHostRegisteredAsync() =>
            Connection.CallMethodAsync(CreateGetPropertyMessage(Interface, "IsStatusNotifierHostRegistered"), static (m, s) =>
                ReadMessage_v_b(m, (StatusNotifierWatcherObject)s!), this);

        public Task<int> GetProtocolVersionAsync() =>
            Connection.CallMethodAsync(CreateGetPropertyMessage(Interface, "ProtocolVersion"), static (m, s)
                => ReadMessage_v_i(m, (StatusNotifierWatcherObject)s!), this);

        public Task<StatusNotifierWatcherProperties> GetPropertiesAsync()
        {
            return Connection.CallMethodAsync(CreateGetAllPropertiesMessage(Interface), static (m, _)
                    => ReadMessage(m), this);

            static StatusNotifierWatcherProperties ReadMessage(Message message)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }

        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<StatusNotifierWatcherProperties>> handler, bool emitOnCapturedContext = true)
        {
            return base.WatchPropertiesChangedAsync(Interface, static (m, _) => ReadMessage(m), handler, emitOnCapturedContext);

            static PropertyChanges<StatusNotifierWatcherProperties> ReadMessage(Message message)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new();
                return new PropertyChanges<StatusNotifierWatcherProperties>(ReadProperties(ref reader, changed), changed.ToArray(),
                    ReadInvalidated(ref reader));
            }

            static string[] ReadInvalidated(ref Reader reader)
            {
                List<string>? invalidated = null;
                var headersEnd = reader.ReadArrayStart(DBusType.String);
                while (reader.HasNext(headersEnd))
                {
                    invalidated ??= new List<string>();
                    var property = reader.ReadString();
                    switch (property)
                    {
                        case "RegisteredStatusNotifierItems":
                            invalidated.Add("RegisteredStatusNotifierItems");
                            break;
                        case "IsStatusNotifierHostRegistered":
                            invalidated.Add("IsStatusNotifierHostRegistered");
                            break;
                        case "ProtocolVersion":
                            invalidated.Add("ProtocolVersion");
                            break;
                    }
                }

                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }

        private static StatusNotifierWatcherProperties ReadProperties(ref Reader reader, ICollection<string>? changedList = null)
        {
            var props = new StatusNotifierWatcherProperties();
            var headersEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(headersEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "RegisteredStatusNotifierItems":
                        reader.ReadSignature("as");
                        props.RegisteredStatusNotifierItems = reader.ReadArray<string>();
                        changedList?.Add("RegisteredStatusNotifierItems");
                        break;
                    case "IsStatusNotifierHostRegistered":
                        reader.ReadSignature("b");
                        props.IsStatusNotifierHostRegistered = reader.ReadBool();
                        changedList?.Add("IsStatusNotifierHostRegistered");
                        break;
                    case "ProtocolVersion":
                        reader.ReadSignature("i");
                        props.ProtocolVersion = reader.ReadInt32();
                        changedList?.Add("ProtocolVersion");
                        break;
                    default:
                        reader.ReadVariant();
                        break;
                }
            }

            return props;
        }
    }

    internal class StatusNotifierWatcherService
    {
        public StatusNotifierWatcherService(Connection connection, string destination) => (Connection, Destination) = (connection, destination);

        public Connection Connection { get; }
        public string Destination { get; }

        public StatusNotifierWatcher CreateStatusNotifierWatcher(string path) => new(this, path);
    }

    internal class StatusNotifierWatcherObject
    {
        protected StatusNotifierWatcherObject(StatusNotifierWatcherService service, ObjectPath path)
        {
            (Service, Path) = (service, path);
        }

        protected StatusNotifierWatcherService Service { get; }
        protected ObjectPath Path { get; }
        protected Connection Connection => Service.Connection;

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
            return Connection.AddMatchAsync(rule, reader, static (ex, changes, _, hs) =>
                    ((Action<Exception?, PropertyChanges<TProperties>>)hs!).Invoke(ex, changes), this, handler, emitOnCapturedContext);
        }

        protected ValueTask<IDisposable> WatchSignalAsync<TArg>(string sender, string @interface, ObjectPath path, string signal, MessageValueReader<TArg> reader, Action<Exception?, TArg> handler, bool emitOnCapturedContext)
        {
            var rule = new MatchRule
            {
                Type = MessageType.Signal,
                Sender = sender,
                Path = path,
                Member = signal,
                Interface = @interface
            };
            return Connection.AddMatchAsync(rule, reader, static (ex, arg, _, hs)
                    => ((Action<Exception?, TArg>)hs!).Invoke(ex, arg), this, handler, emitOnCapturedContext);
        }

        protected ValueTask<IDisposable> WatchSignalAsync(string sender, string @interface, ObjectPath path, string signal, Action<Exception?> handler, bool emitOnCapturedContext)
        {
            var rule = new MatchRule
            {
                Type = MessageType.Signal,
                Sender = sender,
                Path = path,
                Member = signal,
                Interface = @interface
            };
            return Connection.AddMatchAsync(rule, static (_, _)
                => null!, static (Exception? ex, object _, object? _, object? hs)
                => ((Action<Exception?>)hs!).Invoke(ex), this, handler, emitOnCapturedContext);
        }

        protected static string ReadMessage_s(Message message, StatusNotifierWatcherObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadString();
        }

        protected static string[] ReadMessage_v_as(Message message, StatusNotifierWatcherObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("as");
            return reader.ReadArray<string>();
        }

        protected static bool ReadMessage_v_b(Message message, StatusNotifierWatcherObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("b");
            return reader.ReadBool();
        }

        protected static int ReadMessage_v_i(Message message, StatusNotifierWatcherObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("i");
            return reader.ReadInt32();
        }
    }
}
