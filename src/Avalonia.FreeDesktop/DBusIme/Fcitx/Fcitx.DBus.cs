using System;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Avalonia.FreeDesktop.DBusIme.Fcitx
{
    internal class InputContext : FcitxObject
    {
        private const string Interface = "org.fcitx.Fcitx.InputContext";

        public InputContext(FcitxService service, ObjectPath path) : base(service, path) { }

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

        public Task SetCursorRectAsync(int x, int y, int w, int h)
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
                    member: "SetCursorRect");
                writer.WriteInt32(x);
                writer.WriteInt32(y);
                writer.WriteInt32(w);
                writer.WriteInt32(h);
                return writer.CreateMessage();
            }
        }

        public Task SetCapacityAsync(uint caps)
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
                    member: "SetCapacity");
                writer.WriteUInt32(caps);
                return writer.CreateMessage();
            }
        }

        public Task SetSurroundingTextAsync(string text, uint cursor, uint anchor)
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "suu",
                    member: "SetSurroundingText");
                writer.WriteString(text);
                writer.WriteUInt32(cursor);
                writer.WriteUInt32(anchor);
                return writer.CreateMessage();
            }
        }

        public Task SetSurroundingTextPositionAsync(uint cursor, uint anchor)
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "uu",
                    member: "SetSurroundingTextPosition");
                writer.WriteUInt32(cursor);
                writer.WriteUInt32(anchor);
                return writer.CreateMessage();
            }
        }

        public Task DestroyICAsync()
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    "DestroyIC");
                return writer.CreateMessage();
            }
        }

        public Task<int> ProcessKeyEventAsync(uint keyval, uint keycode, uint state, int type, uint time)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_i(m, (FcitxObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "uuuiu",
                    member: "ProcessKeyEvent");
                writer.WriteUInt32(keyval);
                writer.WriteUInt32(keycode);
                writer.WriteUInt32(state);
                writer.WriteInt32(type);
                writer.WriteUInt32(time);
                return writer.CreateMessage();
            }
        }

        public ValueTask<IDisposable> WatchCommitStringAsync(Action<Exception?, string> handler, bool emitOnCapturedContext = true) =>
            WatchSignalAsync(Service.Destination, Interface, Path, "CommitString", static (m, s) =>
                ReadMessage_s(m, (FcitxObject)s!), handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchCurrentIMAsync(Action<Exception?, (string Name, string UniqueName, string LangCode)> handler,
            bool emitOnCapturedContext = true) =>
            WatchSignalAsync(Service.Destination, Interface, Path, "CurrentIM", static (m, s) =>
                ReadMessage_sss(m, (FcitxObject)s!), handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchUpdateFormattedPreeditAsync(Action<Exception?, ((string, int)[] Str, int Cursorpos)> handler,
            bool emitOnCapturedContext = true) =>
            WatchSignalAsync(Service.Destination, Interface, Path, "UpdateFormattedPreedit", static (m, s) =>
                ReadMessage_arsizi(m, (FcitxObject)s!), handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchForwardKeyAsync(Action<Exception?, (uint Keyval, uint State, int Type)> handler,
            bool emitOnCapturedContext = true) =>
            WatchSignalAsync(Service.Destination, Interface, Path, "ForwardKey", static (m, s) =>
                ReadMessage_uui(m, (FcitxObject)s!), handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchDeleteSurroundingTextAsync(Action<Exception?, (int Offset, uint Nchar)> handler,
            bool emitOnCapturedContext = true) =>
            WatchSignalAsync(Service.Destination, Interface, Path, "DeleteSurroundingText", static (m, s) =>
                ReadMessage_iu(m, (FcitxObject)s!), handler, emitOnCapturedContext);
    }

    internal class InputMethod : FcitxObject
    {
        private const string Interface = "org.fcitx.Fcitx.InputMethod";

        public InputMethod(FcitxService service, ObjectPath path) : base(service, path) { }

        public Task<(int Icid, bool Enable, uint Keyval1, uint State1, uint Keyval2, uint State2)> CreateICv3Async(string appname, int pid)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_ibuuuu(m, (FcitxObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "si",
                    member: "CreateICv3");
                writer.WriteString(appname);
                writer.WriteInt32(pid);
                return writer.CreateMessage();
            }
        }
    }

    internal class InputContext1 : FcitxObject
    {
        private const string Interface = "org.fcitx.Fcitx.InputContext1";

        public InputContext1(FcitxService service, ObjectPath path) : base(service, path) { }

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

        public Task SetCursorRectAsync(int x, int y, int w, int h)
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
                    member: "SetCursorRect");
                writer.WriteInt32(x);
                writer.WriteInt32(y);
                writer.WriteInt32(w);
                writer.WriteInt32(h);
                return writer.CreateMessage();
            }
        }

        public Task SetCapabilityAsync(ulong caps)
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "t",
                    member: "SetCapability");
                writer.WriteUInt64(caps);
                return writer.CreateMessage();
            }
        }

        public Task SetSurroundingTextAsync(string text, uint cursor, uint anchor)
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "suu",
                    member: "SetSurroundingText");
                writer.WriteString(text);
                writer.WriteUInt32(cursor);
                writer.WriteUInt32(anchor);
                return writer.CreateMessage();
            }
        }

        public Task SetSurroundingTextPositionAsync(uint cursor, uint anchor)
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "uu",
                    member: "SetSurroundingTextPosition");
                writer.WriteUInt32(cursor);
                writer.WriteUInt32(anchor);
                return writer.CreateMessage();
            }
        }

        public Task DestroyICAsync()
        {
            return Connection.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    "DestroyIC");
                return writer.CreateMessage();
            }
        }

        public Task<bool> ProcessKeyEventAsync(uint keyval, uint keycode, uint state, bool type, uint time)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_b(m, (FcitxObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "uuubu",
                    member: "ProcessKeyEvent");
                writer.WriteUInt32(keyval);
                writer.WriteUInt32(keycode);
                writer.WriteUInt32(state);
                writer.WriteBool(type);
                writer.WriteUInt32(time);
                return writer.CreateMessage();
            }
        }

        public ValueTask<IDisposable> WatchCommitStringAsync(Action<Exception?, string> handler, bool emitOnCapturedContext = true)
            => WatchSignalAsync(Service.Destination, Interface, Path, "CommitString", static (m, s) =>
                    ReadMessage_s(m, (FcitxObject)s!), handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchCurrentIMAsync(Action<Exception?, (string Name, string UniqueName, string LangCode)> handler,
            bool emitOnCapturedContext = true) =>
            WatchSignalAsync(Service.Destination, Interface, Path, "CurrentIM", static (m, s) =>
                    ReadMessage_sss(m, (FcitxObject)s!), handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchUpdateFormattedPreeditAsync(Action<Exception?, ((string, int)[] Str, int Cursorpos)> handler,
            bool emitOnCapturedContext = true) =>
            WatchSignalAsync(Service.Destination, Interface, Path, "UpdateFormattedPreedit", static (m, s) =>
                ReadMessage_arsizi(m, (FcitxObject)s!), handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchForwardKeyAsync(Action<Exception?, (uint Keyval, uint State, bool Type)> handler,
            bool emitOnCapturedContext = true) =>
            WatchSignalAsync(Service.Destination, Interface, Path, "ForwardKey", static (m, s) =>
                    ReadMessage_uub(m, (FcitxObject)s!), handler, emitOnCapturedContext);

        public ValueTask<IDisposable> WatchDeleteSurroundingTextAsync(Action<Exception?, (int Offset, uint Nchar)> handler,
            bool emitOnCapturedContext = true) =>
            WatchSignalAsync(Service.Destination, Interface, Path, "DeleteSurroundingText", static (m, s) =>
                ReadMessage_iu(m, (FcitxObject)s!), handler, emitOnCapturedContext);
    }

    internal class InputMethod1 : FcitxObject
    {
        private const string Interface = "org.fcitx.Fcitx.InputMethod1";

        public InputMethod1(FcitxService service, ObjectPath path) : base(service, path) { }

        public Task<(ObjectPath A0, byte[] A1)> CreateInputContextAsync((string, string)[] a0)
        {
            return Connection.CallMethodAsync(CreateMessage(), static (m, s) => ReadMessage_oay(m, (FcitxObject)s!), this);

            MessageBuffer CreateMessage()
            {
                using var writer = Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    Service.Destination,
                    Path,
                    Interface,
                    signature: "a(ss)",
                    member: "CreateInputContext");
                writer.WriteArray(a0);
                return writer.CreateMessage();
            }
        }
    }

    internal class FcitxService
    {
        public FcitxService(Connection connection, string destination)
            => (Connection, Destination) = (connection, destination);

        public Connection Connection { get; }
        public string Destination { get; }
        public InputContext CreateInputContext(string path) => new(this, path);
        public InputMethod CreateInputMethod(string path) => new(this, path);
        public InputContext1 CreateInputContext1(string path) => new(this, path);
        public InputMethod1 CreateInputMethod1(string path) => new(this, path);
    }

    internal class FcitxObject
    {
        protected FcitxObject(FcitxService service, ObjectPath path)
            => (Service, Path) = (service, path);

        public FcitxService Service { get; }
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
            return Connection.AddMatchAsync<object>(rule, static (_, _) =>
                null!, static (ex, _, _, hs) =>
                ((Action<Exception?>)hs!).Invoke(ex), this, handler, emitOnCapturedContext);
        }

        protected static int ReadMessage_i(Message message, FcitxObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadInt32();
        }

        protected static string ReadMessage_s(Message message, FcitxObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadString();
        }

        protected static (string, string, string) ReadMessage_sss(Message message, FcitxObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadString();
            var arg1 = reader.ReadString();
            var arg2 = reader.ReadString();
            return (arg0, arg1, arg2);
        }

        protected static ((string, int)[], int) ReadMessage_arsizi(Message message, FcitxObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadArray<(string, int)>();
            var arg1 = reader.ReadInt32();
            return (arg0, arg1);
        }

        protected static (uint, uint, int) ReadMessage_uui(Message message, FcitxObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadUInt32();
            var arg1 = reader.ReadUInt32();
            var arg2 = reader.ReadInt32();
            return (arg0, arg1, arg2);
        }

        protected static (int, uint) ReadMessage_iu(Message message, FcitxObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadInt32();
            var arg1 = reader.ReadUInt32();
            return (arg0, arg1);
        }

        protected static (int, bool, uint, uint, uint, uint) ReadMessage_ibuuuu(Message message, FcitxObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadInt32();
            var arg1 = reader.ReadBool();
            var arg2 = reader.ReadUInt32();
            var arg3 = reader.ReadUInt32();
            var arg4 = reader.ReadUInt32();
            var arg5 = reader.ReadUInt32();
            return (arg0, arg1, arg2, arg3, arg4, arg5);
        }

        protected static bool ReadMessage_b(Message message, FcitxObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadBool();
        }

        protected static (uint, uint, bool) ReadMessage_uub(Message message, FcitxObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadUInt32();
            var arg1 = reader.ReadUInt32();
            var arg2 = reader.ReadBool();
            return (arg0, arg1, arg2);
        }

        protected static (ObjectPath, byte[]) ReadMessage_oay(Message message, FcitxObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadObjectPath();
            var arg1 = reader.ReadArray<byte>();
            return (arg0, arg1);
        }
    }
}
