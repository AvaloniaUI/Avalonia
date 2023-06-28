using System.Collections.Generic;

namespace Avalonia.Input.Platform;

public class PlatformPointerConfiguration
{
    public List<PointerGesture> OpenContextMenu { get; set; }

    public PlatformPointerConfiguration()
    {
        OpenContextMenu = new List<PointerGesture>
        {
            new PointerGesture(MouseButton.Right, KeyModifiers.None)
        };
    }

    public PlatformPointerConfiguration(params PointerGesture[] additionalContextGestures) : this()
    {
        OpenContextMenu.AddRange(additionalContextGestures);
    }
}
