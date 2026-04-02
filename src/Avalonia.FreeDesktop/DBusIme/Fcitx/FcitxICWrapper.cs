using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.DBus;
using Avalonia.FreeDesktop.DBusXml;
using Avalonia.Reactive;

namespace Avalonia.FreeDesktop.DBusIme.Fcitx
{
    internal class FcitxICWrapper
    {
        private readonly OrgFcitxFcitxInputContext1Proxy? _modern;
        private readonly OrgFcitxFcitxInputContextProxy? _old;

        public FcitxICWrapper(OrgFcitxFcitxInputContextProxy old)
        {
            _old = old;
        }

        public FcitxICWrapper(OrgFcitxFcitxInputContext1Proxy modern)
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
            if (_old is not null)
                return await _old.ProcessKeyEventAsync(keyVal, keyCode, state, type, time) != 0;
            return await (_modern?.ProcessKeyEventAsync(keyVal, keyCode, state, type > 0, time) ?? Task.FromResult(false));
        }

        public Task<IDisposable> WatchCommitStringAsync(Action<string> handler) =>
            _old?.WatchCommitStringAsync(handler)
            ?? _modern?.WatchCommitStringAsync(handler)
            ?? Task.FromResult(Disposable.Empty);

        public Task<IDisposable> WatchForwardKeyAsync(Action<uint, uint, int> handler) =>
            _old?.WatchForwardKeyAsync(handler)
            ?? _modern?.WatchForwardKeyAsync((keyval, state, type) => handler.Invoke(keyval, state, type ? 1 : 0))
            ?? Task.FromResult(Disposable.Empty);

        public Task<IDisposable> WatchUpdateFormattedPreeditAsync(
            Action<List<FormattedPreeditSegment>, int> handler) =>
            _old?.WatchUpdateFormattedPreeditAsync(handler)
            ?? _modern?.WatchUpdateFormattedPreeditAsync(handler)
            ?? Task.FromResult(Disposable.Empty);

        public Task SetCapacityAsync(uint flags) =>
            _old?.SetCapacityAsync(flags) ?? _modern?.SetCapabilityAsync((ulong)flags) ?? Task.CompletedTask;
    }
}
