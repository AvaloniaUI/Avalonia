using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;

using Avalonia.Controls.Platform;
using Avalonia.Platform;

namespace Avalonia.Browser;

public class JSObjectPlatformHandle : PlatformHandle
{
    internal const string ElementReferenceDescriptor = "JSObject";

    public JSObjectPlatformHandle(JSObject reference) : base(GetJsHandle(reference), ElementReferenceDescriptor)
    {
        Object = reference;
    }

    public JSObject Object { get; }

    // https://github.com/dotnet/runtime/blob/v8.0.7/src/libraries/System.Runtime.InteropServices.JavaScript/src/System/Runtime/InteropServices/JavaScript/JSObject.References.cs#L12
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "JSHandle")]
    internal static extern ref nint GetJsHandle(JSObject @this);
} 

public class JSObjectControlHandle : JSObjectPlatformHandle, INativeControlHostDestroyableControlHandle
{
    public JSObjectControlHandle(JSObject reference) : base(reference)
    {
    }

    public void Destroy()
    {
        if (Object is { } inProcess && !inProcess.IsDisposed)
        {
            inProcess.Dispose();
        }
    }
}
