using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using Avalonia.Utilities;
using Avalonia.Visuals.Media.Imaging;
using SkiaSharp;

namespace RenderDemo.Pages
{
    class CustomDrawOp : ICustomDrawOperation
    {
        private readonly FormattedText _noSkia;
        private readonly Rect _bounds;

        public CustomDrawOp(Rect bounds)
        {
            _bounds = bounds;
        }

        public void Dispose()
        {
            // No-op
        }

        public Rect Bounds => _bounds;

        public bool HitTest(Point p) => false;
        public bool Equals(ICustomDrawOperation other) => false;

        static Stopwatch St = Stopwatch.StartNew();

        private Stack<TargetSurface> _activeSurfaces = new Stack<TargetSurface>();

        readonly struct TargetSurface
        {
            public IDrawingContextLayerImpl Layer { get; }
            public IDrawingContextImpl Context { get; }
            public Size DipSize { get; }

            public TargetSurface(IDrawingContextLayerImpl newLayer, IDrawingContextImpl layerContext, Size dipSize)
            {
                Layer = newLayer;
                Context = layerContext;
                DipSize = dipSize;
            }
        }

        private void Save(IDrawingContextImpl mainCtx, Size size)
        {
            var newLayer = mainCtx.CreateLayer(size);
            _activeSurfaces.Push(new TargetSurface(newLayer, newLayer.CreateDrawingContext(null), size));
        }

        private IDrawingContextImpl GetCurrentLayerCtx() => _activeSurfaces.Peek().Context;

        private void Restore(IDrawingContextImpl mainCtx, BitmapBlendingMode blendingMode)
        {
            var poppedLayer = _activeSurfaces.Pop();


            mainCtx.DrawBitmap(RefCountable.CreateUnownedNotClonable(poppedLayer.Layer),
                1,
                new Rect(poppedLayer.DipSize),
                new Rect(poppedLayer.DipSize));
            poppedLayer.Context.Dispose();

            poppedLayer.Layer.Dispose();
        }

        public void Render(IDrawingContextImpl context)
        {
            Save(context, _bounds.Size);
            
            var cur = GetCurrentLayerCtx();
            
            cur.Transform *= Matrix.CreateTranslation(-25, -25);
            cur.DrawRectangle(new ImmutableSolidColorBrush(new Color(127, 127, 127, 127)), null,
                new RoundedRect(new Rect(0, 0, 250, 250)));

            Restore(context, BitmapBlendingMode.Xor);
            
            
            Save(context, _bounds.Size);
            
            var cur2 = GetCurrentLayerCtx();
            
            cur2.Transform *= Matrix.CreateTranslation(0, 0);
            cur2.DrawRectangle(new ImmutableSolidColorBrush(new Color(127, 127, 127, 127)), null,
                new RoundedRect(new Rect(0, 0, 250, 250)));

            Restore(context, BitmapBlendingMode.Xor);
 
        }
    }


    public class CustomSkiaPage : Control
    {
        public CustomSkiaPage()
        {
            ClipToBounds = true;
        }

        public override void Render(DrawingContext context)
        {
            context.Custom(new CustomDrawOp(new Rect(0, 0, Bounds.Width, Bounds.Height)));
            Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
        }
    }
}
