using System;

namespace Avalonia.Rendering.Composition.Server
{
    internal abstract partial class ServerCompositionGradientBrush : ServerCompositionBrush
    {
        public ServerCompositionGradientStopCollection Stops { get; }
        public ServerCompositionGradientBrush(ServerCompositor compositor) : base(compositor)
        {
            Stops = new ServerCompositionGradientStopCollection(compositor);
        }

        public override long LastChangedBy => Math.Max(base.LastChangedBy, (long)Stops.LastChangedBy);
    }
}