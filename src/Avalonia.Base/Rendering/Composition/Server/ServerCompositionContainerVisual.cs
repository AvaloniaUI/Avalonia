using System;
using System.Numerics;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server
{
    /// <summary>
    /// Server-side counterpart of <see cref="CompositionContainerVisual"/>.
    /// Mostly propagates update and render calls, but is also responsible
    /// for updating adorners in deferred manner
    /// </summary>
    internal partial class ServerCompositionContainerVisual : ServerCompositionVisual
    {
        public new ServerCompositionVisualCollection Children => base.Children!;
    }
}
