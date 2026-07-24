using Avalonia.Media;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition;

public partial class CompositionGradientStop : IGradientStop
{
    internal CompositionGradientStop(Compositor compositor, ServerCompositionGradientStop server, double offset, Color color) : base(compositor, server)
    {
        Server = server;
        if (MathUtilities.IsZero(offset))
        {
            offset = 0;
        }
        Offset = (offset < 0) ? 0 : (offset > 1) ? 1 : offset;
        Color = color;
        InitializeDefaults();
    }
    partial void InitializeDefaultsExtra()
    {
        Server.Activate();
    }
}
