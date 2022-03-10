using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Tmds.DBus;

namespace Avalonia.FreeDesktop.DBusIme.IBus
{
    internal class IBusX11TextInputMethod : DBusTextInputMethodBase
    {
        private IIBusInputContext _context;

        public IBusX11TextInputMethod(Connection connection) : base(connection, 
            "org.freedesktop.portal.IBus")
        {
        }

        protected override async Task<bool> Connect(string name)
        {
            var path =
                await Connection.CreateProxy<IIBusPortal>(name, "/org/freedesktop/IBus")
                    .CreateInputContextAsync(GetAppName());

            _context = Connection.CreateProxy<IIBusInputContext>(name, path);
            AddDisposable(await _context.WatchCommitTextAsync(OnCommitText));
            AddDisposable(await _context.WatchForwardKeyEventAsync(OnForwardKey));
            Enqueue(() => _context.SetCapabilitiesAsync((uint)IBusCapability.CapFocus));
            return true;
        }

        private void OnForwardKey((uint keyval, uint keycode, uint state) k)
        {
            var state = (IBusModifierMask)k.state;
            KeyModifiers mods = default;
            if (state.HasAllFlags(IBusModifierMask.ControlMask))
                mods |= KeyModifiers.Control;
            if (state.HasAllFlags(IBusModifierMask.Mod1Mask))
                mods |= KeyModifiers.Alt;
            if (state.HasAllFlags(IBusModifierMask.ShiftMask))
                mods |= KeyModifiers.Shift;
            if (state.HasAllFlags(IBusModifierMask.Mod4Mask))
                mods |= KeyModifiers.Meta;
            FireForward(new X11InputMethodForwardedKey
            {
                KeyVal = (int)k.keyval,
                Type = state.HasAllFlags(IBusModifierMask.ReleaseMask) ? RawKeyEventType.KeyUp : RawKeyEventType.KeyDown,
                Modifiers = mods
            });
        }

        
        private void OnCommitText(object wtf)
        {
            // Hello darkness, my old friend
            var prop = wtf.GetType().GetField("Item3");
            if (prop != null)
            {
                var text = (string)prop.GetValue(wtf);
                if (!string.IsNullOrEmpty(text))
                    FireCommit(text);
            }
        }

        protected override Task Disconnect() => _context.DestroyAsync();

        protected override void OnDisconnected()
        {
            _context = null;
            base.OnDisconnected();
        }

        protected override Task SetCursorRectCore(PixelRect rect) 
            => _context.SetCursorLocationAsync(rect.X, rect.Y, rect.Width, rect.Height);

        protected override Task SetActiveCore(bool active)
            => active ? _context.FocusInAsync() : _context.FocusOutAsync();

        protected override Task ResetContextCore()
            => _context.ResetAsync();

        protected override Task<bool> HandleKeyCore(RawKeyEventArgs args, int keyVal, int keyCode)
        {
            IBusModifierMask state = default;
            if (args.Modifiers.HasAllFlags(RawInputModifiers.Control))
                state |= IBusModifierMask.ControlMask;
            if (args.Modifiers.HasAllFlags(RawInputModifiers.Alt))
                state |= IBusModifierMask.Mod1Mask;
            if (args.Modifiers.HasAllFlags(RawInputModifiers.Shift))
                state |= IBusModifierMask.ShiftMask;
            if (args.Modifiers.HasAllFlags(RawInputModifiers.Meta))
                state |= IBusModifierMask.Mod4Mask;

            if (args.Type == RawKeyEventType.KeyUp)
                state |= IBusModifierMask.ReleaseMask;

            return _context.ProcessKeyEventAsync((uint)keyVal, (uint)keyCode, (uint)state);
        }

        public override void SetOptions(TextInputOptionsQueryEventArgs options)
        {
            // No-op, because ibus 
        }
    }
}
