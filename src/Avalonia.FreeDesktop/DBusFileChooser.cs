using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Connection.DynamicAssemblyName)]
namespace Avalonia.FreeDesktop
{
    [DBusInterface("org.freedesktop.portal.FileChooser")]
    internal interface IFileChooser : IDBusObject
    {
        Task<ObjectPath> OpenFileAsync(string ParentWindow, string Title, IDictionary<string, object> Options);
        Task<ObjectPath> SaveFileAsync(string ParentWindow, string Title, IDictionary<string, object> Options);
        Task<ObjectPath> SaveFilesAsync(string ParentWindow, string Title, IDictionary<string, object> Options);
        Task<T> GetAsync<T>(string prop);
        Task<FileChooserProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    internal class FileChooserProperties
    {
        public uint Version { get; set; }
    }

    internal static class FileChooserExtensions
    {
        public static Task<uint> GetVersionAsync(this IFileChooser o) => o.GetAsync<uint>("version");
    }
}
