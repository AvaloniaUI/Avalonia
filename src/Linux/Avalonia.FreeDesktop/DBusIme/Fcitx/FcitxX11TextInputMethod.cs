using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Logging;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace Avalonia.FreeDesktop.DBusIme.Fcitx
{
    internal class FcitxX11TextInputMethod : DBusTextInputMethodBase
    {
        private FcitxICWrapper? _context;
        private FcitxCapabilityFlags? _lastReportedFlags;

        public FcitxX11TextInputMethod(Connection connection) : base(connection, "org.fcitx.Fcitx", "org.freedesktop.portal.Fcitx") { }

        protected override async Task<bool> Connect(string name)
        {
            if (name == "org.fcitx.Fcitx")
            {
                var method = new OrgFcitxFcitxInputMethod(Connection, name, "/inputmethod");
                var resp = await method.CreateICv3Async(GetAppName(),
                    Process.GetCurrentProcess().Id);

                var proxy = new OrgFcitxFcitxInputContext(Connection, name, $"/inputcontext_{resp.icid}");
                _context = new FcitxICWrapper(proxy);
            }
            else
            {
                var method = new OrgFcitxFcitxInputMethod1(Connection, name, "/inputmethod");
                var resp = await method.CreateInputContextAsync(new[] { ("appName", GetAppName()) });
                var proxy = new OrgFcitxFcitxInputContext1(Connection, name, resp.Item1);
                _context = new FcitxICWrapper(proxy);
            }

            AddDisposable(await _context.WatchCommitStringAsync(OnCommitString));
            AddDisposable(await _context.WatchForwardKeyAsync(OnForward));
            return true;
        }

        protected override Task DisconnectAsync() => _context?.DestroyICAsync() ?? Task.CompletedTask;

        protected override void OnDisconnected() => _context = null;

        protected override void Reset()
        {
            _lastReportedFlags = null;
            base.Reset();
        }

        protected override Task SetCursorRectCore(PixelRect cursorRect) =>
            _context?.SetCursorRectAsync(cursorRect.X, cursorRect.Y, Math.Max(1, cursorRect.Width),
                Math.Max(1, cursorRect.Height))
            ?? Task.CompletedTask;

        protected override Task SetActiveCore(bool active)=> (active
                ? _context?.FocusInAsync()
                : _context?.FocusOutAsync())
             ?? Task.CompletedTask;

        protected override Task ResetContextCore() => _context?.ResetAsync() ?? Task.CompletedTask;

        protected override async Task<bool> HandleKeyCore(RawKeyEventArgs args, int keyVal, int keyCode)
        {
            FcitxKeyState state = default;
            if (args.Modifiers.HasAllFlags(RawInputModifiers.Control))
                state |= FcitxKeyState.FcitxKeyState_Ctrl;
            if (args.Modifiers.HasAllFlags(RawInputModifiers.Alt))
                state |= FcitxKeyState.FcitxKeyState_Alt;
            if (args.Modifiers.HasAllFlags(RawInputModifiers.Shift))
                state |= FcitxKeyState.FcitxKeyState_Shift;
            if (args.Modifiers.HasAllFlags(RawInputModifiers.Meta))
                state |= FcitxKeyState.FcitxKeyState_Super;

            var type = args.Type == RawKeyEventType.KeyDown ?
                FcitxKeyEventType.FCITX_PRESS_KEY :
                FcitxKeyEventType.FCITX_RELEASE_KEY;
            if (_context is not null)
                return await _context.ProcessKeyEventAsync((uint)keyVal, (uint)keyCode, (uint)state, (int)type,
                    (uint)args.Timestamp).ConfigureAwait(false);

            return false;
        }

        public override void SetOptions(TextInputOptions options) =>
            Enqueue(async () =>
            {
                if(_context == null)
                    return;
                FcitxCapabilityFlags flags = default;
                if (options.Lowercase)
                    flags |= FcitxCapabilityFlags.CAPACITY_LOWERCASE;
                if (options.Uppercase)
                    flags |= FcitxCapabilityFlags.CAPACITY_UPPERCASE;
                if (!options.AutoCapitalization)
                    flags |= FcitxCapabilityFlags.CAPACITY_NOAUTOUPPERCASE;
                if (options.ContentType == TextInputContentType.Email)
                    flags |= FcitxCapabilityFlags.CAPACITY_EMAIL;
                else if (options.ContentType == TextInputContentType.Number)
                    flags |= FcitxCapabilityFlags.CAPACITY_NUMBER;
                else if (options.ContentType == TextInputContentType.Password)
                    flags |= FcitxCapabilityFlags.CAPACITY_PASSWORD;
                else if (options.ContentType == TextInputContentType.Digits)
                    flags |= FcitxCapabilityFlags.CAPACITY_DIALABLE;
                else if (options.ContentType == TextInputContentType.Url)
                    flags |= FcitxCapabilityFlags.CAPACITY_URL;
                if (flags != _lastReportedFlags)
                {
                    _lastReportedFlags = flags;
                    await _context.SetCapacityAsync((uint)flags);
                }
            });

        private void OnForward(Exception? e, (uint keyval, uint state, int type) ev)
        {
            var state = (FcitxKeyState)ev.state;
            KeyModifiers mods = default;
            if (state.HasAllFlags(FcitxKeyState.FcitxKeyState_Ctrl))
                mods |= KeyModifiers.Control;
            if (state.HasAllFlags(FcitxKeyState.FcitxKeyState_Alt))
                mods |= KeyModifiers.Alt;
            if (state.HasAllFlags(FcitxKeyState.FcitxKeyState_Shift))
                mods |= KeyModifiers.Shift;
            if (state.HasAllFlags(FcitxKeyState.FcitxKeyState_Super))
                mods |= KeyModifiers.Meta;
            FireForward(new X11InputMethodForwardedKey
            {
                Modifiers = mods,
                KeyVal = (int)ev.keyval,
                Type = ev.type == (int)FcitxKeyEventType.FCITX_PRESS_KEY ?
                    RawKeyEventType.KeyDown :
                    RawKeyEventType.KeyUp
            });
        }

        private void OnCommitString(Exception? e, string s)
        {
            if (e is not null)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.FreeDesktopPlatform)?.Log(this, $"OnCommitString failed: {e}");
                return;
            }

            FireCommit(s);
        }
    }
}
