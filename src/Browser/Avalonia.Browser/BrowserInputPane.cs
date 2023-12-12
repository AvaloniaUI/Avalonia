using System;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Controls.Platform;

namespace Avalonia.Browser;

internal class BrowserInputPane : IInputPane
{
    public BrowserInputPane(JSObject container)
    {
        InputHelper.SubscribeKeyboardGeometryChange(container, OnGeometryChange);
    }

    public InputPaneState State { get; private set; }
    public Rect OccludedRect { get; private set; }
    public event EventHandler<InputPaneStateEventArgs>? StateChanged;
    
    private bool OnGeometryChange(JSObject args)
    {
        var oldState = (OccludedRect, State);

        OccludedRect = new Rect(
            args.GetPropertyAsDouble("x"),
            args.GetPropertyAsDouble("y"),
            args.GetPropertyAsDouble("width"),
            args.GetPropertyAsDouble("height"));
        State = OccludedRect.Width != 0 ? InputPaneState.Open : InputPaneState.Closed;

        if (oldState != (OccludedRect, State))
        {
            StateChanged?.Invoke(this, new InputPaneStateEventArgs(State, null, OccludedRect));
        }

        return true;
    }
}
