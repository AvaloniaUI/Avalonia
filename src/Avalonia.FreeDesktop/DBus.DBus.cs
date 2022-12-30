using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Avalonia.FreeDesktop
{
    internal record DBusProperties
    {
        public string[] Features { get; set; } = default!;
        public string[] Interfaces { get; set; } = default!;
    }

    internal class DBus : DBusObject
    {
        private const string Interface = "org.freedesktop.DBus";

        public DBus(DBusService service, ObjectPath path) : base(service, path) { }

        public Task<string> HelloAsync()
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_s(m, (DBusObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    "Hello");
                return writer.CreateMessage();
            }
        }

        public Task<uint> RequestNameAsync(string a0, uint a1)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_u(m, (DBusObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "su",
                    member: "RequestName");
                writer.WriteString(a0);
                writer.WriteUInt32(a1);
                return writer.CreateMessage();
            }
        }

        public Task<uint> ReleaseNameAsync(string a0)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_u(m, (DBusObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "s",
                    member: "ReleaseName");
                writer.WriteString(a0);
                return writer.CreateMessage();
            }
        }

        public Task<uint> StartServiceByNameAsync(string a0, uint a1)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_u(m, (DBusObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "su",
                    member: "StartServiceByName");
                writer.WriteString(a0);
                writer.WriteUInt32(a1);
                return writer.CreateMessage();
            }
        }

        public Task UpdateActivationEnvironmentAsync(Dictionary<string, string> a0)
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "a{ss}",
                    member: "UpdateActivationEnvironment");
                writer.WriteDictionary(a0);
                return writer.CreateMessage();
            }
        }

        public Task<bool> NameHasOwnerAsync(string a0)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_b(m, (DBusObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "s",
                    member: "NameHasOwner");
                writer.WriteString(a0);
                return writer.CreateMessage();
            }
        }

        public Task<string[]> ListNamesAsync()
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_as(m, (DBusObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    "ListNames");
                return writer.CreateMessage();
            }
        }

        public Task<string[]> ListActivatableNamesAsync()
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_as(m, (DBusObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    "ListActivatableNames");
                return writer.CreateMessage();
            }
        }

        public Task AddMatchAsync(string a0)
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
                    member: "AddMatch");
                writer.WriteString(a0);
                return writer.CreateMessage();
            }
        }

        public Task RemoveMatchAsync(string a0)
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
                    member: "RemoveMatch");
                writer.WriteString(a0);
                return writer.CreateMessage();
            }
        }

        public Task<string> GetNameOwnerAsync(string a0)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_s(m, (DBusObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "s",
                    member: "GetNameOwner");
                writer.WriteString(a0);
                return writer.CreateMessage();
            }
        }

        public Task<string[]> ListQueuedOwnersAsync(string a0)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_as(m, (DBusObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "s",
                    member: "ListQueuedOwners");
                writer.WriteString(a0);
                return writer.CreateMessage();
            }
        }

        public Task<uint> GetConnectionUnixUserAsync(string a0)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_u(m, (DBusObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "s",
                    member: "GetConnectionUnixUser");
                writer.WriteString(a0);
                return writer.CreateMessage();
            }
        }

        public Task<uint> GetConnectionUnixProcessIDAsync(string a0)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_u(m, (DBusObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "s",
                    member: "GetConnectionUnixProcessID");
                writer.WriteString(a0);
                return writer.CreateMessage();
            }
        }

        public Task<byte[]> GetAdtAuditSessionDataAsync(string a0)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_ay(m, (DBusObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "s",
                    member: "GetAdtAuditSessionData");
                writer.WriteString(a0);
                return writer.CreateMessage();
            }
        }

        public Task<byte[]> GetConnectionSELinuxSecurityContextAsync(string a0)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_ay(m, (DBusObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "s",
                    member: "GetConnectionSELinuxSecurityContext");
                writer.WriteString(a0);
                return writer.CreateMessage();
            }
        }

        public Task ReloadConfigAsync()
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    "ReloadConfig");
                return writer.CreateMessage();
            }
        }

        public Task<string> GetIdAsync()
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_s(m, (DBusObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    "GetId");
                return writer.CreateMessage();
            }
        }

        public Task<Dictionary<string, object>> GetConnectionCredentialsAsync(string a0)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_aesv(m, (DBusObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "s",
                    member: "GetConnectionCredentials");
                writer.WriteString(a0);
                return writer.CreateMessage();
            }
        }

        public ValueTask<IDisposable> WatchNameOwnerChangedAsync(Action<Exception?, (string A0, string A1, string A2)> handler, bool emitOnCapturedContext = true) =>
            WatchSignalAsync(Service.Destination, Interface, Path, "NameOwnerChanged", static (m, s) =>
                ReadMessage_sss(m, (DBusObject)s!), handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchNameLostAsync(Action<Exception?, string> handler, bool emitOnCapturedContext = true) =>
            WatchSignalAsync(Service.Destination, Interface, Path, "NameLost", static (m, s) =>
                ReadMessage_s(m, (DBusObject)s!), handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchNameAcquiredAsync(Action<Exception?, string> handler, bool emitOnCapturedContext = true) =>
            WatchSignalAsync(Service.Destination, Interface, Path, "NameAcquired", static (m, s) =>
                ReadMessage_s(m, (DBusObject)s!), handler, emitOnCapturedContext);

        public Task SetFeaturesAsync(string[] value)
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
                writer.WriteString("Features");
                writer.WriteSignature("as");
                writer.WriteArray(value);
                return writer.CreateMessage();
            }
        }

        public Task SetInterfacesAsync(string[] value)
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
                writer.WriteString("Interfaces");
                writer.WriteSignature("as");
                writer.WriteArray(value);
                return writer.CreateMessage();
            }
        }

        public Task<string[]> GetFeaturesAsync() =>
            Connection.CallMethodAsync(CreateGetPropertyMessage(Interface, "Features"), static (m, s) =>
                ReadMessage_v_as(m, (DBusObject)s!), this);

        public Task<string[]> GetInterfacesAsync() =>
            Connection.CallMethodAsync(CreateGetPropertyMessage(Interface, "Interfaces"), static (m, s) =>
                ReadMessage_v_as(m, (DBusObject)s!), this);

        public Task<DBusProperties> GetPropertiesAsync()
        {
            return Connection.CallMethodAsync(CreateGetAllPropertiesMessage(Interface), static (m, _) =>
                ReadMessage(m), this);

            static DBusProperties ReadMessage(Message message)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }

        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<DBusProperties>> handler, bool emitOnCapturedContext = true)
        {
            return base.WatchPropertiesChangedAsync(Interface, static (m, _) =>
                ReadMessage(m), handler, emitOnCapturedContext);

            static PropertyChanges<DBusProperties> ReadMessage(Message message)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new();
                return new PropertyChanges<DBusProperties>(ReadProperties(ref reader, changed), changed.ToArray(), ReadInvalidated(ref reader));
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
                        case "Features":
                            invalidated.Add("Features");
                            break;
                        case "Interfaces":
                            invalidated.Add("Interfaces");
                            break;
                    }
                }

                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }

        private static DBusProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
        {
            var props = new DBusProperties();
            var headersEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(headersEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "Features":
                        reader.ReadSignature("as");
                        props.Features = reader.ReadArray<string>();
                        changedList?.Add("Features");
                        break;
                    case "Interfaces":
                        reader.ReadSignature("as");
                        props.Interfaces = reader.ReadArray<string>();
                        changedList?.Add("Interfaces");
                        break;
                    default:
                        reader.ReadVariant();
                        break;
                }
            }

            return props;
        }
    }

    internal class DBusService
    {
        public DBusService(Connection connection, string destination)
            => (Connection, Destination) = (connection, destination);

        public Connection Connection { get; }
        public string Destination { get; }
        public DBus CreateDBus(string path) => new(this, path);
    }

    internal class DBusObject
    {
        protected DBusObject(DBusService service, ObjectPath path)
        {
            Service = service;
            Path = path;
        }

        public DBusService Service { get; }
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
            return Connection.AddMatchAsync(rule, reader, static (ex, changes, _, hs) =>
                ((Action<Exception?, PropertyChanges<TProperties>>)hs!).Invoke(ex, changes), this, handler, emitOnCapturedContext);
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
            return Connection.AddMatchAsync(rule, reader, static (ex, arg, _, hs) =>
                    ((Action<Exception?, TArg>)hs!).Invoke(ex, arg), this, handler, emitOnCapturedContext);
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
            return Connection.AddMatchAsync<object>(rule, static (_, _) => null!, static (ex, _, _, hs) =>
                ((Action<Exception?>)hs!).Invoke(ex), this, handler, emitOnCapturedContext);
        }

        protected static string ReadMessage_s(Message message, DBusObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadString();
        }

        protected static uint ReadMessage_u(Message message, DBusObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadUInt32();
        }

        protected static bool ReadMessage_b(Message message, DBusObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadBool();
        }

        protected static string[] ReadMessage_as(Message message, DBusObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadArray<string>();
        }

        protected static byte[] ReadMessage_ay(Message message, DBusObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadArray<byte>();
        }

        protected static Dictionary<string, object> ReadMessage_aesv(Message message, DBusObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadDictionary<string, object>();
        }

        protected static (string, string, string) ReadMessage_sss(Message message, DBusObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadString();
            var arg1 = reader.ReadString();
            var arg2 = reader.ReadString();
            return (arg0, arg1, arg2);
        }

        protected static string[] ReadMessage_v_as(Message message, DBusObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("as");
            return reader.ReadArray<string>();
        }
    }
}
