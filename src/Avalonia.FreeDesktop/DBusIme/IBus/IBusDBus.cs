using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Connection.DynamicAssemblyName)]
namespace Avalonia.FreeDesktop.DBusIme.IBus
{
    [DBusInterface("org.freedesktop.IBus.InputContext")]
    interface IIBusInputContext : IDBusObject
    {
        Task<bool> ProcessKeyEventAsync(uint Keyval, uint Keycode, uint State);
        Task SetCursorLocationAsync(int X, int Y, int W, int H);
        Task FocusInAsync();
        Task FocusOutAsync();
        Task ResetAsync();
        Task SetCapabilitiesAsync(uint Caps);
        Task PropertyActivateAsync(string Name, int State);
        Task SetEngineAsync(string Name);
        Task<object> GetEngineAsync();
        Task DestroyAsync();
        Task SetSurroundingTextAsync(object Text, uint CursorPos, uint AnchorPos);
        Task<IDisposable> WatchCommitTextAsync(Action<object> cb, Action<Exception>? onError = null);
        Task<IDisposable> WatchForwardKeyEventAsync(Action<(uint keyval, uint keycode, uint state)> handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchRequireSurroundingTextAsync(Action handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchDeleteSurroundingTextAsync(Action<(int offset, uint nchars)> handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchUpdatePreeditTextAsync(Action<(object text, uint cursorPos, bool visible)> handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchShowPreeditTextAsync(Action handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchHidePreeditTextAsync(Action handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchUpdateAuxiliaryTextAsync(Action<(object text, bool visible)> handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchShowAuxiliaryTextAsync(Action handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchHideAuxiliaryTextAsync(Action handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchUpdateLookupTableAsync(Action<(object table, bool visible)> handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchShowLookupTableAsync(Action handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchHideLookupTableAsync(Action handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchPageUpLookupTableAsync(Action handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchPageDownLookupTableAsync(Action handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchCursorUpLookupTableAsync(Action handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchCursorDownLookupTableAsync(Action handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchRegisterPropertiesAsync(Action<object> handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchUpdatePropertyAsync(Action<object> handler, Action<Exception>? onError = null);
    }
    
    
    [DBusInterface("org.freedesktop.IBus.Portal")]
    interface IIBusPortal : IDBusObject
    {
        Task<ObjectPath> CreateInputContextAsync(string Name);
    }
}
