using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.SceneGraph;
using Avalonia.UnitTests;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering;

public class ImmediateRendererEffectTests
{
    [Fact]
    public void Effect_Should_Wrap_Clip_ClipGeometry_And_OpacityMask()
    {
        using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
        {
            var control = new RecordingControl
            {
                Width = 100,
                Height = 100,
                ClipToBounds = true,
                Clip = new EllipseGeometry(new Rect(0, 0, 100, 100)),
                OpacityMask = Brushes.Black,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 8,
                    OffsetX = 4,
                    OffsetY = 4,
                    Color = Colors.Black,
                    Opacity = 1
                }
            };

            control.Measure(Size.Infinity);
            control.Arrange(new Rect(control.DesiredSize));

            var context = new RecordingDrawingContext();
            ImmediateRenderer.Render(context, control);

            var calls = context.Calls;
            var renderIndex = calls.IndexOf("Render");

            Assert.Equal(
                ["PushEffect", "PushClip", "PushGeometryClip", "PushOpacityMask"],
                calls.Skip(renderIndex - 4).Take(4));

            Assert.Equal(
                ["PopOpacityMask", "PopGeometryClip", "PopClip", "PopEffect"],
                calls.Skip(renderIndex + 1).Take(4));
        }
    }

    private class RecordingControl : Control
    {
        public override void Render(DrawingContext context)
        {
            ((RecordingDrawingContext)context).Calls.Add("Render");
        }
    }

    private class RecordingDrawingContext : DrawingContext
    {
        public List<string> Calls { get; } = new();

        protected override void PushClipCore(Rect rect) => Calls.Add("PushClip");
        protected override void PushClipCore(RoundedRect rect) => Calls.Add("PushClip");
        protected override void PushGeometryClipCore(Geometry clip) => Calls.Add("PushGeometryClip");
        protected override void PushOpacityCore(double opacity) => Calls.Add("PushOpacity");
        protected override void PushOpacityMaskCore(IBrush mask, Rect bounds) => Calls.Add("PushOpacityMask");
        protected override void PushTransformCore(Matrix matrix) => Calls.Add("PushTransform");
        protected override void PushEffectCore(IEffect effect, Rect bounds) => Calls.Add("PushEffect");
        protected override void PushRenderOptionsCore(RenderOptions renderOptions) => Calls.Add("PushRenderOptions");
        protected override void PushTextOptionsCore(TextOptions textOptions) => Calls.Add("PushTextOptions");

        protected override void PopClipCore() => Calls.Add("PopClip");
        protected override void PopGeometryClipCore() => Calls.Add("PopGeometryClip");
        protected override void PopOpacityCore() => Calls.Add("PopOpacity");
        protected override void PopOpacityMaskCore() => Calls.Add("PopOpacityMask");
        protected override void PopTransformCore() => Calls.Add("PopTransform");
        protected override void PopEffectCore() => Calls.Add("PopEffect");
        protected override void PopRenderOptionsCore() => Calls.Add("PopRenderOptions");
        protected override void PopTextOptionsCore() => Calls.Add("PopTextOptions");

        protected override void DrawEllipseCore(IBrush? brush, IPen? pen, Rect rect) { }
        protected override void DrawGeometryCore(IBrush? brush, IPen? pen, IGeometryImpl geometry) { }
        protected override void DrawLineCore(IPen pen, Point p1, Point p2) { }
        protected override void DrawRectangleCore(IBrush? brush, IPen? pen, RoundedRect rrect, BoxShadows boxShadows = default) { }
        protected override void DisposeCore() { }
        internal override void DrawBitmap(IRef<IBitmapImpl> source, double opacity, Rect sourceRect, Rect destRect) { }
        public override void Custom(ICustomDrawOperation custom) { }
        public override void DrawGlyphRun(IBrush? foreground, GlyphRun glyphRun) { }
    }
}
