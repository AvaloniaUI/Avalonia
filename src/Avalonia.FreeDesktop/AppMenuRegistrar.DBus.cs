using System;
using System.Threading.Tasks;

using Tmds.DBus.Protocol;


namespace Avalonia.FreeDesktop
{
    internal class Registrar : AppMenuRegistrarObject
    {
        private const string Interface = "com.canonical.AppMenu.Registrar";

        public Registrar(RegistrarService service, ObjectPath path) : base(service, path) { }

        public Task RegisterWindowAsync(uint windowId, ObjectPath menuObjectPath)
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "uo",
                    member: "RegisterWindow",
                    flags: MessageFlags.NoReplyExpected);
                writer.WriteUInt32(windowId);
                writer.WriteObjectPath(menuObjectPath);
                return writer.CreateMessage();
            }
        }

        public Task UnregisterWindowAsync(uint windowId)
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "u",
                    member: "UnregisterWindow");
                writer.WriteUInt32(windowId);
                return writer.CreateMessage();
            }
        }

        public Task<(string Service, ObjectPath MenuObjectPath)> GetMenuForWindowAsync(uint windowId)
        {
            return Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_so(m, (AppMenuRegistrarObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "u",
                    member: "GetMenuForWindow");
                writer.WriteUInt32(windowId);
                return writer.CreateMessage();
            }
        }
    }

    internal class RegistrarService
    {
        public RegistrarService(Connection connection, string destination)
            => (Connection, Destination) = (connection, destination);

        public Connection Connection { get; }
        public string Destination { get; }
        public Registrar CreateRegistrar(string path) => new Registrar(this, path);
    }

    internal class AppMenuRegistrarObject
    {
        protected AppMenuRegistrarObject(RegistrarService service, ObjectPath path)
            => (Service, Path) = (service, path);

        public RegistrarService Service { get; }
        public ObjectPath Path { get; }
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

        protected ValueTask<IDisposable> WatchPropertiesChangedAsync<TProperties>(string @interface,
            MessageValueReader<PropertyChanges<TProperties>> reader, Action<Exception?, PropertyChanges<TProperties>> handler,
            bool emitOnCapturedContext)
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
                (Exception? ex, PropertyChanges<TProperties> changes, object? rs, object? hs) =>
                    ((Action<Exception?, PropertyChanges<TProperties>>)hs!).Invoke(ex, changes),
                this, handler, emitOnCapturedContext);
        }

        public ValueTask<IDisposable> WatchSignalAsync<TArg>(string sender, string @interface, ObjectPath path, string signal,
            MessageValueReader<TArg> reader, Action<Exception?, TArg> handler, bool emitOnCapturedContext)
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
                (Exception? ex, TArg arg, object? rs, object? hs) => ((Action<Exception?, TArg>)hs!).Invoke(ex, arg),
                this, handler, emitOnCapturedContext);
        }

        public ValueTask<IDisposable> WatchSignalAsync(string sender, string @interface, ObjectPath path, string signal, Action<Exception?> handler,
            bool emitOnCapturedContext)
        {
            var rule = new MatchRule
            {
                Type = MessageType.Signal,
                Sender = sender,
                Path = path,
                Member = signal,
                Interface = @interface
            };
            return Connection.AddMatchAsync<object>(rule, (Message message, object? state) => null!,
                (Exception? ex, object v, object? rs, object? hs) => ((Action<Exception?>)hs!).Invoke(ex), this, handler, emitOnCapturedContext);
        }

        protected static (string, ObjectPath) ReadMessage_so(Message message, AppMenuRegistrarObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadString();
            var arg1 = reader.ReadObjectPath();
            return (arg0, arg1);
        }
    }
}
