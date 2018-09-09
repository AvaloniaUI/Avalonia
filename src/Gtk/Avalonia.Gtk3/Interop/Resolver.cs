using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia.Platform;

namespace Avalonia.Gtk3.Interop
{
    internal class GtkImportAttribute : Attribute
    {
        public GtkDll Dll { get; set; }
        public string Name { get; set; }
        public bool Optional { get; set; }

        public GtkImportAttribute(GtkDll dll, string name = null, bool optional = false)
        {
            Dll = dll;
            Name = name;
            Optional = optional;
        }
    }

    public enum GtkDll
    {
        Gdk,
        Gtk,
        Glib,
        Gio,
        Gobject,
        Cairo,
        GdkPixBuf
    }

    static class Resolver
    {
        private static Lazy<OperatingSystemType> Platform =
            new Lazy<OperatingSystemType>(
                () => AvaloniaLocator.Current.GetService<IRuntimePlatform>().GetRuntimeInfo().OperatingSystem);

        public static ICustomGtk3NativeLibraryResolver Custom { get; set; }


        static string FormatName(string name, int version = 0)
        {
            if (Platform.Value == OperatingSystemType.WinNT)
                return "lib" + name + "-" + version + ".dll";
            if (Platform.Value == OperatingSystemType.Linux)
                return "lib" + name + ".so" + "." + version;
            if (Platform.Value == OperatingSystemType.OSX)
                return "lib" + name + "." + version + ".dylib";
            throw new Exception("Unknown platform, use custom name resolver");
        }

        

        static string GetDllName(GtkDll dll)
        {
            var name = Custom?.GetName(dll);
            if (name != null)
                return name;

            switch (dll)
            {
                case GtkDll.Cairo:
                    return FormatName("cairo", 2);
                case GtkDll.Gdk:
                    return FormatName("gdk-3");
                case GtkDll.Glib:
                    return FormatName("glib-2.0");
                case GtkDll.Gio:
                    return FormatName("gio-2.0");
                case GtkDll.Gtk:
                    return FormatName("gtk-3");
                case GtkDll.Gobject:
                    return FormatName("gobject-2.0");
                case GtkDll.GdkPixBuf:
                    return FormatName("gdk_pixbuf-2.0");
                default:
                    throw new ArgumentException("Unknown lib: " + dll);
            }
        }

        static IntPtr LoadDll(IDynLoader  loader, GtkDll dll)
        {
            
            var exceptions = new List<Exception>();

            var name = GetDllName(dll);
            if (Custom?.TrySystemFirst != false)
            {
                try
                {
                    return loader.LoadLibrary(name);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }
            var path = Custom?.Lookup(dll);
            if (path == null && Custom?.BasePath != null)
                path = Path.Combine(Custom.BasePath, name);
            if (path != null)
            {
                try
                {
                    return loader.LoadLibrary(path);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }
            throw new AggregateException("Unable to load " + dll, exceptions);
        }

        public static void Resolve(string basePath = null)
        {
            var loader = Platform.Value == OperatingSystemType.WinNT ? (IDynLoader)new Win32Loader() : new UnixLoader();

            var dlls = Enum.GetValues(typeof(GtkDll)).Cast<GtkDll>().ToDictionary(x => x, x => LoadDll(loader, x));
            
            foreach (var fieldInfo in typeof(Native).GetTypeInfo().DeclaredFields)
            {
                var import = fieldInfo.FieldType.GetTypeInfo().GetCustomAttributes(typeof(GtkImportAttribute), true).Cast<GtkImportAttribute>().FirstOrDefault();
                if(import == null)
                    continue;
                IntPtr lib = dlls[import.Dll];

                var funcPtr =  loader.GetProcAddress(lib, import.Name ?? fieldInfo.FieldType.Name, import.Optional);

                if (funcPtr != IntPtr.Zero)
                    fieldInfo.SetValue(null, Marshal.GetDelegateForFunctionPointer(funcPtr, fieldInfo.FieldType));
            }

            var nativeHandleNames = new[] { "gdk_win32_window_get_handle", "gdk_x11_window_get_xid", "gdk_quartz_window_get_nswindow" };
            foreach (var name in nativeHandleNames)
            {
                var ptr = loader.GetProcAddress(dlls[GtkDll.Gdk], name, true);
                if (ptr == IntPtr.Zero)
                    continue;
                Native.GetNativeGdkWindowHandle = (Native.D.gdk_get_native_handle) Marshal
                    .GetDelegateForFunctionPointer(ptr, typeof(Native.D.gdk_get_native_handle));
            }
            if (Native.GetNativeGdkWindowHandle == null)
                throw new Exception($"Unable to locate any of [{string.Join(", ", nativeHandleNames)}] in libgdk");

        }


    }
}

