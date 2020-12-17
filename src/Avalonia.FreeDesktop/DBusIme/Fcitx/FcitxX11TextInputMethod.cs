using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Logging;
using Tmds.DBus;

namespace Avalonia.FreeDesktop.DBusIme.Fcitx
{
    internal class FcitxIx11TextInputMethodFactory : IX11InputMethodFactory
    {
        private readonly Connection _connection;

        public FcitxIx11TextInputMethodFactory(Connection connection)
        {
            _connection = connection;
        }
        
        public (ITextInputMethodImpl method, IX11InputMethodControl control) CreateClient(IntPtr xid)
        {
            var cl = new FcitxTextInputMethod(xid, _connection);
            return (cl, cl);
        }
    }


    internal class FcitxTextInputMethod : ITextInputMethodImpl, IX11InputMethodControl
    {
        private readonly IntPtr _xid;
        private readonly Connection _connection;
        private IFcitxInputContext _context;
        private bool _connecting;
        private string _currentName;
        private DBusCallQueue _queue = new DBusCallQueue();
        private bool _controlActive, _windowActive, _imeActive;
        private Rect _logicalRect;
        private double _scaling = 1;
        private PixelPoint _windowPosition;
        private bool _disposed;
        private PixelRect? _lastReportedRect;
        private FcitxCapabilityFlags _lastReportedFlags;
        
        private List<IDisposable> _disposables = new List<IDisposable>();
        private List<IDisposable> _subscriptions = new List<IDisposable>();
        public FcitxTextInputMethod(IntPtr xid, Connection connection)
        {
            _xid = xid;
            _connection = connection;
            _disposables.Add(_connection.ResolveServiceOwnerAsync("org.fcitx.Fcitx", OnNameChange));
        }

        private async void OnNameChange(ServiceOwnerChangedEventArgs args)
        {
            if (args.NewOwner != null && _context == null && !_connecting)
            {
                _connecting = true;
                try
                {
                    var method = _connection.CreateProxy<IFcitxInputMethod>(args.ServiceName, "/inputmethod");
                    var resp = await method.CreateICv3Async(
                        Application.Current.Name ?? Assembly.GetEntryAssembly()?.GetName()?.Name ?? "Avalonia",
                        Process.GetCurrentProcess().Id);

                    _context = _connection.CreateProxy<IFcitxInputContext>(args.ServiceName,
                        "/inputcontext_" + resp.icid);
                    _currentName = args.ServiceName;
                    _imeActive = false;
                    _lastReportedRect = null;
                    _lastReportedFlags = default;
                    _subscriptions.Add(await _context.WatchCommitStringAsync(OnCommitString));
                    _subscriptions.Add(await _context.WatchForwardKeyAsync(OnForward));
                    UpdateActive();
                    UpdateCursorRect();
                    
                }
                catch(Exception e)
                {
                    Logger.TryGet(LogEventLevel.Error, "FCITX")
                        ?.Log(this, "Unable to create fcitx input context:\n" + e);
                }
                finally
                {
                    _connecting = false;
                }

            }

            // fcitx has crashed
            if (args.NewOwner == null && args.ServiceName == _currentName)
            {
                _context = null;
                _currentName = null;
                _imeActive = false;
                foreach(var s in _subscriptions)
                    s.Dispose();
                _subscriptions.Clear();
            }
        }

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
            _onForward?.Invoke(new X11InputMethodForwardedKey
            {
                Modifiers = mods,
                KeyVal = (int)ev.keyval,
                Type = ev.type == (int)FcitxKeyEventType.FCITX_PRESS_KEY ?
                    RawKeyEventType.KeyDown :
                    RawKeyEventType.KeyUp
            });
        }

        private void OnCommitString(string s) => _onCommit?.Invoke(s);

        async Task OnError(Exception e)
        {
            Logger.TryGet(LogEventLevel.Error, "FCITX")
                ?.Log(this, "Error:\n" + e);
            try
            {
                await _context.DestroyICAsync();
            }
            catch (Exception ex)
            {
                Logger.TryGet(LogEventLevel.Error, "FCITX")
                    ?.Log(this, "Error while destroying the context:\n" + ex);
            }

            _context = null;
            _currentName = null;
            _imeActive = false;
        }

        void UpdateActive()
        {
            _queue.Enqueue(async () =>
            {
                if(_context == null)
                    return;
                
                var active = _windowActive && _controlActive;
                if (active != _imeActive)
                {
                    _imeActive = active;
                    if (_imeActive)
                        await _context.FocusInAsync();
                    else
                        await _context.FocusOutAsync();
                }
            }, OnError);
        }

        void UpdateCursorRect()
        {
            _queue.Enqueue(async () =>
            {
                if(_context == null)
                    return;
                var cursorRect = PixelRect.FromRect(_logicalRect, _scaling);
                cursorRect = cursorRect.Translate(_windowPosition);
                if (cursorRect != _lastReportedRect)
                {
                    _lastReportedRect = cursorRect;
                    _context?.SetCursorRectAsync(cursorRect.X, cursorRect.Y, Math.Max(1, cursorRect.Width),
                        Math.Max(1, cursorRect.Height));
                }
            }, OnError);
        }
        
        public void SetOptions(TextInputOptionsQueryEventArgs options)
        {
            _queue.Enqueue(async () =>
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
            }, OnError);
        }
        
        public void SetActive(bool active)
        {
            _controlActive = active;
            UpdateActive();
        }
        
        void IX11InputMethodControl.SetWindowActive(bool active)
        {
            _windowActive = active;
            UpdateActive();
        }

        bool IX11InputMethodControl.IsEnabled => _context != null && _controlActive;

        Task<bool> IX11InputMethodControl.HandleEventAsync(RawKeyEventArgs args, int keyVal, int keyCode)
        {
            return _queue.EnqueueAsync<bool>(async () =>
            {
                if (_context == null)
                    return false;
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

                try
                {
                    return await _context.ProcessKeyEventAsync((uint)keyVal, (uint)keyCode, (uint)state, (int)type,
                        (uint)args.Timestamp) != 0;
                }
                catch (Exception e)
                {
                    await OnError(e);
                    return false;
                }
            });
        }

        private Action<string> _onCommit;
        event Action<string> IX11InputMethodControl.OnCommit
        {
            add => _onCommit += value;
            remove => _onCommit -= value;
        }
        
        private Action<X11InputMethodForwardedKey> _onForward;
        event Action<X11InputMethodForwardedKey> IX11InputMethodControl.OnForwardKey
        {
            add => _onForward += value;
            remove => _onForward -= value;
        }

        public void UpdateWindowInfo(PixelPoint position, double scaling)
        {
            _windowPosition = position;
            _scaling = scaling;
            UpdateCursorRect();
        }

        public void SetCursorRect(Rect rect)
        {
            _logicalRect = rect;
            UpdateCursorRect();
        }


        void IDisposable.Dispose()
        {
            _disposed = true;
            foreach(var d in _disposables)
                d.Dispose();
            _disposables.Clear();
            
            foreach(var s in _subscriptions)
                s.Dispose();
            _subscriptions.Clear();
            
            // fire and forget
            _context?.DestroyICAsync().ContinueWith(_ => { });
            _context = null;
            _currentName = null;
        }
    }
    
}
