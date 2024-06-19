using Avalonia.Controls;
using Avalonia.Platform;

namespace IntegrationTestApp.Embedding;

internal class NativeTextBox : NativeControlHost
{
    public static INativeControlFactory? Factory { get; set; }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        return Factory?.CreateControl(parent, () => base.CreateNativeControlCore(parent))
            ?? base.CreateNativeControlCore(parent); 
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        base.DestroyNativeControlCore(control);
    }
}
