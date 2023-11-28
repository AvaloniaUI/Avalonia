using System;
using Avalonia.Controls.Platform;
using Avalonia.MicroCom;
using Avalonia.Win32.Interop;
using Avalonia.Win32.Win32Com;

namespace Avalonia.Win32.Input;

internal unsafe class WindowsInputPane : IInputPane, IDisposable
{
    private static Guid CLSID_FrameworkInputPane = Guid.Parse("D5120AA3-46BA-44C5-822D-CA8092C1FC72");
    private static Guid SID_IFrameworkInputPane  = Guid.Parse("5752238B-24F0-495A-82F1-2FD593056796");

    private readonly WindowImpl _windowImpl;
    private readonly IFrameworkInputPane _inputPane;
    private readonly uint _cookie;

    public WindowsInputPane(WindowImpl windowImpl)
    {
        _windowImpl = windowImpl;
        _inputPane = UnmanagedMethods.CreateInstance<IFrameworkInputPane>(ref CLSID_FrameworkInputPane, ref SID_IFrameworkInputPane);

        using (var handler = new Handler(this))
        {
            uint cookie = 0;
            _inputPane.AdviseWithHWND(windowImpl.Handle.Handle, handler, &cookie);
            _cookie = cookie;
        }
    }
    public InputPaneState State { get; private set; }

    public Rect OccludedRect { get; private set; }

    public event EventHandler<InputPaneStateEventArgs>? StateChanged;

    private void OnStateChanged(bool showing, UnmanagedMethods.RECT? prcInputPaneScreenLocation)
    {
        var oldState = (OccludedRect, State);
        OccludedRect = prcInputPaneScreenLocation.HasValue
            ? ScreenRectToClient(prcInputPaneScreenLocation.Value)
            : default;
        State = showing ? InputPaneState.Open : InputPaneState.Closed;

        if (oldState != (OccludedRect, State))
        {
            StateChanged?.Invoke(this, new InputPaneStateEventArgs(State, null, OccludedRect));
        }
    }

    private Rect ScreenRectToClient(UnmanagedMethods.RECT screenRect)
    {
        var position = new PixelPoint(screenRect.left, screenRect.top);
        var size = new PixelSize(screenRect.Width, screenRect.Height);
        return new Rect(_windowImpl.PointToClient(position), size.ToSize(_windowImpl.DesktopScaling));
    }

    public void Dispose()
    {
        if (_cookie != 0)
        {
            _inputPane.Unadvise(_cookie);
        }

        _inputPane.Dispose();
    }

    private class Handler : CallbackBase, IFrameworkInputPaneHandler
    {
        private readonly WindowsInputPane _pane;

        public Handler(WindowsInputPane pane) => _pane = pane;
        public void Showing(UnmanagedMethods.RECT* rect, int _) => _pane.OnStateChanged(true, *rect);
        public void Hiding(int fEnsureFocusedElementInView) => _pane.OnStateChanged(false, null);
    }
}
