using System;
using System.Runtime.InteropServices.JavaScript;

using Avalonia.Controls.Platform;

namespace Avalonia.Browser;

public class JSObjectControlHandle : INativeControlHostDestroyableControlHandle
{
    internal const string ElementReferenceDescriptor = "JSObject";

    public JSObjectControlHandle(JSObject reference)
    {
        Object = reference;
    }

    public JSObject Object { get; }

    public IntPtr Handle => throw new NotSupportedException();

    public string? HandleDescriptor => ElementReferenceDescriptor;

    public void Destroy()
    {
        if (Object is JSObject inProcess && !inProcess.IsDisposed)
        {
            inProcess.Dispose();
        }
    }
}
