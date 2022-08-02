using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Tmds.DBus.Connection.DynamicAssemblyName)]
namespace Avalonia.FreeDesktop.DBusIme.Fcitx
{
    [DBusInterface("org.fcitx.Fcitx.InputMethod")]
    interface IFcitxInputMethod : IDBusObject
    {
        Task<(int icid, bool enable, uint keyval1, uint state1, uint keyval2, uint state2)> CreateICv3Async(
            string Appname, int Pid);
    }
    
    
    [DBusInterface("org.fcitx.Fcitx.InputContext")]
    interface IFcitxInputContext : IDBusObject
    {
        Task EnableICAsync();
        Task CloseICAsync();
        Task FocusInAsync();
        Task FocusOutAsync();
        Task ResetAsync();
        Task MouseEventAsync(int X);
        Task SetCursorLocationAsync(int X, int Y);
        Task SetCursorRectAsync(int X, int Y, int W, int H);
        Task SetCapacityAsync(uint Caps);
        Task SetSurroundingTextAsync(string Text, uint Cursor, uint Anchor);
        Task SetSurroundingTextPositionAsync(uint Cursor, uint Anchor);
        Task DestroyICAsync();
        Task<int> ProcessKeyEventAsync(uint Keyval, uint Keycode, uint State, int Type, uint Time);
        Task<IDisposable> WatchEnableIMAsync(Action handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchCloseIMAsync(Action handler, Action<Exception>? onError = null);
        Task<IDisposable?> WatchCommitStringAsync(Action<string> handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchCurrentIMAsync(Action<(string name, string uniqueName, string langCode)> handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchUpdatePreeditAsync(Action<(string str, int cursorpos)> handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchUpdateFormattedPreeditAsync(Action<((string, int)[] str, int cursorpos)> handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchUpdateClientSideUIAsync(Action<(string auxup, string auxdown, string preedit, string candidateword, string imname, int cursorpos)> handler, Action<Exception>? onError = null);
        Task<IDisposable?> WatchForwardKeyAsync(Action<(uint keyval, uint state, int type)> handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchDeleteSurroundingTextAsync(Action<(int offset, uint nchar)> handler, Action<Exception>? onError = null);
    }
    
    [DBusInterface("org.fcitx.Fcitx.InputContext1")]
    interface IFcitxInputContext1 : IDBusObject
    {
        Task FocusInAsync();
        Task FocusOutAsync();
        Task ResetAsync();
        Task SetCursorRectAsync(int X, int Y, int W, int H);
        Task SetCapabilityAsync(ulong Caps);
        Task SetSurroundingTextAsync(string Text, uint Cursor, uint Anchor);
        Task SetSurroundingTextPositionAsync(uint Cursor, uint Anchor);
        Task DestroyICAsync();
        Task<bool> ProcessKeyEventAsync(uint Keyval, uint Keycode, uint State, bool Type, uint Time);
        Task<IDisposable?> WatchCommitStringAsync(Action<string> handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchCurrentIMAsync(Action<(string name, string uniqueName, string langCode)> handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchUpdateFormattedPreeditAsync(Action<((string, int)[] str, int cursorpos)> handler, Action<Exception>? onError = null);
        Task<IDisposable?> WatchForwardKeyAsync(Action<(uint keyval, uint state, bool type)> handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchDeleteSurroundingTextAsync(Action<(int offset, uint nchar)> handler, Action<Exception>? onError = null);
    }

    [DBusInterface("org.fcitx.Fcitx.InputMethod1")]
    interface IFcitxInputMethod1 : IDBusObject
    {
        Task<(ObjectPath path, byte[] data)> CreateInputContextAsync((string, string)[] arg0);
    }
}
