using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Media.Imaging;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Platform;
using static Avalonia.OpenGL.GlConsts;

namespace ImpellerSandbox;

/// <summary>The sandbox content, shared by the desktop and mobile heads.</summary>
internal class SandboxView : UserControl
{
    private readonly TextBlock _cacheCounter = new() { Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center };
    private int _cacheTicks;

    public SandboxView()
    {
        Background = Brushes.DimGray;

        var gradient = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Colors.MediumPurple, 0),
                new GradientStop(Colors.DeepSkyBlue, 1)
            }
        };

        var header = new Border
        {
            Background = gradient,
            BorderBrush = Brushes.White,
            BorderThickness = new Thickness(3),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(24),
            Margin = new Thickness(16),
            Child = new TextBlock
            {
                Text = "Impeller × Avalonia — shapes & text",
                FontSize = 26,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };

        var checker = CreateCheckerboard();

        var controlsRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Margin = new Thickness(16, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new Button { Content = "A Button" },
                new CheckBox { Content = "A CheckBox", IsChecked = true },
                // OpenGL control (GPU texture-sharing interop): orange clear + blue GL-origin corner.
                new GlTestControl { Width = 90, Height = 60 },
                new TextBox { PlaceholderText = "Type here…", Width = 160 },
                // Raster bitmap via DrawBitmap.
                new Image { Source = checker, Width = 64, Height = 64 },
                // Same bitmap as an ImageBrush fill.
                new Border
                {
                    Width = 96, Height = 64, CornerRadius = new CornerRadius(10),
                    Background = new ImageBrush(checker) { Stretch = Stretch.Fill }
                },
                // Gradient-foreground text.
                new TextBlock
                {
                    Text = "Gradient", FontSize = 30, VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new LinearGradientBrush
                    {
                        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                        EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
                        GradientStops = { new GradientStop(Colors.Magenta, 0), new GradientStop(Colors.Cyan, 1) }
                    }
                }
            }
        };

        // Effect pipeline: BlurEffect + DropShadowEffect, driven via Control.Effect.
        var effectsRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 24,
            Margin = new Thickness(16, 8),
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                // Drop shadow: expect a soft dark shadow offset to the bottom-right.
                new Border
                {
                    Width = 140, Height = 70, CornerRadius = new CornerRadius(12),
                    Background = Brushes.OrangeRed,
                    Child = new TextBlock
                    {
                        Text = "Drop shadow", Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    Effect = new DropShadowEffect { OffsetX = 8, OffsetY = 8, BlurRadius = 10, Color = Colors.Black, Opacity = 0.7 }
                },
                // Blur: expect the whole element softened.
                new Border
                {
                    Width = 140, Height = 70, CornerRadius = new CornerRadius(12),
                    Background = gradient,
                    Child = new TextBlock
                    {
                        Text = "Blurred", FontSize = 20, Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    Effect = new BlurEffect { Radius = 6 }
                },
                // Reference: same element, no effect.
                new Border
                {
                    Width = 140, Height = 70, CornerRadius = new CornerRadius(12),
                    Background = gradient,
                    Child = new TextBlock
                    {
                        Text = "No effect", FontSize = 20, Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                },
                // Bitmap cache: rendered through an offscreen layer, partially redrawn as the counter ticks.
                new Border
                {
                    Width = 140, Height = 70, CornerRadius = new CornerRadius(12),
                    Background = Brushes.SeaGreen,
                    CacheMode = new BitmapCache(),
                    Child = new StackPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Children =
                        {
                            new TextBlock { Text = "Cached", FontSize = 20, Foreground = Brushes.White },
                            _cacheCounter
                        }
                    }
                }
            }
        };
        DispatcherTimer.Run(() =>
        {
            _cacheCounter.Text = (++_cacheTicks).ToString();
            return true;
        }, TimeSpan.FromMilliseconds(100));

        // Tile / scene brushes: VisualBrush (scene) + tiled and stretched ImageBrushes.
        var visualBrushSource = new Border
        {
            Width = 80, Height = 80, Background = Brushes.MidnightBlue,
            Child = new TextBlock
            {
                Text = "V", FontSize = 44, Foreground = Brushes.Gold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        var brushRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 24,
            Margin = new Thickness(16, 8),
            Children =
            {
                // VisualBrush (scene brush), stretched to fill.
                new Border
                {
                    Width = 140, Height = 70, CornerRadius = new CornerRadius(12),
                    Background = new VisualBrush { Visual = visualBrushSource, Stretch = Stretch.Uniform }
                },
                // Tiled ImageBrush: 32x32 tiles repeated.
                new Border
                {
                    Width = 140, Height = 70, CornerRadius = new CornerRadius(12),
                    Background = new ImageBrush(checker)
                    {
                        TileMode = TileMode.Tile, Stretch = Stretch.None,
                        DestinationRect = new RelativeRect(0, 0, 32, 32, RelativeUnit.Absolute)
                    }
                },
                // ImageBrush stretched Uniform (letterboxed).
                new Border
                {
                    Width = 140, Height = 70, CornerRadius = new CornerRadius(12), Background = Brushes.Black,
                    Child = new Border { Background = new ImageBrush(checker) { Stretch = Stretch.Uniform } }
                },
                // RenderTargetBitmap filled with a tiled ImageBrush.
                new Image { Width = 140, Height = 70, Source = CreateTiledRtb(checker) }
            }
        };

        var layout = new StackPanel
        {
            Children =
            {
                header, controlsRow, effectsRow,
                // Per-draw blend modes: same two bitmaps composited with different modes.
                new BlendCanvas { Height = 96, Margin = new Thickness(16, 0) },
                brushRow,
                // Gradient fidelity: linear-with-transform, elliptical radial, focal radial, conic.
                new GradientCanvas { Height = 110, Margin = new Thickness(16, 4) },
                // Dashed strokes (complex arrays) + opacity mask.
                new DashMaskCanvas { Height = 110, Margin = new Thickness(16, 4) },
                // Inset box shadow, outset box shadow, acrylic.
                new StackPanel
                {
                    Orientation = Orientation.Horizontal, Spacing = 24, Margin = new Thickness(16, 4),
                    Children =
                    {
                        new Border
                        {
                            Width = 150, Height = 70, CornerRadius = new CornerRadius(12), Background = Brushes.Gainsboro,
                            BoxShadow = BoxShadows.Parse("inset 5 5 16 0 #B0000000")
                        },
                        new Border
                        {
                            Width = 150, Height = 70, CornerRadius = new CornerRadius(12), Background = Brushes.Gainsboro,
                            BoxShadow = BoxShadows.Parse("6 6 14 0 #B0000000")
                        },
                        new Panel
                        {
                            Width = 180, Height = 70,
                            Children =
                            {
                                new Border { CornerRadius = new CornerRadius(12), Background = gradient },
                                new ExperimentalAcrylicBorder
                                {
                                    CornerRadius = new CornerRadius(12),
                                    Material = new ExperimentalAcrylicMaterial
                                    {
                                        TintColor = Colors.SkyBlue, TintOpacity = 0.7, MaterialOpacity = 0.5
                                    }
                                }
                            }
                        }
                    }
                },
                new ShapeCanvas { Height = 150, Margin = new Thickness(16) }
            }
        };

        Content = new ScrollViewer
        {
            Content = layout,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };
    }

    /// <summary>Renders a tiled ImageBrush into a RenderTargetBitmap.</summary>
    private static RenderTargetBitmap CreateTiledRtb(Bitmap checker)
    {
        var rtb = new RenderTargetBitmap(new PixelSize(140, 70), new Vector(96, 96));
        using (var ctx = rtb.CreateDrawingContext())
        {
            ctx.FillRectangle(Brushes.Black, new Rect(0, 0, 140, 70));
            var brush = new ImageBrush(checker)
            {
                TileMode = TileMode.Tile, Stretch = Stretch.None,
                DestinationRect = new RelativeRect(0, 0, 32, 32, RelativeUnit.Absolute)
            };
            ctx.FillRectangle(brush, new Rect(0, 0, 140, 70));
        }
        return rtb;
    }

    /// <summary>Builds a small RGBA checkerboard bitmap in code to exercise the bitmap upload path.</summary>
    private static unsafe WriteableBitmap CreateCheckerboard()
    {
        const int n = 64;
        var bmp = new WriteableBitmap(new PixelSize(n, n), new Vector(96, 96), PixelFormats.Rgba8888, AlphaFormat.Premul);
        using var fb = bmp.Lock();
        var ptr = (byte*)fb.Address;
        for (var y = 0; y < n; y++)
        {
            for (var x = 0; x < n; x++)
            {
                var cell = ((x / 8) + (y / 8)) % 2 == 0;
                var p = ptr + y * fb.RowBytes + x * 4;
                p[0] = (byte)(cell ? 230 : 40);   // R
                p[1] = (byte)(x * 4);              // G ramp
                p[2] = (byte)(cell ? 40 : 230);   // B
                p[3] = 255;                        // A
            }
        }
        return bmp;
    }
}

/// <summary>
/// Composites an overlay bitmap over a base bitmap with a different blend mode per cell. Advanced modes need
/// a backdrop readback, so Impeller draws them as SourceOver.
/// </summary>
internal class BlendCanvas : Control
{
    private static readonly (BitmapBlendingMode Mode, string Label)[] Modes =
    {
        (BitmapBlendingMode.SourceOver, "SourceOver"),
        (BitmapBlendingMode.Plus, "Plus"),
        (BitmapBlendingMode.DestinationOver, "DestOver"),
        (BitmapBlendingMode.Xor, "Xor")
    };

    private readonly WriteableBitmap _base = CreateGradient(horizontal: true, alpha: 255);      // opaque red → blue
    private readonly WriteableBitmap _overlay = CreateGradient(horizontal: false, alpha: 160);  // 63% white → black

    public override void Render(DrawingContext context)
    {
        context.FillRectangle(Brushes.Black, new Rect(Bounds.Size));

        var cellW = Bounds.Width / Modes.Length;
        for (var i = 0; i < Modes.Length; i++)
        {
            var cell = new Rect(i * cellW + 6, 6, cellW - 12, Bounds.Height - 12);

            context.DrawImage(_base, cell);

            using (context.PushRenderOptions(new RenderOptions { BitmapBlendingMode = Modes[i].Mode }))
                context.DrawImage(_overlay, cell);

            var label = new FormattedText(Modes[i].Label, CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, new Typeface("Inter"), 13, Brushes.White);
            context.DrawText(label, new Point(cell.X + 4, cell.Y + 2));
        }
    }

    private static unsafe WriteableBitmap CreateGradient(bool horizontal, byte alpha)
    {
        const int n = 64;
        var bmp = new WriteableBitmap(new PixelSize(n, n), new Vector(96, 96), PixelFormats.Rgba8888, AlphaFormat.Unpremul);
        using var fb = bmp.Lock();
        var ptr = (byte*)fb.Address;
        for (var y = 0; y < n; y++)
        {
            for (var x = 0; x < n; x++)
            {
                var t = (horizontal ? x : y) / (float)(n - 1);
                var p = ptr + y * fb.RowBytes + x * 4;
                if (horizontal)
                {
                    p[0] = (byte)(255 * (1 - t)); // R
                    p[1] = 0;                     // G
                    p[2] = (byte)(255 * t);       // B
                }
                else
                {
                    var g = (byte)(255 * t);      // white → black
                    p[0] = g; p[1] = g; p[2] = g;
                }
                p[3] = alpha;
            }
        }
        return bmp;
    }
}

/// <summary>A minimal OpenGL control to exercise GPU texture-sharing interop on the Impeller backend.</summary>
internal class GlTestControl : OpenGlControlBase
{
    private delegate void GlIntDelegate(int cap);
    private delegate void GlScissorDelegate(int x, int y, int w, int h);

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        gl.ClearColor(1f, 0.45f, 0f, 1f); // orange
        gl.Clear(GL_COLOR_BUFFER_BIT);

        // Blue square at GL origin (0,0 = bottom-left) to reveal composited orientation.
        var enable = System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer<GlIntDelegate>(gl.GetProcAddress("glEnable"));
        var disable = System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer<GlIntDelegate>(gl.GetProcAddress("glDisable"));
        var scissor = System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer<GlScissorDelegate>(gl.GetProcAddress("glScissor"));
        enable(GL_SCISSOR_TEST);
        scissor(0, 0, 30, 30);
        gl.ClearColor(0.1f, 0.5f, 1f, 1f); // blue
        gl.Clear(GL_COLOR_BUFFER_BIT);
        disable(GL_SCISSOR_TEST);
    }
}

/// <summary>Exercises complex dashed strokes (rect, rounded rect, ellipse, line) and an opacity mask.</summary>
internal class DashMaskCanvas : Control
{
    public override void Render(DrawingContext context)
    {
        context.FillRectangle(Brushes.Black, new Rect(Bounds.Size));

        var dash = new DashStyle(new double[] { 5, 2, 1, 2 }, 0);
        var h = Bounds.Height - 24;

        context.DrawRectangle(null, new Pen(Brushes.Cyan, 3, dashStyle: dash), new Rect(12, 12, 120, h));
        context.DrawRectangle(null, new Pen(Brushes.Yellow, 3, dashStyle: dash),
            new RoundedRect(new Rect(150, 12, 120, h), 16));
        context.DrawEllipse(null, new Pen(Brushes.Lime, 3, dashStyle: dash), new Rect(290, 12, 120, h));
        context.DrawLine(new Pen(Brushes.White, 3, dashStyle: dash),
            new Point(430, 12 + h / 2), new Point(560, 12 + h / 2));

        // Opacity mask: fade a filled rect from opaque (left) to transparent (right).
        var maskBounds = new Rect(580, 12, 170, h);
        var mask = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
            GradientStops = { new GradientStop(Colors.White, 0), new GradientStop(Colors.Transparent, 1) }
        };
        using (context.PushOpacityMask(mask, maskBounds))
            context.FillRectangle(Brushes.OrangeRed, maskBounds);
    }
}

/// <summary>Exercises gradient fidelity: brush transforms, elliptical + focal radial, conic angle.</summary>
internal class GradientCanvas : Control
{
    public override void Render(DrawingContext context)
    {
        context.FillRectangle(Brushes.Black, new Rect(Bounds.Size));

        var cellW = Bounds.Width / 4;
        var cell = new Size(cellW - 12, Bounds.Height - 12);

        // 1. Linear gradient with a 45° brush Transform.
        var linear = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            GradientStops = { new GradientStop(Colors.OrangeRed, 0), new GradientStop(Colors.Yellow, 1) },
            Transform = new RotateTransform(45)
        };
        context.DrawRectangle(linear, null, new Rect(new Point(0 * cellW + 6, 6), cell));

        // 2. Elliptical radial (RadiusX != RadiusY).
        var elliptical = new RadialGradientBrush
        {
            Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            RadiusX = new RelativeScalar(0.5, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(0.25, RelativeUnit.Relative),
            GradientStops = { new GradientStop(Colors.White, 0), new GradientStop(Colors.MediumBlue, 1) }
        };
        context.DrawRectangle(elliptical, null, new Rect(new Point(1 * cellW + 6, 6), cell));

        // 3. Radial with an off-center focal point (GradientOrigin ≠ Center).
        var focal = new RadialGradientBrush
        {
            Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0.28, 0.28, RelativeUnit.Relative),
            RadiusX = new RelativeScalar(0.5, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(0.5, RelativeUnit.Relative),
            GradientStops = { new GradientStop(Colors.White, 0), new GradientStop(Colors.DarkGreen, 1) }
        };
        context.DrawRectangle(focal, null, new Rect(new Point(2 * cellW + 6, 6), cell));

        // 4. Conic gradient (angle sweep starting from the top).
        var conic = new ConicGradientBrush
        {
            Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            Angle = 0,
            GradientStops =
            {
                new GradientStop(Colors.Red, 0), new GradientStop(Colors.Lime, 0.33),
                new GradientStop(Colors.Blue, 0.66), new GradientStop(Colors.Red, 1)
            }
        };
        context.DrawRectangle(conic, null, new Rect(new Point(3 * cellW + 6, 6), cell));
    }
}

/// <summary>Draws primitives directly through the drawing context to exercise the backend.</summary>
internal class ShapeCanvas : Control
{
    public override void Render(DrawingContext context)
    {
        context.FillRectangle(Brushes.Black, new Rect(Bounds.Size));

        // Solid + stroked rectangle.
        context.DrawRectangle(Brushes.CornflowerBlue, new Pen(Brushes.Yellow, 4), new Rect(20, 20, 200, 120));

        // Rounded rect with a linear gradient fill and a stroke.
        var grad = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
            GradientStops = { new GradientStop(Colors.OrangeRed, 0), new GradientStop(Colors.Gold, 1) }
        };
        context.DrawRectangle(grad, new Pen(Brushes.White, 2), new RoundedRect(new Rect(260, 20, 220, 120), 24));

        // Ellipse.
        context.DrawEllipse(Brushes.MediumSpringGreen, new Pen(Brushes.DarkGreen, 3),
            new Point(620, 80), 80, 55);

        // Line.
        context.DrawLine(new Pen(Brushes.White, 3), new Point(20, 170), new Point(700, 170));

        // A filled triangle (stream geometry / path).
        var triangle = new StreamGeometry();
        using (var g = triangle.Open())
        {
            g.BeginFigure(new Point(120, 200), true);
            g.LineTo(new Point(220, 340));
            g.LineTo(new Point(20, 340));
            g.EndFigure(true);
        }
        context.DrawGeometry(Brushes.HotPink, new Pen(Brushes.White, 2), triangle);

        // Text via the glyph-outline path.
        var text = new FormattedText(
            "The quick brown fox — 0123456789",
            CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface("Inter"), 30, Brushes.White);
        context.DrawText(text, new Point(260, 220));

        var text2 = new FormattedText(
            "Rendered by Impeller via NImpeller",
            CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface("Inter"), 20, Brushes.LightGray);
        context.DrawText(text2, new Point(260, 280));
    }
}
