using Avalonia.Gtk3.Interop;

namespace Avalonia.Gtk3
{
    public interface ICustomGtk3NativeLibraryResolver
    {
        string GetName(GtkDll dll);
        string BasePath { get; }
        bool TrySystemFirst { get; }
        string Lookup(GtkDll dll);
    }
}
