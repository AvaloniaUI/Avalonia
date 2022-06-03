#nullable enable
using Avalonia.Controls.Platform;

using Microsoft.JSInterop;

namespace Avalonia.Web.Blazor
{
    public class JSObjectControlHandle : INativeControlHostDestroyableControlHandle
    {
        internal const string ElementReferenceDescriptor = "JSObjectReference";

        public JSObjectControlHandle(IJSObjectReference reference)
        {
            Object = reference;
        }

        public IJSObjectReference Object { get; }

        public IntPtr Handle => throw new NotSupportedException();

        public string? HandleDescriptor => ElementReferenceDescriptor;

        public void Destroy()
        {
            if (Object is IJSInProcessObjectReference inProcess)
            {
                inProcess.Dispose();
            }
            else
            {
                _ = Object.DisposeAsync();
            }
        }
    }
}
