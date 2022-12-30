using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Avalonia.FreeDesktop
{
    internal record NotificationProperties
    {
        public uint Version { get; set; }
    }

    internal class Notification : DesktopObject
    {
        private const string Interface = "org.freedesktop.portal.Notification";

        public Notification(DesktopService service, ObjectPath path) : base(service, path) { }

        public Task AddNotificationAsync(string id, Dictionary<string, object> notification)
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "sa{sv}",
                    member: "AddNotification");
                writer.WriteString(id);
                writer.WriteDictionary(notification);
                return writer.CreateMessage();
            }
        }

        public Task RemoveNotificationAsync(string id)
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
                    member: "RemoveNotification");
                writer.WriteString(id);
                return writer.CreateMessage();
            }
        }

        public ValueTask<IDisposable> WatchActionInvokedAsync(Action<Exception?, (string Id, string Action, object[] Parameter)> handler,
            bool emitOnCapturedContext = true)
            => WatchSignalAsync(Service.Destination, Interface, Path, "ActionInvoked", static (m, s)
                => ReadMessage_ssav(m, (DesktopObject)s!), handler, emitOnCapturedContext);

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
                writer.WriteString("version");
                writer.WriteSignature("u");
                writer.WriteUInt32(value);
                return writer.CreateMessage();
            }
        }

        public Task<uint> GetVersionAsync()
            => Connection.CallMethodAsync(CreateGetPropertyMessage(Interface, "version"), static (m, s)
                => ReadMessage_v_u(m, (DesktopObject)s!), this);

        public Task<NotificationProperties> GetPropertiesAsync()
        {
            return Connection.CallMethodAsync(CreateGetAllPropertiesMessage(Interface), static (m, _)
                => ReadMessage(m), this);

            static NotificationProperties ReadMessage(Message message)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }

        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<NotificationProperties>> handler,
            bool emitOnCapturedContext = true)
        {
            return base.WatchPropertiesChangedAsync(Interface, static (m, _)
                    => ReadMessage(m), handler, emitOnCapturedContext);

            static PropertyChanges<NotificationProperties> ReadMessage(Message message)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new();
                return new PropertyChanges<NotificationProperties>(ReadProperties(ref reader, changed), changed.ToArray(),
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
                        case "version":
                            invalidated.Add("Version");
                            break;
                    }
                }

                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }

        private static NotificationProperties ReadProperties(ref Reader reader, ICollection<string>? changedList = null)
        {
            var props = new NotificationProperties();
            var headersEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(headersEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "version":
                        reader.ReadSignature("u");
                        props.Version = reader.ReadUInt32();
                        changedList?.Add("Version");
                        break;
                    default:
                        reader.ReadVariant();
                        break;
                }
            }

            return props;
        }
    }

    internal record OpenURIProperties
    {
        public uint Version { get; set; }
    }

    internal class OpenURI : DesktopObject
    {
        private const string Interface = "org.freedesktop.portal.OpenURI";

        public OpenURI(DesktopService service, ObjectPath path) : base(service, path) { }

        public Task<ObjectPath> OpenUriAsync(string parentWindow, string uri, Dictionary<string, object> options)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_o(m, (DesktopObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "ssa{sv}",
                    member: "OpenURI");
                writer.WriteString(parentWindow);
                writer.WriteString(uri);
                writer.WriteDictionary(options);
                return writer.CreateMessage();
            }
        }

        public Task<ObjectPath> OpenFileAsync(string parentWindow, SafeHandle fd, Dictionary<string, object> options)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_o(m, (DesktopObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "sha{sv}",
                    member: "OpenFile");
                writer.WriteString(parentWindow);
                writer.WriteHandle(fd);
                writer.WriteDictionary(options);
                return writer.CreateMessage();
            }
        }

        public Task<ObjectPath> OpenDirectoryAsync(string parentWindow, SafeHandle fd, Dictionary<string, object> options)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_o(m, (DesktopObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "sha{sv}",
                    member: "OpenDirectory");
                writer.WriteString(parentWindow);
                writer.WriteHandle(fd);
                writer.WriteDictionary(options);
                return writer.CreateMessage();
            }
        }

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
                writer.WriteString("version");
                writer.WriteSignature("u");
                writer.WriteUInt32(value);
                return writer.CreateMessage();
            }
        }

        public Task<uint> GetVersionAsync()
            => Connection.CallMethodAsync(CreateGetPropertyMessage(Interface, "version"), static (m, s)
                => ReadMessage_v_u(m, (DesktopObject)s!), this);

        public Task<OpenURIProperties> GetPropertiesAsync()
        {
            return Connection.CallMethodAsync(CreateGetAllPropertiesMessage(Interface), static (m, _)
                => ReadMessage(m), this);

            static OpenURIProperties ReadMessage(Message message)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }

        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<OpenURIProperties>> handler,
            bool emitOnCapturedContext = true)
        {
            return base.WatchPropertiesChangedAsync(Interface, static (m, _)
                    => ReadMessage(m), handler, emitOnCapturedContext);

            static PropertyChanges<OpenURIProperties> ReadMessage(Message message)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new();
                return new PropertyChanges<OpenURIProperties>(ReadProperties(ref reader, changed), changed.ToArray(), ReadInvalidated(ref reader));
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
                        case "version":
                            invalidated.Add("Version");
                            break;
                    }
                }

                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }

        private static OpenURIProperties ReadProperties(ref Reader reader, ICollection<string>? changedList = null)
        {
            var props = new OpenURIProperties();
            var headersEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(headersEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "version":
                        reader.ReadSignature("u");
                        props.Version = reader.ReadUInt32();
                        changedList?.Add("Version");
                        break;
                    default:
                        reader.ReadVariant();
                        break;
                }
            }

            return props;
        }
    }

    internal record DynamicLauncherProperties
    {
        public uint SupportedLauncherTypes { get; set; }
        public uint Version { get; set; }
    }

    internal class DynamicLauncher : DesktopObject
    {
        private const string Interface = "org.freedesktop.portal.DynamicLauncher";

        public DynamicLauncher(DesktopService service, ObjectPath path) : base(service, path) { }

        public Task InstallAsync(string token, string desktopFileId, string desktopEntry, Dictionary<string, object> options)
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "sssa{sv}",
                    member: "Install");
                writer.WriteString(token);
                writer.WriteString(desktopFileId);
                writer.WriteString(desktopEntry);
                writer.WriteDictionary(options);
                return writer.CreateMessage();
            }
        }

        public Task<ObjectPath> PrepareInstallAsync(string parentWindow, string name, object iconV, Dictionary<string, object> options)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_o(m, (DesktopObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "ssva{sv}",
                    member: "PrepareInstall");
                writer.WriteString(parentWindow);
                writer.WriteString(name);
                writer.WriteVariant(iconV);
                writer.WriteDictionary(options);
                return writer.CreateMessage();
            }
        }

        public Task<string> RequestInstallTokenAsync(string name, object iconV, Dictionary<string, object> options)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s)
                => ReadMessage_s(m, (DesktopObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "sva{sv}",
                    member: "RequestInstallToken");
                writer.WriteString(name);
                writer.WriteVariant(iconV);
                writer.WriteDictionary(options);
                return writer.CreateMessage();
            }
        }

        public Task UninstallAsync(string desktopFileId, Dictionary<string, object> options)
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "sa{sv}",
                    member: "Uninstall");
                writer.WriteString(desktopFileId);
                writer.WriteDictionary(options);
                return writer.CreateMessage();
            }
        }

        public Task<string> GetDesktopEntryAsync(string desktopFileId)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s)
                => ReadMessage_s(m, (DesktopObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "s",
                    member: "GetDesktopEntry");
                writer.WriteString(desktopFileId);
                return writer.CreateMessage();
            }
        }

        public Task<(object IconV, string IconFormat, uint IconSize)> GetIconAsync(string desktopFileId)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s)
                => ReadMessage_vsu(m, (DesktopObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "s",
                    member: "GetIcon");
                writer.WriteString(desktopFileId);
                return writer.CreateMessage();
            }
        }

        public Task LaunchAsync(string desktopFileId, Dictionary<string, object> options)
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "sa{sv}",
                    member: "Launch");
                writer.WriteString(desktopFileId);
                writer.WriteDictionary(options);
                return writer.CreateMessage();
            }
        }

        public Task SetSupportedLauncherTypesAsync(uint value)
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
                writer.WriteString("SupportedLauncherTypes");
                writer.WriteSignature("u");
                writer.WriteUInt32(value);
                return writer.CreateMessage();
            }
        }

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
                writer.WriteString("version");
                writer.WriteSignature("u");
                writer.WriteUInt32(value);
                return writer.CreateMessage();
            }
        }

        public Task<uint> GetSupportedLauncherTypesAsync()
            => Connection.CallMethodAsync(CreateGetPropertyMessage(Interface, "SupportedLauncherTypes"), static (m, s)
                => ReadMessage_v_u(m, (DesktopObject)s!), this);

        public Task<uint> GetVersionAsync()
            => Connection.CallMethodAsync(CreateGetPropertyMessage(Interface, "version"), static (m, s)
                => ReadMessage_v_u(m, (DesktopObject)s!), this);

        public Task<DynamicLauncherProperties> GetPropertiesAsync()
        {
            return Connection.CallMethodAsync(CreateGetAllPropertiesMessage(Interface), static (m, _)
                => ReadMessage(m), this);

            static DynamicLauncherProperties ReadMessage(Message message)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }

        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<DynamicLauncherProperties>> handler,
            bool emitOnCapturedContext = true)
        {
            return base.WatchPropertiesChangedAsync(Interface, static (m, _)
                    => ReadMessage(m), handler,
                emitOnCapturedContext);

            static PropertyChanges<DynamicLauncherProperties> ReadMessage(Message message)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new();
                return new PropertyChanges<DynamicLauncherProperties>(ReadProperties(ref reader, changed), changed.ToArray(),
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
                        case "SupportedLauncherTypes":
                            invalidated.Add("SupportedLauncherTypes");
                            break;
                        case "version":
                            invalidated.Add("Version");
                            break;
                    }
                }

                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }

        private static DynamicLauncherProperties ReadProperties(ref Reader reader, ICollection<string>? changedList = null)
        {
            var props = new DynamicLauncherProperties();
            var headersEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(headersEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "SupportedLauncherTypes":
                        reader.ReadSignature("u");
                        props.SupportedLauncherTypes = reader.ReadUInt32();
                        changedList?.Add("SupportedLauncherTypes");
                        break;
                    case "version":
                        reader.ReadSignature("u");
                        props.Version = reader.ReadUInt32();
                        changedList?.Add("Version");
                        break;
                    default:
                        reader.ReadVariant();
                        break;
                }
            }

            return props;
        }
    }

    internal record FileChooserProperties
    {
        public uint Version { get; set; }
    }

    internal class FileChooser : DesktopObject
    {
        private const string Interface = "org.freedesktop.portal.FileChooser";

        public FileChooser(DesktopService service, ObjectPath path) : base(service, path) { }

        public Task<ObjectPath> OpenFileAsync(string parentWindow, string title, Dictionary<string, object> options)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_o(m, (DesktopObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "ssa{sv}",
                    member: "OpenFile");
                writer.WriteString(parentWindow);
                writer.WriteString(title);
                writer.WriteDictionary(options);
                return writer.CreateMessage();
            }
        }

        public Task<ObjectPath> SaveFileAsync(string parentWindow, string title, Dictionary<string, object> options)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_o(m, (DesktopObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "ssa{sv}",
                    member: "SaveFile");
                writer.WriteString(parentWindow);
                writer.WriteString(title);
                writer.WriteDictionary(options);
                return writer.CreateMessage();
            }
        }

        public Task<ObjectPath> SaveFilesAsync(string parentWindow, string title, Dictionary<string, object> options)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_o(m, (DesktopObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "ssa{sv}",
                    member: "SaveFiles");
                writer.WriteString(parentWindow);
                writer.WriteString(title);
                writer.WriteDictionary(options);
                return writer.CreateMessage();
            }
        }

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
                writer.WriteString("version");
                writer.WriteSignature("u");
                writer.WriteUInt32(value);
                return writer.CreateMessage();
            }
        }

        public Task<uint> GetVersionAsync()
            => Connection.CallMethodAsync(CreateGetPropertyMessage(Interface, "version"), static (m, s)
                => ReadMessage_v_u(m, (DesktopObject)s!), this);

        public Task<FileChooserProperties> GetPropertiesAsync()
        {
            return Connection.CallMethodAsync(CreateGetAllPropertiesMessage(Interface), static (m, _)
                => ReadMessage(m), this);

            static FileChooserProperties ReadMessage(Message message)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }

        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<FileChooserProperties>> handler,
            bool emitOnCapturedContext = true)
        {
            return base.WatchPropertiesChangedAsync(Interface, static (m, _)
                    => ReadMessage(m), handler, emitOnCapturedContext);

            static PropertyChanges<FileChooserProperties> ReadMessage(Message message)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new();
                return new PropertyChanges<FileChooserProperties>(ReadProperties(ref reader, changed), changed.ToArray(),
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
                        case "version":
                            invalidated.Add("Version");
                            break;
                    }
                }

                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }

        private static FileChooserProperties ReadProperties(ref Reader reader, ICollection<string>? changedList = null)
        {
            var props = new FileChooserProperties();
            var headersEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(headersEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "version":
                        reader.ReadSignature("u");
                        props.Version = reader.ReadUInt32();
                        changedList?.Add("Version");
                        break;
                    default:
                        reader.ReadVariant();
                        break;
                }
            }

            return props;
        }
    }

    internal class Request : DesktopObject
    {
        private const string Interface = "org.freedesktop.portal.Request";

        public Request(DesktopService service, ObjectPath path) : base(service, path) { }

        public Task CloseAsync()
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    "Close");
                return writer.CreateMessage();
            }
        }

        public ValueTask<IDisposable> WatchResponseAsync(Action<Exception?, (uint response, IDictionary<string, object> results)> handler, bool emitOnCapturedContext = true)
            => WatchSignalAsync(Service.Destination, Interface, Path, "Response",
                static (m, s) => ReadMessage_uaesv(m, (DesktopObject)s!), handler, emitOnCapturedContext);
    }

    internal class DesktopService
    {
        public DesktopService(Connection connection, string destination)
            => (Connection, Destination) = (connection, destination);

        public Connection Connection { get; }
        public string Destination { get; }
        public Notification CreateNotification(string path) => new(this, path);
        public OpenURI CreateOpenUri(string path) => new(this, path);
        public DynamicLauncher CreateDynamicLauncher(string path) => new(this, path);
        public FileChooser CreateFileChooser(string path) => new(this, path);
        public Request CreateRequest(string path) => new(this, path);
    }

    internal class DesktopObject
    {
        protected DesktopObject(DesktopService service, ObjectPath path)
            => (Service, Path) = (service, path);

        protected DesktopService Service { get; }
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
            return Connection.AddMatchAsync(rule, reader, static (ex, changes, _, hs)
                    => ((Action<Exception?, PropertyChanges<TProperties>>)hs!).Invoke(ex, changes), this, handler, emitOnCapturedContext);
        }

        protected ValueTask<IDisposable> WatchSignalAsync<TArg>(string sender, string @interface, ObjectPath path, string signal,
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

        protected static ObjectPath ReadMessage_o(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadObjectPath();
        }

        protected static uint ReadMessage_v_u(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("u");
            return reader.ReadUInt32();
        }

        protected static (string, string, object[]) ReadMessage_ssav(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadString();
            var arg1 = reader.ReadString();
            var arg2 = reader.ReadArray<object>();
            return (arg0, arg1, arg2);
        }

        protected static (uint, Dictionary<string, object>) ReadMessage_uaesv(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadUInt32();
            var arg1 = reader.ReadDictionary<string, object>();
            return (arg0, arg1);
        }

        protected static string ReadMessage_s(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadString();
        }

        protected static (object, string, uint) ReadMessage_vsu(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadVariant();
            var arg1 = reader.ReadString();
            var arg2 = reader.ReadUInt32();
            return (arg0, arg1, arg2);
        }
    }

    internal class PropertyChanges<TProperties>
    {
        public PropertyChanges(TProperties properties, string[] invalidated, string[] changed)
            => (Properties, Invalidated, Changed) = (properties, invalidated, changed);

        public TProperties Properties { get; }
        public string[] Invalidated { get; }
        public string[] Changed { get; }
        public bool HasChanged(string property) => Array.IndexOf(Changed, property) != -1;
        public bool IsInvalidated(string property) => Array.IndexOf(Invalidated, property) != -1;
    }
}
