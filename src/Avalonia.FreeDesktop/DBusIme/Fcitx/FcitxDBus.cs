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
        Task<(int icid, uint keyval1, uint state1, uint keyval2, uint state2)> CreateICAsync();
        Task<(int icid, bool enable, uint keyval1, uint state1, uint keyval2, uint state2)> CreateICv2Async(string Appname);
        Task<(int icid, bool enable, uint keyval1, uint state1, uint keyval2, uint state2)> CreateICv3Async(string Appname, int Pid);
        Task ExitAsync();
        Task<string> GetCurrentIMAsync();
        Task SetCurrentIMAsync(string Im);
        Task ReloadConfigAsync();
        Task ReloadAddonConfigAsync(string Addon);
        Task RestartAsync();
        Task ConfigureAsync();
        Task ConfigureAddonAsync(string Addon);
        Task ConfigureIMAsync(string Im);
        Task<string> GetCurrentUIAsync();
        Task<string> GetIMAddonAsync(string Im);
        Task ActivateIMAsync();
        Task InactivateIMAsync();
        Task ToggleIMAsync();
        Task ResetIMListAsync();
        Task<int> GetCurrentStateAsync();
        Task<T> GetAsync<T>(string prop);
        Task<FcitxInputMethodProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    class FcitxInputMethodProperties
    {
        private (string, string, string, bool)[] _IMList = default((string, string, string, bool)[]);
        public (string, string, string, bool)[] IMList
        {
            get
            {
                return _IMList;
            }

            set
            {
                _IMList = (value);
            }
        }

        private string _CurrentIM = default(string);
        public string CurrentIM
        {
            get
            {
                return _CurrentIM;
            }

            set
            {
                _CurrentIM = (value);
            }
        }
    }

    static class FcitxInputMethodExtensions
    {
        public static Task<(string, string, string, bool)[]> GetIMListAsync(this IFcitxInputMethod o) => o.GetAsync<(string, string, string, bool)[]>("IMList");
        public static Task<string> GetCurrentIMAsync(this IFcitxInputMethod o) => o.GetAsync<string>("CurrentIM");
        public static Task SetIMListAsync(this IFcitxInputMethod o, (string, string, string, bool)[] val) => o.SetAsync("IMList", val);
        public static Task SetCurrentIMAsync(this IFcitxInputMethod o, string val) => o.SetAsync("CurrentIM", val);
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
        Task<IDisposable> WatchEnableIMAsync(Action handler, Action<Exception> onError = null);
        Task<IDisposable> WatchCloseIMAsync(Action handler, Action<Exception> onError = null);
        Task<IDisposable> WatchCommitStringAsync(Action<string> handler, Action<Exception> onError = null);
        Task<IDisposable> WatchCurrentIMAsync(Action<(string name, string uniqueName, string langCode)> handler, Action<Exception> onError = null);
        Task<IDisposable> WatchUpdatePreeditAsync(Action<(string str, int cursorpos)> handler, Action<Exception> onError = null);
        Task<IDisposable> WatchUpdateFormattedPreeditAsync(Action<((string, int)[] str, int cursorpos)> handler, Action<Exception> onError = null);
        Task<IDisposable> WatchUpdateClientSideUIAsync(Action<(string auxup, string auxdown, string preedit, string candidateword, string imname, int cursorpos)> handler, Action<Exception> onError = null);
        Task<IDisposable> WatchForwardKeyAsync(Action<(uint keyval, uint state, int type)> handler, Action<Exception> onError = null);
        Task<IDisposable> WatchDeleteSurroundingTextAsync(Action<(int offset, uint nchar)> handler, Action<Exception> onError = null);
    }
}
