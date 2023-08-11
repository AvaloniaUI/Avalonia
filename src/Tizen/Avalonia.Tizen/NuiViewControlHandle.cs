using Avalonia.Controls.Platform;
using Tizen.NUI.BaseComponents;

namespace Avalonia.Tizen;

/// <summary>
/// Tizen Nui native view handle for native view attachment
/// </summary>
public class NuiViewControlHandle : INativeControlHostDestroyableControlHandle
{
    internal const string ViewDescriptor = "NuiView";

    /// <summary>
    /// Create handle with native view
    /// </summary>
    /// <param name="view">NUI Tizen native view to attach</param>
    public NuiViewControlHandle(View view)
    {
        View = view;
    }

    /// <summary>
    /// NUI Tizen View
    /// </summary>
    public View View { get; set; }
    /// <summary>
    /// NUI Tizen not supporting handle
    /// </summary>
    /// <exception cref="NotSupportedException"></exception>
    public IntPtr Handle => throw new NotSupportedException();
    /// <summary>
    /// Return `ViewDescriptor` all the time
    /// </summary>
    public string? HandleDescriptor => ViewDescriptor;
    /// <summary>
    /// Dispose Tizen View when it call
    /// </summary>
    public void Destroy() => View.Dispose();
}
