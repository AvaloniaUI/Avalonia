using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using RemoteViewing.Vnc.Server;
using RemoteViewing.Vnc;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace Avalonia.Headless.Vnc;
internal class HeadlessVncConnectionManager
{
    readonly TcpListener _tcpListener;
    readonly ShutdownMode _shutdownMode;
    readonly ConcurrentDictionary<string, HeadlessVncFramebufferSource> _vncFrameBuffers = new();
    readonly Stack<Window> _displayWindows;

    IClassicDesktopStyleApplicationLifetime? _appLifetime;
    Window? _currentWindow;

    Window DisplayWindow => _currentWindow ?? _appLifetime?.MainWindow
        ?? throw new InvalidOperationException("MainWindow wasn't initialized");

    public Size? ClientSize { get; private set; }
    public bool ResizeSessionIfContentSizeChanges { get; }

    public HeadlessVncConnectionManager(AppBuilder appBuilder, string host, int port, ShutdownMode shutdownMode,
        bool resizeSessionIfContentSizeChanges)
    {
        _tcpListener = new(host == null ? IPAddress.Loopback : IPAddress.Parse(host), port);
        _shutdownMode = shutdownMode;
        _displayWindows = new();
        ResizeSessionIfContentSizeChanges = resizeSessionIfContentSizeChanges;

        appBuilder.AfterSetup(_ =>
        {
            _appLifetime = (IClassicDesktopStyleApplicationLifetime)appBuilder.Instance!.ApplicationLifetime!;
            _appLifetime!.Startup += AppStartup;
        });
    }

    private async void AppStartup(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
    {
        //only start listening once the window has initialised so we know it's client size
        while (_appLifetime?.MainWindow == null || !_appLifetime.MainWindow.IsActive)
        {
            await Task.Delay(100);
            Dispatcher.UIThread.RunJobs();
        }
        ClientSize = DisplayWindow.ClientSize;

        _tcpListener.Start();
        while (true)
        {
            var client = await _tcpListener.AcceptTcpClientAsync();
            var options = new VncServerSessionOptions
            {
                AuthenticationMethod = AuthenticationMethod.None
            };
            string connectionId = Guid.NewGuid().ToString();
            var session = new VncServerSession();
            var frameBuffer = new HeadlessVncFramebufferSource(session, DisplayWindow, ResizeSessionIfContentSizeChanges);
            _vncFrameBuffers.TryAdd(connectionId, frameBuffer);

            session.SetFramebufferSource(frameBuffer);
            session.Connect(client.GetStream(), options);
            session.Closed += (s, e) => SessionClosed(connectionId);
        }
    }

    internal void SetCurrentWindow(Window window)
    {
        if (window == DisplayWindow)
            return;

        _displayWindows.Push(DisplayWindow);
        _currentWindow = window;
        foreach (HeadlessVncFramebufferSource frameBuffer in _vncFrameBuffers.Values)
            frameBuffer.Window = window;
    }

    internal void WindowClosed()
    {
#if NET6_0
        _displayWindows.TryPop(out _currentWindow);
#else
            if (_displayWindows.Count == 0)
                _currentWindow = null;
            else
                _currentWindow = _displayWindows.Pop();
#endif
        foreach (HeadlessVncFramebufferSource frameBuffer in _vncFrameBuffers.Values)
            frameBuffer.Window = DisplayWindow;
    }

    internal void SessionClosed(string sessionId)
    {
        _vncFrameBuffers.TryRemove(sessionId, out _);
        if (_shutdownMode == ShutdownMode.OnLastWindowClose && _vncFrameBuffers.Count == 0)
            Dispatcher.UIThread.InvokeShutdown();
    }
}
