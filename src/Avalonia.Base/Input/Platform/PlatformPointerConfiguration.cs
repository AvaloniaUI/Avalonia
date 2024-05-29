using System.Collections.Generic;
using Avalonia.Metadata;

namespace Avalonia.Input.Platform;

public sealed class PlatformPointerConfiguration
{
    public List<PointerGesture> OpenContextMenu { get; set; }

    [PrivateApi]
    public PlatformPointerConfiguration()
    {
        OpenContextMenu = new List<PointerGesture>
        {
            new PointerGesture(MouseButton.Right, KeyModifiers.None)
        };
    }

    [PrivateApi]
    public PlatformPointerConfiguration(params PointerGesture[] additionalContextGestures) : this()
    {
        OpenContextMenu.AddRange(additionalContextGestures);
    }
}
