using System;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Controls.Platform;

namespace Avalonia.Browser;

internal class BrowserInputPane : InputPaneBase
{
    public bool OnGeometryChange(double x, double y, double width, double height)
    {
        var oldState = (OccludedRect, State);

        OccludedRect = new Rect(x, y, width, height);
        State = OccludedRect.Width != 0 ? InputPaneState.Open : InputPaneState.Closed;

        if (oldState != (OccludedRect, State))
        {
            OnStateChanged(new InputPaneStateEventArgs(State, null, OccludedRect));
        }

        return true;
    }
}
