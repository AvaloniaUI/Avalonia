using Avalonia.Controls.Platform;
using Tizen.NUI.BaseComponents;

namespace Avalonia.Tizen;

public class NuiViewControlHandle : INativeControlHostDestroyableControlHandle
{
    internal const string ViewDescriptor = nameof(View);

    public NuiViewControlHandle(View view)
    {
        View = view;
    }

    public View View { get; set; }
    public IntPtr Handle => throw new NotSupportedException();
    public string? HandleDescriptor => ViewDescriptor;
    public void Destroy() => View.Dispose();
}
