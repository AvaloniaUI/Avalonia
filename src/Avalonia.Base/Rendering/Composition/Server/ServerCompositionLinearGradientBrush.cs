using Avalonia.Media;

namespace Avalonia.Rendering.Composition.Server
{
    internal partial class ServerCompositionLinearGradientBrush 
    {
        /*
        protected override void UpdateBackendBrush(ICbBrush brush)
        {
            var stopColors = new Color[Stops.List.Count];
            var offsets = new float[Stops.List.Count];
            for (var c = 0; c < Stops.List.Count; c++)
            {
                stopColors[c] = Stops.List[c].Color;
                offsets[c] = Stops.List[c].Offset;
            }

            ((ICbLinearGradientBrush) brush).Update(StartPoint, EndPoint, stopColors, offsets, ExtendMode);
        }*/

    }
}