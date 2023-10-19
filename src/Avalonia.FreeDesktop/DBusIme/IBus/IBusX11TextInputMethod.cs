using System;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Logging;
using Avalonia.Media.TextFormatting.Unicode;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;


namespace Avalonia.FreeDesktop.DBusIme.IBus
{
    internal class IBusX11TextInputMethod : DBusTextInputMethodBase
    {
        private OrgFreedesktopIBusService? _service;
        private OrgFreedesktopIBusInputContext? _context;
        private string? _preeditText;
        private int _preeditCursor;
        private bool _preeditShown = true;
        private int _insideReset = 0;

        public IBusX11TextInputMethod(Connection connection) : base(connection, "org.freedesktop.portal.IBus") { }

        protected override async Task<bool> Connect(string name)
        {
            var portal = new OrgFreedesktopIBusPortal(Connection, name, "/org/freedesktop/IBus");
            var path = await portal.CreateInputContextAsync(GetAppName());
            _service = new OrgFreedesktopIBusService(Connection, name, path);
            _context = new OrgFreedesktopIBusInputContext(Connection, name, path);
            AddDisposable(await _context.WatchCommitTextAsync(OnCommitText));
            AddDisposable(await _context.WatchForwardKeyEventAsync(OnForwardKey));
            AddDisposable(await _context.WatchUpdatePreeditTextAsync(OnUpdatePreedit));
            AddDisposable(await _context.WatchShowPreeditTextAsync(OnShowPreedit));
            AddDisposable(await _context.WatchHidePreeditTextAsync(OnHidePreedit));
            Enqueue(() => _context.SetCapabilitiesAsync((uint)IBusCapability.CapFocus));
            return true;
        }

        private void OnHidePreedit(Exception? obj)
        {
            _preeditShown = false;
            if (Client?.SupportsPreedit == true)
                Client.SetPreeditText(null, null);
        }

        private void OnShowPreedit(Exception? obj)
        {
            _preeditShown = true;
            if (Client?.SupportsPreedit == true)
                Client.SetPreeditText(_preeditText, _preeditText == null ? null : _preeditCursor);
        }

        private void OnUpdatePreedit(Exception? arg1, (DBusVariantItem text, uint cursor_pos, bool visible) preeditComponents)
        {
            if (preeditComponents.text is { Value: DBusStructItem { Count: >= 3 } structItem } &&
                structItem[2] is DBusStringItem stringItem)
            {
                _preeditText = stringItem.Value;
                _preeditCursor = _preeditText != null
                    ? Utf16Utils.CharacterOffsetToStringOffset(_preeditText,
                        (int)Math.Min(preeditComponents.cursor_pos, int.MaxValue), false)
                    : 0;
                
                _preeditShown = true;
            }
            else
            {
                _preeditText = null;
                _preeditShown = false;
                _preeditCursor = 0;
            }

            if (Client?.SupportsPreedit == true)
                Client.SetPreeditText(
                    _preeditShown ? _preeditText : null, _preeditCursor);
        }

        private void OnForwardKey(Exception? e, (uint keyval, uint keycode, uint state) k)
        {
            if (e is not null)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.FreeDesktopPlatform)?.Log(this, $"OnForwardKey failed: {e}");
                return;
            }

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

        private void OnCommitText(Exception? e, DBusVariantItem variantItem)
        {
            if (_insideReset > 0) 
            {
                // For some reason iBus can trigger a CommitText while being reset.
                // Thankfully the signal is sent _during_ Reset call processing,
                // so it arrives on-the-wire before Reset call result, so we can 
                // check if we have any pending Reset calls and ignore the signal here
                return;
            }
            if (e is not null)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.FreeDesktopPlatform)?.Log(this, $"OnCommitText failed: {e}");
                return;
            }

            if (variantItem.Value is DBusStructItem { Count: >= 3 } structItem && structItem[2] is DBusStringItem stringItem)
                FireCommit(stringItem.Value);
        }

        protected override Task DisconnectAsync() => _service?.DestroyAsync() ?? Task.CompletedTask;

        protected override void OnDisconnected()
        {
            _service = null;
            _context = null;
            base.OnDisconnected();
        }

        protected override Task SetCursorRectCore(PixelRect rect)
            => _context?.SetCursorLocationAsync(rect.X, rect.Y, rect.Width, rect.Height)
            ?? Task.CompletedTask;

        protected override Task SetActiveCore(bool active)
            => (active ? _context?.FocusInAsync() : _context?.FocusOutAsync())
                ?? Task.CompletedTask;

        protected override async Task ResetContextCore()
        {
            _preeditShown = true;
            if (_context == null)
                return;
            if (_context == null)
                return;

            try
            {
                _insideReset++;
                await _context.ResetAsync();
            }
            finally
            {
                _insideReset--;
            }
        }

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

            return _context is not null ? _context.ProcessKeyEventAsync((uint)keyVal, (uint)keyCode, (uint)state) : Task.FromResult(false);
        }

        public override void SetOptions(TextInputOptions options)
        {
            // No-op, because ibus
        }

        protected override async Task SetCapabilitiesCore(bool supportsPreedit, bool supportsSurroundingText)
        {
            var caps = IBusCapability.CapFocus;
            if (supportsPreedit)
                caps |= IBusCapability.CapPreeditText;
            if (_context != null)
                await _context.SetCapabilitiesAsync((uint)caps);
        }
    }
}
