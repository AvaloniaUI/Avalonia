using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Logging;
using Tmds.DBus;

namespace Avalonia.FreeDesktop.DBusIme
{
    internal class DBusInputMethodFactory<T> : IX11InputMethodFactory where T : ITextInputMethodImpl, IX11InputMethodControl
    {
        private readonly Func<IntPtr, T> _factory;

        public DBusInputMethodFactory(Func<IntPtr, T> factory)
        {
            _factory = factory;
        }

        public (ITextInputMethodImpl method, IX11InputMethodControl control) CreateClient(IntPtr xid)
        {
            var im = _factory(xid);
            return (im, im);
        }
    }

    internal abstract class DBusTextInputMethodBase : IX11InputMethodControl, ITextInputMethodImpl
    {
        private List<IDisposable> _disposables = new List<IDisposable>();
        private Queue<string> _onlineNamesQueue = new Queue<string>();
        protected Connection Connection { get; }
        private readonly string[] _knownNames;
        private bool _connecting;
        private string? _currentName;
        private DBusCallQueue _queue;
        private bool _controlActive, _windowActive;
        private bool? _imeActive;
        private Rect _logicalRect;
        private PixelRect? _lastReportedRect;
        private double _scaling = 1;
        private PixelPoint _windowPosition;
        private ITextInputMethodClient? _client;

        protected bool IsConnected => _currentName != null;

        public DBusTextInputMethodBase(Connection connection, params string[] knownNames)
        {
            _queue = new DBusCallQueue(QueueOnError);
            Connection = connection;
            _knownNames = knownNames;
            Watch();
        }

        public ITextInputMethodClient Client => _client;

        public bool IsActive => _client != null;

        async void Watch()
        {
            foreach (var name in _knownNames)
                _disposables.Add(await Connection.ResolveServiceOwnerAsync(name, OnNameChange));
        }

        protected abstract Task<bool> Connect(string name);

        protected string GetAppName() =>
            Application.Current?.Name ?? Assembly.GetEntryAssembly()?.GetName()?.Name ?? "Avalonia";

        private async void OnNameChange(ServiceOwnerChangedEventArgs args)
        {
            if (args.NewOwner != null && _currentName == null)
            {
                _onlineNamesQueue.Enqueue(args.ServiceName);
                if (!_connecting)
                {
                    _connecting = true;
                    try
                    {
                        while (_onlineNamesQueue.Count > 0)
                        {
                            var name = _onlineNamesQueue.Dequeue();
                            try
                            {
                                if (await Connect(name))
                                {
                                    _onlineNamesQueue.Clear();
                                    _currentName = name;
                                    return;
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.TryGet(LogEventLevel.Error, "IME")
                                    ?.Log(this, "Unable to create IME input context:\n" + e);
                            }
                        }
                    }
                    finally
                    {
                        _connecting = false;
                    }
                }

            }

            // IME has crashed
            if (args.NewOwner == null && args.ServiceName == _currentName)
            {
                _currentName = null;
                foreach (var s in _disposables)
                    s.Dispose();
                _disposables.Clear();

                OnDisconnected();
                Reset();

                // Watch again
                Watch();
            }
        }

        protected virtual Task Disconnect()
        {
            return Task.CompletedTask;
        }

        protected virtual void OnDisconnected()
        {

        }

        protected virtual void Reset()
        {
            _lastReportedRect = null;
            _imeActive = null;
        }

        async Task QueueOnError(Exception e)
        {
            Logger.TryGet(LogEventLevel.Error, "IME")
                ?.Log(this, "Error:\n" + e);
            try
            {
                await Disconnect();
            }
            catch (Exception ex)
            {
                Logger.TryGet(LogEventLevel.Error, "IME")
                    ?.Log(this, "Error while destroying the context:\n" + ex);
            }
            OnDisconnected();
            _currentName = null;
        }

        protected void Enqueue(Func<Task> cb) => _queue.Enqueue(cb);

        protected void AddDisposable(IDisposable? d)
        {
            if(d is { })
                _disposables.Add(d);
        }
        
        public void Dispose()
        {
            foreach(var d in _disposables)
                d.Dispose();
            _disposables.Clear();
            try
            {
                Disconnect().ContinueWith(_ => { });
            }
            catch
            {
                // fire and forget
            }
            _currentName = null;
        }

        protected abstract Task SetCursorRectCore(PixelRect rect);
        protected abstract Task SetActiveCore(bool active);
        protected abstract Task ResetContextCore();
        protected abstract Task<bool> HandleKeyCore(RawKeyEventArgs args, int keyVal, int keyCode);

        void UpdateActive()
        {
            _queue.Enqueue(async () =>
            {
                if(!IsConnected)
                    return;
                
                var active = _windowActive && _controlActive;
                if (active != _imeActive)
                {
                    _imeActive = active;
                    await SetActiveCore(active);
                }
            });
        }


        void IX11InputMethodControl.SetWindowActive(bool active)
        {
            _windowActive = active;
            UpdateActive();
        }
        
        void ITextInputMethodImpl.SetClient(ITextInputMethodClient? client)
        {
            _client = client;
            UpdateActive();
        }

        bool IX11InputMethodControl.IsEnabled => IsConnected && _imeActive == true;

        async ValueTask<bool> IX11InputMethodControl.HandleEventAsync(RawKeyEventArgs args, int keyVal, int keyCode)
        {
            try
            {
                return await _queue.EnqueueAsync(async () => await HandleKeyCore(args, keyVal, keyCode));
            }
            // Disconnected
            catch (OperationCanceledException)
            {
                return false;
            }
            // Error, disconnect
            catch (Exception e)
            {
                await QueueOnError(e);
                return false;
            }
        }

        private Action<string>? _onCommit;
        event Action<string> IX11InputMethodControl.Commit
        {
            add => _onCommit += value;
            remove => _onCommit -= value;
        }

        protected void FireCommit(string s) => _onCommit?.Invoke(s);
        
        private Action<X11InputMethodForwardedKey>? _onForward;
        event Action<X11InputMethodForwardedKey> IX11InputMethodControl.ForwardKey
        {
            add => _onForward += value;
            remove => _onForward -= value;
        }

        protected void FireForward(X11InputMethodForwardedKey k) => _onForward?.Invoke(k);
        
        void UpdateCursorRect()
        {
            _queue.Enqueue(async () =>
            {
                if(!IsConnected)
                    return;
                var cursorRect = PixelRect.FromRect(_logicalRect, _scaling);
                cursorRect = cursorRect.Translate(_windowPosition);
                if (cursorRect != _lastReportedRect)
                {
                    _lastReportedRect = cursorRect;
                    await SetCursorRectCore(cursorRect);
                }
            });
        }
        
        void IX11InputMethodControl.UpdateWindowInfo(PixelPoint position, double scaling)
        {
            _windowPosition = position;
            _scaling = scaling;
            UpdateCursorRect();
        }

        void ITextInputMethodImpl.SetCursorRect(Rect rect)
        {
            _logicalRect = rect;
            UpdateCursorRect();
        }

        public abstract void SetOptions(TextInputOptions options);

        void ITextInputMethodImpl.Reset()
        {
            Reset();
            _queue.Enqueue(async () =>
            {
                if (!IsConnected)
                    return;
                await ResetContextCore();
            });
        }
    }
}
