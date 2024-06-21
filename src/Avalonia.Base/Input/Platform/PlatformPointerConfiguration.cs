using System.Collections.Generic;
using Avalonia.Metadata;

namespace Avalonia.Input.Platform;

public sealed class PlatformPointerConfiguration
{
    public List<PointerActionGesture> OpenContextMenu { get; set; }

    [PrivateApi]
    public PlatformPointerConfiguration()
    {
        OpenContextMenu = new List<PointerActionGesture>
        {
            new(MouseButton.Right)
        };
    }

    [PrivateApi]
    public PlatformPointerConfiguration(params PointerActionGesture[] additionalContextGestures) : this()
    {
        OpenContextMenu.AddRange(additionalContextGestures);
    }
}
