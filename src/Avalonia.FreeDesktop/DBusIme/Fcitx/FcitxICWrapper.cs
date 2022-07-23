using System;
using System.Threading.Tasks;

namespace Avalonia.FreeDesktop.DBusIme.Fcitx
{
    internal class FcitxICWrapper
    {
        private readonly IFcitxInputContext1? _modern;
        private readonly IFcitxInputContext? _old;

        public FcitxICWrapper(IFcitxInputContext old)
        {
            _old = old;
        }

        public FcitxICWrapper(IFcitxInputContext1 modern)
        {
            _modern = modern;
        }

        public Task FocusInAsync() => _old?.FocusInAsync() ?? _modern?.FocusInAsync() ?? Task.CompletedTask;

        public Task FocusOutAsync() => _old?.FocusOutAsync() ?? _modern?.FocusOutAsync() ?? Task.CompletedTask;
        
        public Task ResetAsync() => _old?.ResetAsync() ?? _modern?.ResetAsync() ?? Task.CompletedTask;

        public Task SetCursorRectAsync(int x, int y, int w, int h) =>
            _old?.SetCursorRectAsync(x, y, w, h) ?? _modern?.SetCursorRectAsync(x, y, w, h) ?? Task.CompletedTask;
        public Task DestroyICAsync() => _old?.DestroyICAsync() ?? _modern?.DestroyICAsync() ?? Task.CompletedTask;

        public async Task<bool> ProcessKeyEventAsync(uint keyVal, uint keyCode, uint state, int type, uint time)
        {
            if(_old!=null)
                return await _old.ProcessKeyEventAsync(keyVal, keyCode, state, type, time) != 0;
            return await (_modern?.ProcessKeyEventAsync(keyVal, keyCode, state, type > 0, time) ?? Task.FromResult(false));
        }

        public Task<IDisposable?> WatchCommitStringAsync(Action<string> handler) =>
            _old?.WatchCommitStringAsync(handler) 
                ?? _modern?.WatchCommitStringAsync(handler) 
                    ?? Task.FromResult(default(IDisposable?));

        public Task<IDisposable?> WatchForwardKeyAsync(Action<(uint keyval, uint state, int type)> handler)
        {
            return _old?.WatchForwardKeyAsync(handler)
                   ?? _modern?.WatchForwardKeyAsync(ev =>
                       handler((ev.keyval, ev.state, ev.type ? 1 : 0)))
                    ?? Task.FromResult(default(IDisposable?));
        }

        public Task SetCapacityAsync(uint flags) =>
            _old?.SetCapacityAsync(flags) ?? _modern?.SetCapabilityAsync(flags) ?? Task.CompletedTask;
    }
}
