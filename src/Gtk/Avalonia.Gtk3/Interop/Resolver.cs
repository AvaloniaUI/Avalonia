using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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

    internal enum GtkDll
    {
        Gdk,
        Gtk,
        Glib,
        Gio,
        Gobject
    }

    static class Resolver
    {
        [DllImport("kernel32.dll")]
        static extern int GetVersion();

        static bool IsWin32()
        {
            try
            {
                GetVersion();
                return true;
            }
            catch
            {
                return false;
            }
        }

        

        public static void Resolve(string basePath = null)
        {
            var loader = IsWin32() ? (IDynLoader)new Win32Loader() : new UnixLoader();
            

            var gdk = loader.LoadLibrary(basePath, "libgdk-3");
            var gtk = loader.LoadLibrary(basePath, "libgtk-3");
            var gio = loader.LoadLibrary(basePath, "libgio-2.0");
            var glib = loader.LoadLibrary(basePath, "libglib-2.0");
            var gobject = loader.LoadLibrary(basePath, "libgobject-2.0");
            foreach (var fieldInfo in typeof(Native).GetTypeInfo().DeclaredFields)
            {
                var import = fieldInfo.FieldType.GetTypeInfo().GetCustomAttributes(typeof(GtkImportAttribute), true).Cast<GtkImportAttribute>().FirstOrDefault();
                if(import == null)
                    continue;
                IntPtr lib;
                if (import.Dll == GtkDll.Gtk)
                    lib = gtk;
                else if (import.Dll == GtkDll.Gdk)
                    lib = gdk;
                else if (import.Dll == GtkDll.Gio)
                    lib = gio;
                else if (import.Dll == GtkDll.Glib)
                    lib = glib;
                else if (import.Dll == GtkDll.Gobject)
                    lib = gobject;
                else
                    throw new ArgumentException("Invalid GtkImportAttribute for " + fieldInfo.FieldType);

                var funcPtr = loader.GetProcAddress(lib, import.Name ?? fieldInfo.FieldType.Name);
                fieldInfo.SetValue(null, Marshal.GetDelegateForFunctionPointer(funcPtr, fieldInfo.FieldType));
            }

            var nativeHandleNames = new[] {"gdk_x11_window_get_xid", "gdk_win32_window_get_handle"};
            foreach (var name in nativeHandleNames)
            {
                try
                {
                    Native.GetNativeGdkWindowHandle = (Native.D.gdk_get_native_handle)Marshal
                        .GetDelegateForFunctionPointer(loader.GetProcAddress(gdk, name), typeof(Native.D.gdk_get_native_handle));
                }
                catch { }
            }
            if (Native.GetNativeGdkWindowHandle == null)
                throw new Exception($"Unable to locate any of [{string.Join(", ", nativeHandleNames)}] in libgdk");

        }


    }
}
