using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Avalonia.X11.Clipboard;

internal class EventStreamWindow : IDisposable
{
    private readonly AvaloniaX11Platform _platform;
    private IntPtr _handle;
    public IntPtr Handle => _handle;
    private readonly List<(Func<XEvent, bool> filter, TaskCompletionSource<XEvent?> tcs, TimeSpan timeout)> _listeners = new();
    // We are adding listeners to an intermediate collection to avoid freshly added listeners to be called
    // in the same event loop iteration and potentially processing an event that was not meant for them.
    private readonly List<(Func<XEvent, bool> filter, TaskCompletionSource<XEvent?> tcs, TimeSpan timeout)> _addedListeners = new();
    private readonly DispatcherTimer _timeoutTimer;
    private readonly bool _isForeign;
    private static readonly Stopwatch _time = Stopwatch.StartNew();

    public EventStreamWindow(AvaloniaX11Platform platform, IntPtr? foreignWindow = null)
    {
        _platform = platform;
        if (foreignWindow.HasValue)
        {
            _isForeign = true;
            _handle = foreignWindow.Value;
            _platform.Windows[_handle] = OnEvent;
        }
        else
            _handle = XLib.CreateEventWindow(platform, OnEvent);

        _timeoutTimer = new(TimeSpan.FromSeconds(1), DispatcherPriority.Background, OnTimer);
    }

    void MergeListeners()
    {
        _listeners.AddRange(_addedListeners);
        _addedListeners.Clear();
    }
    
    private void OnTimer(object? sender, EventArgs eventArgs)
    {
        MergeListeners();
        for (var i = 0; i < _listeners.Count; i++)
        {
            var (filter, tcs, timeout) = _listeners[i];
            if (timeout < _time.Elapsed)
            {
                _listeners.RemoveAt(i);
                i--;
                tcs.SetResult(null);
            }
        }
        if(_listeners.Count == 0)
            _timeoutTimer.Stop();
    }

    private void OnEvent(ref XEvent xev)
    {
        MergeListeners();
        for (var i = 0; i < _listeners.Count; i++)
        {
            var (filter, tcs, timeout) = _listeners[i];
            if (filter(xev))
            {
                _listeners.RemoveAt(i);
                i--;
                tcs.SetResult(xev);
            }
        }
    }

    public Task<XEvent?> WaitForEventAsync(Func<XEvent, bool> predicate, TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(5);
        
        if (timeout < TimeSpan.Zero)
            throw new TimeoutException();
        if(timeout > TimeSpan.FromDays(1))
            throw new ArgumentOutOfRangeException(nameof(timeout));
        
        var tcs = new TaskCompletionSource<XEvent?>();
        _addedListeners.Add((predicate, tcs, _time.Elapsed + timeout.Value));

        _timeoutTimer.Start();
        return tcs.Task;
    }
    
    public void Dispose()
    {
        _timeoutTimer.Stop();

        _platform.Windows.Remove(_handle);
        if (_isForeign)
            XLib.XSelectInput(_platform.Display, _handle, IntPtr.Zero);
        else
            XLib.XDestroyWindow(_platform.Display, _handle);
        
        _handle = IntPtr.Zero;
        var toDispose = _listeners.ToList();
        toDispose.AddRange(_addedListeners);
        _listeners.Clear();
        _addedListeners.Clear();
        foreach(var l in toDispose)
            l.tcs.SetResult(null);
    }
}
