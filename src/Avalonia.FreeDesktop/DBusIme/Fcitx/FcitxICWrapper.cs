using System;
using System.Threading.Tasks;

namespace Avalonia.FreeDesktop.DBusIme.Fcitx
{
    internal class FcitxICWrapper
    {
        private readonly IFcitxInputContext1 _modern;
        private readonly IFcitxInputContext _old;

        public FcitxICWrapper(IFcitxInputContext old)
        {
            _old = old;
        }

        public FcitxICWrapper(IFcitxInputContext1 modern)
        {
            _modern = modern;
        }

        public Task FocusInAsync() => _old?.FocusInAsync() ?? _modern.FocusInAsync();

        public Task FocusOutAsync() => _old?.FocusOutAsync() ?? _modern.FocusOutAsync();
        
        public Task ResetAsync() => _old?.ResetAsync() ?? _modern.ResetAsync();

        public Task SetCursorRectAsync(int x, int y, int w, int h) =>
            _old?.SetCursorRectAsync(x, y, w, h) ?? _modern.SetCursorRectAsync(x, y, w, h);
        public Task DestroyICAsync() => _old?.DestroyICAsync() ?? _modern.DestroyICAsync();

        public async Task<bool> ProcessKeyEventAsync(uint keyVal, uint keyCode, uint state, int type, uint time)
        {
            if(_old!=null)
                return await _old.ProcessKeyEventAsync(keyVal, keyCode, state, type, time) != 0;
            return await _modern.ProcessKeyEventAsync(keyVal, keyCode, state, type > 0, time);
        }

        public Task<IDisposable> WatchCommitStringAsync(Action<string> handler) =>
            _old?.WatchCommitStringAsync(handler) ?? _modern.WatchCommitStringAsync(handler);

        public Task<IDisposable> WatchForwardKeyAsync(Action<(uint keyval, uint state, int type)> handler)
        {
            return _old?.WatchForwardKeyAsync(handler)
                   ?? _modern.WatchForwardKeyAsync(ev =>
                       handler((ev.keyval, ev.state, ev.type ? 1 : 0)));
        }

        public Task SetCapacityAsync(uint flags) =>
            _old?.SetCapacityAsync(flags) ?? _modern.SetCapabilityAsync(flags);
    }
}
