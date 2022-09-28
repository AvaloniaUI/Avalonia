using System;
using System.Runtime.InteropServices;

namespace Avalonia.Web
{
    public static partial class Emscripten
    {
        const string Prefix = "av_";
        const string Library = "libEmscripten";

        [LibraryImport(Library, EntryPoint = Prefix + "log", StringMarshalling = StringMarshalling.Utf8)]
        public static partial void Log(EM_LOG flags, string format);

        [LibraryImport(Library, EntryPoint = Prefix + "debugger")]
        public static partial void Debugger();
    }

    [Flags]
    public enum EM_LOG : int
    {
        CONSOLE = 1,
        WARN = 2,
        ERROR = 4,
        C_STACK = 8,
        JS_STACK = 16,
        NO_PATHS = 64,
        FUNC_PARAMS = 128,
        DEBUG = 256,
        INFO = 512,
    }
}
