using System;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Controls.Platform;

namespace Avalonia.Browser;

internal class BrowserInputPane : InputPaneBase
{
    public BrowserInputPane(JSObject container)
    {
        InputHelper.SubscribeKeyboardGeometryChange(container, OnGeometryChange);
    }

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
            OnStateChanged(new InputPaneStateEventArgs(State, null, OccludedRect));
        }

        return true;
    }
}
