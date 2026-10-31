namespace Avalonia.Media.Fonts.Tables.Colr
{
    /// <summary>
    /// Traverses a resolved paint tree and calls the appropriate methods on the painter.
    /// </summary>
    internal static class PaintTraverser
    {
        public static void Traverse(Paint paint, IColorPainter painter)
        {
            switch (paint)
            {
                case ResolvedSolid fill:
                    painter.FillSolid(fill.Color);
                    break;

                case ResolvedClipBox clip:
                    painter.PushClip(clip.Box);
                    Traverse(clip.Inner, painter);
                    painter.PopClip();
                    break;

                case ResolvedLinearGradient linearGrad:
                    painter.FillLinearGradient(
                        linearGrad.P0, 
                        linearGrad.P1, 
                        linearGrad.Stops, 
                        linearGrad.Extend);
                    break;

                case ResolvedRadialGradient radialGrad:
                    painter.FillRadialGradient(
                        radialGrad.C0, 
                        radialGrad.R0, 
                        radialGrad.C1, 
                        radialGrad.R1, 
                        radialGrad.Stops, 
                        radialGrad.Extend);
                    break;

                case ResolvedConicGradient conicGrad:
                    painter.FillConicGradient(
                        conicGrad.Center, 
                        conicGrad.StartAngle, 
                        conicGrad.EndAngle, 
                        conicGrad.Stops, 
                        conicGrad.Extend);
                    break;

                case ResolvedTransform t:
                    painter.PushTransform(t.Matrix);
                    Traverse(t.Inner, painter);
                    painter.PopTransform();
                    break;

                case ColrLayers layers:
                    foreach (var child in layers.Layers)
                        Traverse(child, painter);
                    break;

                case Glyph glyph:
                    // PaintGlyph clips its child paint to the glyph outline in the current transform;
                    // fills inside paint the clip region, whatever transforms or layers sit between.
                    painter.PushGlyphClip(glyph.GlyphIndex);
                    Traverse(glyph.Paint, painter);
                    painter.PopClip();
                    break;

                case Composite comp:
                    painter.PushLayer(comp.Mode);
                    Traverse(comp.Backdrop, painter);
                    Traverse(comp.Source, painter);
                    painter.PopLayer();
                    break;
            }
        }
    }
}
