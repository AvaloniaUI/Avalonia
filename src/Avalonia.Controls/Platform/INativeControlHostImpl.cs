using System;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Platform
{
    public interface INativeControlHostImpl
    {
        INativeControlHostDestroyableControlHandle CreateDefaultChild(IPlatformHandle parent);
        INativeControlHostControlTopLevelAttachment CreateNewAttachment(Func<IPlatformHandle, IPlatformHandle> create);
        INativeControlHostControlTopLevelAttachment CreateNewAttachment(IPlatformHandle handle);
        bool IsCompatibleWith(IPlatformHandle handle);
    }

    public interface INativeControlHostDestroyableControlHandle : IPlatformHandle
    {
        void Destroy();
    }

    public interface INativeControlHostControlTopLevelAttachment : IDisposable
    {
        INativeControlHostImpl AttachedTo { get; set; }
        bool IsCompatibleWith(INativeControlHostImpl host);
        void Hide();
        void ShowInBounds(TransformedBounds transformedBounds);
    }

    public interface ITopLevelImplWithNativeControlHost
    {
        INativeControlHostImpl NativeControlHost { get; }
    }
}
