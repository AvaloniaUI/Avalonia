using System;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Avalonia.FreeDesktop.DBusIme.IBus
{
    internal class Portal : IBusObject
    {
        private const string Interface = "org.freedesktop.IBus.Portal";

        public Portal(IBusService service, ObjectPath path) : base(service, path) { }

        public Task<ObjectPath> CreateInputContextAsync(string clientName)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_o(m, (IBusObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "s",
                    member: "CreateInputContext");
                writer.WriteString(clientName);
                return writer.CreateMessage();
            }
        }
    }

    internal class InputContext : IBusObject
    {
        private const string Interface = "org.freedesktop.IBus.InputContext";

        public InputContext(IBusService service, ObjectPath path) : base(service, path) { }

        public Task<bool> ProcessKeyEventAsync(uint keyval, uint keycode, uint state)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_b(m, (IBusObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "uuu",
                    member: "ProcessKeyEvent");
                writer.WriteUInt32(keyval);
                writer.WriteUInt32(keycode);
                writer.WriteUInt32(state);
                return writer.CreateMessage();
            }
        }

        public Task SetCursorLocationAsync(int x, int y, int w, int h)
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "iiii",
                    member: "SetCursorLocation");
                writer.WriteInt32(x);
                writer.WriteInt32(y);
                writer.WriteInt32(w);
                writer.WriteInt32(h);
                return writer.CreateMessage();
            }
        }

        public Task SetCursorLocationRelativeAsync(int x, int y, int w, int h)
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "iiii",
                    member: "SetCursorLocationRelative");
                writer.WriteInt32(x);
                writer.WriteInt32(y);
                writer.WriteInt32(w);
                writer.WriteInt32(h);
                return writer.CreateMessage();
            }
        }

        public Task ProcessHandWritingEventAsync(double[] coordinates)
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "ad",
                    member: "ProcessHandWritingEvent");
                writer.WriteArray(coordinates);
                return writer.CreateMessage();
            }
        }

        public Task CancelHandWritingAsync(uint nStrokes)
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
                    member: "CancelHandWriting");
                writer.WriteUInt32(nStrokes);
                return writer.CreateMessage();
            }
        }

        public Task FocusInAsync()
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    "FocusIn");
                return writer.CreateMessage();
            }
        }

        public Task FocusOutAsync()
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    "FocusOut");
                return writer.CreateMessage();
            }
        }

        public Task ResetAsync()
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    "Reset");
                return writer.CreateMessage();
            }
        }

        public Task SetCapabilitiesAsync(uint caps)
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
                    member: "SetCapabilities");
                writer.WriteUInt32(caps);
                return writer.CreateMessage();
            }
        }

        public Task PropertyActivateAsync(string name, uint state)
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "su",
                    member: "PropertyActivate");
                writer.WriteString(name);
                writer.WriteUInt32(state);
                return writer.CreateMessage();
            }
        }

        public Task SetEngineAsync(string name)
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
                    member: "SetEngine");
                writer.WriteString(name);
                return writer.CreateMessage();
            }
        }

        public Task<object> GetEngineAsync()
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_v(m, (IBusObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    "GetEngine");
                return writer.CreateMessage();
            }
        }

        public Task SetSurroundingTextAsync(object text, uint cursorPos, uint anchorPos)
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "vuu",
                    member: "SetSurroundingText");
                writer.WriteVariant(text);
                writer.WriteUInt32(cursorPos);
                writer.WriteUInt32(anchorPos);
                return writer.CreateMessage();
            }
        }

        public ValueTask<IDisposable> WatchCommitTextAsync(Action<Exception?, object> handler, bool emitOnCapturedContext = true) =>
            WatchSignalAsync(Service.Destination, Interface, Path, "CommitText", static (m, s) =>
                ReadMessage_v(m, (IBusObject)s!), handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchForwardKeyEventAsync(Action<Exception?, (uint Keyval, uint Keycode, uint State)> handler,
            bool emitOnCapturedContext = true) =>
            WatchSignalAsync(Service.Destination, Interface, Path, "ForwardKeyEvent", static (m, s) =>
                ReadMessage_uuu(m, (IBusObject)s!), handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchUpdatePreeditTextAsync(Action<Exception?, (object Text, uint CursorPos, bool Visible)> handler,
            bool emitOnCapturedContext = true) =>
            WatchSignalAsync(Service.Destination, Interface, Path, "UpdatePreeditText", static (m, s) =>
                ReadMessage_vub(m, (IBusObject)s!), handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchUpdatePreeditTextWithModeAsync(
            Action<Exception?, (object Text, uint CursorPos, bool Visible, uint Mode)> handler, bool emitOnCapturedContext = true) =>
            WatchSignalAsync(Service.Destination, Interface, Path, "UpdatePreeditTextWithMode", static (m, s) =>
                ReadMessage_vubu(m, (IBusObject)s!), handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchShowPreeditTextAsync(Action<Exception?> handler, bool emitOnCapturedContext = true)
            => WatchSignalAsync(Service.Destination, Interface, Path, "ShowPreeditText", handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchHidePreeditTextAsync(Action<Exception?> handler, bool emitOnCapturedContext = true)
            => WatchSignalAsync(Service.Destination, Interface, Path, "HidePreeditText", handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchUpdateAuxiliaryTextAsync(Action<Exception?, (object Text, bool Visible)> handler,
            bool emitOnCapturedContext = true) =>
            WatchSignalAsync(Service.Destination, Interface, Path, "UpdateAuxiliaryText", static (m, s) =>
                ReadMessage_vb(m, (IBusObject)s!), handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchShowAuxiliaryTextAsync(Action<Exception?> handler, bool emitOnCapturedContext = true)
            => WatchSignalAsync(Service.Destination, Interface, Path, "ShowAuxiliaryText", handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchHideAuxiliaryTextAsync(Action<Exception?> handler, bool emitOnCapturedContext = true)
            => WatchSignalAsync(Service.Destination, Interface, Path, "HideAuxiliaryText", handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchUpdateLookupTableAsync(Action<Exception?, (object Table, bool Visible)> handler,
            bool emitOnCapturedContext = true) =>
            WatchSignalAsync(Service.Destination, Interface, Path, "UpdateLookupTable", static (m, s) =>
                ReadMessage_vb(m, (IBusObject)s!), handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchShowLookupTableAsync(Action<Exception?> handler, bool emitOnCapturedContext = true)
            => WatchSignalAsync(Service.Destination, Interface, Path, "ShowLookupTable", handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchHideLookupTableAsync(Action<Exception?> handler, bool emitOnCapturedContext = true)
            => WatchSignalAsync(Service.Destination, Interface, Path, "HideLookupTable", handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchPageUpLookupTableAsync(Action<Exception?> handler, bool emitOnCapturedContext = true)
            => WatchSignalAsync(Service.Destination, Interface, Path, "PageUpLookupTable", handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchPageDownLookupTableAsync(Action<Exception?> handler, bool emitOnCapturedContext = true)
            => WatchSignalAsync(Service.Destination, Interface, Path, "PageDownLookupTable", handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchCursorUpLookupTableAsync(Action<Exception?> handler, bool emitOnCapturedContext = true)
            => WatchSignalAsync(Service.Destination, Interface, Path, "CursorUpLookupTable", handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchCursorDownLookupTableAsync(Action<Exception?> handler, bool emitOnCapturedContext = true)
            => WatchSignalAsync(Service.Destination, Interface, Path, "CursorDownLookupTable", handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchRegisterPropertiesAsync(Action<Exception?, object> handler, bool emitOnCapturedContext = true)
            => WatchSignalAsync(Service.Destination, Interface, Path, "RegisterProperties", static (m, s) => ReadMessage_v(m, (IBusObject)s!), handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchUpdatePropertyAsync(Action<Exception?, object> handler, bool emitOnCapturedContext = true)
            => WatchSignalAsync(Service.Destination, Interface, Path, "UpdateProperty", static (m, s) => ReadMessage_v(m, (IBusObject)s!), handler, emitOnCapturedContext);
    }

    internal class Service : IBusObject
    {
        private const string Interface = "org.freedesktop.IBus.Service";

        public Service(IBusService service, ObjectPath path) : base(service, path) { }

        public Task DestroyAsync()
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    "Destroy");
                return writer.CreateMessage();
            }
        }
    }

    internal class IBusService
    {
        public IBusService(Connection connection, string destination)
            => (Connection, Destination) = (connection, destination);

        public Connection Connection { get; }
        public string Destination { get; }
        public Portal CreatePortal(string path) => new(this, path);
        public InputContext CreateInputContext(string path) => new(this, path);
        public Service CreateService(string path) => new(this, path);
    }

    internal class IBusObject
    {
        protected IBusObject(IBusService service, ObjectPath path)
            => (Service, Path) = (service, path);

        public IBusService Service { get; }
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
            return Connection.AddMatchAsync(rule, reader, static (ex, arg, _, hs) => ((Action<Exception?, TArg>)hs!).Invoke(ex, arg),
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
            return Connection.AddMatchAsync<object>(rule, static (_, _) => null!, static (ex, _, _, hs) => ((Action<Exception?>)hs!).Invoke(ex), this, handler, emitOnCapturedContext);
        }

        protected static ObjectPath ReadMessage_o(Message message, IBusObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadObjectPath();
        }

        protected static bool ReadMessage_b(Message message, IBusObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadBool();
        }

        protected static object ReadMessage_v(Message message, IBusObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadVariant();
        }

        protected static (uint, uint, uint) ReadMessage_uuu(Message message, IBusObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadUInt32();
            var arg1 = reader.ReadUInt32();
            var arg2 = reader.ReadUInt32();
            return (arg0, arg1, arg2);
        }

        protected static (object, uint, bool) ReadMessage_vub(Message message, IBusObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadVariant();
            var arg1 = reader.ReadUInt32();
            var arg2 = reader.ReadBool();
            return (arg0, arg1, arg2);
        }

        protected static (object, uint, bool, uint) ReadMessage_vubu(Message message, IBusObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadVariant();
            var arg1 = reader.ReadUInt32();
            var arg2 = reader.ReadBool();
            var arg3 = reader.ReadUInt32();
            return (arg0, arg1, arg2, arg3);
        }

        protected static (object, bool) ReadMessage_vb(Message message, IBusObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadVariant();
            var arg1 = reader.ReadBool();
            return (arg0, arg1);
        }
    }
}
