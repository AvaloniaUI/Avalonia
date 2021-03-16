using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Tmds.DBus;

namespace Avalonia.FreeDesktop.DBusIme.Fcitx
{
    internal class FcitxX11TextInputMethod : DBusTextInputMethodBase
    {
        private FcitxICWrapper _context;
        private FcitxCapabilityFlags? _lastReportedFlags;

        public FcitxX11TextInputMethod(Connection connection) : base(connection,
            "org.fcitx.Fcitx",
            "org.freedesktop.portal.Fcitx"
            )
        {

        }

        protected override async Task<bool> Connect(string name)
        {
            if (name == "org.fcitx.Fcitx")
            {
                var method = Connection.CreateProxy<IFcitxInputMethod>(name, "/inputmethod");
                var resp = await method.CreateICv3Async(GetAppName(),
                    Process.GetCurrentProcess().Id);

                var proxy = Connection.CreateProxy<IFcitxInputContext>(name,
                    "/inputcontext_" + resp.icid);

                _context = new FcitxICWrapper(proxy);
            }
            else
            {
                var method = Connection.CreateProxy<IFcitxInputMethod1>(name, "/inputmethod");
                var resp = await method.CreateInputContextAsync(new[] { ("appName", GetAppName()) });
                var proxy = Connection.CreateProxy<IFcitxInputContext1>(name, resp.path);
                _context = new FcitxICWrapper(proxy);
            }

            AddDisposable(await _context.WatchCommitStringAsync(OnCommitString));
            AddDisposable(await _context.WatchForwardKeyAsync(OnForward));
            return true;
        }

        protected override Task Disconnect() => _context.DestroyICAsync();

        protected override void OnDisconnected() => _context = null;

        protected override void Reset()
        {
            _lastReportedFlags = null;
            base.Reset();
        }

        protected override Task SetCursorRectCore(PixelRect cursorRect) =>
            _context.SetCursorRectAsync(cursorRect.X, cursorRect.Y, Math.Max(1, cursorRect.Width),
                Math.Max(1, cursorRect.Height));

        protected override Task SetActiveCore(bool active)
        {
            if (active)
                return _context.FocusInAsync();
            else
                return _context.FocusOutAsync();
        }

        protected override Task ResetContextCore() => _context.ResetAsync();

        protected override async Task<bool> HandleKeyCore(RawKeyEventArgs args, int keyVal, int keyCode)
        {
            FcitxKeyState state = default;
            if (args.Modifiers.HasFlagCustom(RawInputModifiers.Control))
                state |= FcitxKeyState.FcitxKeyState_Ctrl;
            if (args.Modifiers.HasFlagCustom(RawInputModifiers.Alt))
                state |= FcitxKeyState.FcitxKeyState_Alt;
            if (args.Modifiers.HasFlagCustom(RawInputModifiers.Shift))
                state |= FcitxKeyState.FcitxKeyState_Shift;
            if (args.Modifiers.HasFlagCustom(RawInputModifiers.Meta))
                state |= FcitxKeyState.FcitxKeyState_Super;

            var type = args.Type == RawKeyEventType.KeyDown ?
                FcitxKeyEventType.FCITX_PRESS_KEY :
                FcitxKeyEventType.FCITX_RELEASE_KEY;
            
            return await _context.ProcessKeyEventAsync((uint)keyVal, (uint)keyCode, (uint)state, (int)type,
                (uint)args.Timestamp).ConfigureAwait(false);
        }
        
        public override void SetOptions(TextInputOptionsQueryEventArgs options) =>
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
                else if (options.ContentType == TextInputContentType.Phone)
                    flags |= FcitxCapabilityFlags.CAPACITY_DIALABLE;
                else if (options.ContentType == TextInputContentType.Url)
                    flags |= FcitxCapabilityFlags.CAPACITY_URL;
                if (flags != _lastReportedFlags)
                {
                    _lastReportedFlags = flags;
                    await _context.SetCapacityAsync((uint)flags);
                }
            });

        private void OnForward((uint keyval, uint state, int type) ev)
        {
            var state = (FcitxKeyState)ev.state;
            KeyModifiers mods = default;
            if (state.HasFlagCustom(FcitxKeyState.FcitxKeyState_Ctrl))
                mods |= KeyModifiers.Control;
            if (state.HasFlagCustom(FcitxKeyState.FcitxKeyState_Alt))
                mods |= KeyModifiers.Alt;
            if (state.HasFlagCustom(FcitxKeyState.FcitxKeyState_Shift))
                mods |= KeyModifiers.Shift;
            if (state.HasFlagCustom(FcitxKeyState.FcitxKeyState_Super))
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

        private void OnCommitString(string s) => FireCommit(s);
    }
}
