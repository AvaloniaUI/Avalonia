using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition
{
    public partial class CompositionGradientBrush : CompositionBrush
    {
        internal CompositionGradientBrush(Compositor compositor, ServerCompositionGradientBrush server) : base(compositor, server)
        {
            ColorStops = new CompositionGradientStopCollection(compositor, server.Stops);
        }
        
        public CompositionGradientStopCollection ColorStops { get; }
    }
    
    
}