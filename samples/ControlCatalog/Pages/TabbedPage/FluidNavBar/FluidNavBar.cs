using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using SkiaSharp;

namespace ControlCatalog.Pages
{
    /// <summary>
    /// A fluid navigation bar that replicates the Flutter fluid_nav_bar vignette.
    /// The bar background has a bezier "dip" that travels to the selected tab.
    /// Each icon is drawn progressively using SKPathMeasure for the fill animation.
    /// </summary>
    public class FluidNavBar : Control, Avalonia.Rendering.ICustomHitTest
    {
        internal const double NominalHeight    = 56.0;
        internal const double CircleRadius     = 25.0;
        internal const double ActiveFloat      = 16.0;   // px the circle rises
        internal const double IconDrawScale    = 0.9;    // icon scale within circle
        internal const double ScaleCurveScale  = 0.50;
        internal const double FloatLinearPIn   = 0.28;
        internal const double FillLinearPIn    = 0.25;
        internal const double XAnimDuration    = 0.620;  // s — X bump travel
        internal const double YDipDuration     = 0.300;  // s — dip down
        internal const double YBounceDelay     = 0.500;  // s — wait before bounce
        internal const double YBounceDuration  = 1.200;  // s — elastic bounce up
        internal const double FloatUpDuration  = 1.666;  // s — circle rising
        internal const double FloatDownDuration = 0.833; // s — circle falling

        public static readonly StyledProperty<IList<FluidNavItem>> ItemsProperty =
            AvaloniaProperty.Register<FluidNavBar, IList<FluidNavItem>>(
                nameof(Items), new List<FluidNavItem>());

        public static readonly StyledProperty<int> SelectedIndexProperty =
            AvaloniaProperty.Register<FluidNavBar, int>(nameof(SelectedIndex), 0);

        public static readonly StyledProperty<Color> BarColorProperty =
            AvaloniaProperty.Register<FluidNavBar, Color>(nameof(BarColor), Colors.White);

        public static readonly StyledProperty<Color> ButtonColorProperty =
            AvaloniaProperty.Register<FluidNavBar, Color>(nameof(ButtonColor), Colors.White);

        public static readonly StyledProperty<Color> ActiveIconColorProperty =
            AvaloniaProperty.Register<FluidNavBar, Color>(nameof(ActiveIconColor), Colors.Black);

        public static readonly StyledProperty<Color> InactiveIconColorProperty =
            AvaloniaProperty.Register<FluidNavBar, Color>(
                nameof(InactiveIconColor), Color.FromArgb(140, 120, 120, 120));

        private double   _xCurrent  = -1;   // -1 = not yet initialised
        private double   _lastWidth  = -1;   // tracks width changes for resize correction
        private double   _xStart, _xTarget, _xAnimStartSec;
        private double   _yValue    = 1.0;  // 0 = deepest dip, 1 = flat
        private double   _yDipStartSec;
        private bool     _yBounceStarted;
        private double   _yBounceStartSec;

        // per-item (length = Items.Count after OnItemsChanged)
        private double[] _floatProgress  = Array.Empty<double>();
        private double[] _floatStartSec  = Array.Empty<double>();
        private bool[]   _floatGoingUp   = Array.Empty<bool>();

        // Parsed Skia paths — owned here, disposed on detach / items change
        private SKPath?[] _parsedPaths = Array.Empty<SKPath?>();

        private DispatcherTimer? _animTimer;
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private bool _animating;

        public IList<FluidNavItem> Items
        {
            get => GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        public int SelectedIndex
        {
            get => GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, value);
        }

        public Color BarColor
        {
            get => GetValue(BarColorProperty);
            set => SetValue(BarColorProperty, value);
        }

        public Color ButtonColor
        {
            get => GetValue(ButtonColorProperty);
            set => SetValue(ButtonColorProperty, value);
        }

        public Color ActiveIconColor
        {
            get => GetValue(ActiveIconColorProperty);
            set => SetValue(ActiveIconColorProperty, value);
        }

        public Color InactiveIconColor
        {
            get => GetValue(InactiveIconColorProperty);
            set => SetValue(InactiveIconColorProperty, value);
        }

        public event EventHandler<int>? SelectionChanged;

        public FluidNavBar()
        {
            ClipToBounds = false;
            Height = NominalHeight;
            Cursor = new Cursor(StandardCursorType.Hand);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ItemsProperty)
                OnItemsChanged();
            else if (change.Property == SelectedIndexProperty)
                OnSelectedIndexChanged(change.GetOldValue<int>(), change.GetNewValue<int>());
            else if (change.Property == BarColorProperty
                  || change.Property == ButtonColorProperty
                  || change.Property == ActiveIconColorProperty
                  || change.Property == InactiveIconColorProperty)
                InvalidateVisual();
        }

        public bool HitTest(Point point)
        {
            return point.X >= 0 && point.X <= Bounds.Width
                && point.Y >= -(ActiveFloat + CircleRadius)
                && point.Y <= Bounds.Height;
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            var n = Items?.Count ?? 0;
            if (n == 0 || Bounds.Width <= 0) return;

            var pos   = e.GetPosition(this);
            var index = (int)(pos.X / (Bounds.Width / n));
            index = Math.Clamp(index, 0, n - 1);

            if (index != SelectedIndex)
            {
                SetCurrentValue(SelectedIndexProperty, index);
                SelectionChanged?.Invoke(this, index);
            }

            e.Handled = true;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var w = double.IsPositiveInfinity(availableSize.Width) ? 300 : availableSize.Width;
            return new Size(w, NominalHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var w = finalSize.Width;
            if (w > 0)
            {
                if (_xCurrent < 0 || _lastWidth < 0)
                {
                    // First layout — snap everything to the current selection
                    _xCurrent = IndexToX(SelectedIndex, w);
                    _xTarget  = _xCurrent;
                    _xStart   = _xCurrent;
                }
                else if (Math.Abs(w - _lastWidth) > 0.5)
                {
                    // Width changed (resize) — scale pixel positions proportionally
                    // so the bump stays over the correct slot
                    var ratio = w / _lastWidth;
                    _xCurrent = _xCurrent * ratio;
                    _xStart   = _xStart   * ratio;
                    _xTarget  = IndexToX(SelectedIndex, w);
                    InvalidateVisual();
                }
                _lastWidth = w;
            }
            return new Size(w > 0 ? w : 300, NominalHeight);
        }

        public override void Render(DrawingContext context)
        {
            var w = Bounds.Width;
            var h = Bounds.Height;

            if (w <= 0 || h <= 0 || Items == null || Items.Count == 0) return;

            var n = Items.Count;

            // Initialise _xCurrent if layout didn't run yet
            if (_xCurrent < 0)
            {
                _xCurrent = IndexToX(SelectedIndex, w);
                _xTarget  = _xCurrent;
            }

            // Snapshot per-item animation state for this frame
            var slotCenters  = new double[n];
            var floatOffsets = new double[n];
            var scaleYValues = new double[n];
            var fillAmounts  = new double[n];

            for (int i = 0; i < n; i++)
                slotCenters[i] = IndexToX(i, w);

            for (int i = 0; i < n; i++)
            {
                var p      = i < _floatProgress.Length ? _floatProgress[i] : (i == SelectedIndex ? 1.0 : 0.0);
                var goUp   = i < _floatGoingUp.Length  ? _floatGoingUp[i]  : (i == SelectedIndex);

                // Float offset — uses LinearPoint(0.28, 0) to delay start, then elastic/quintic easing
                var linearP   = LinearPoint(p, FloatLinearPIn, 0.0);
                var floatEased = goUp ? ElasticOut(linearP, 0.38) : EaseInQuint(linearP);
                floatOffsets[i] = ActiveFloat * floatEased;

                // Scale Y squish via CenteredElastic curves
                var centered   = goUp ? CenteredElasticOut(p, 0.6) : CenteredElasticIn(p, 0.6);
                scaleYValues[i] = 0.75 + centered * ScaleCurveScale;

                // Icon fill — LinearPoint(0.25, 1.0) adds a slight draw delay vs float
                fillAmounts[i] = LinearPoint(p, FillLinearPIn, 1.0);
            }

            // Clamp scaleY to sane range to avoid SVG-transform oddities
            for (int i = 0; i < n; i++)
                scaleYValues[i] = Math.Max(0.1, Math.Min(1.5, scaleYValues[i]));

            var op = new FluidNavBarRenderOp(
                new Rect(0, -(ActiveFloat + CircleRadius), w, h + ActiveFloat + CircleRadius),
                (float)w, (float)h,
                (float)_xCurrent, (float)_yValue,
                slotCenters, floatOffsets, scaleYValues, fillAmounts,
                _parsedPaths,
                BarColor, ButtonColor, ActiveIconColor, InactiveIconColor);

            context.Custom(op);
        }

        private void OnItemsChanged()
        {
            foreach (var p in _parsedPaths) p?.Dispose();

            var n = Items?.Count ?? 0;
            _parsedPaths    = new SKPath?[n];
            _floatProgress  = new double[n];
            _floatStartSec  = new double[n];
            _floatGoingUp   = new bool[n];

            for (int i = 0; i < n; i++)
            {
                var svg = Items![i].SvgPath;
                if (!string.IsNullOrEmpty(svg))
                    _parsedPaths[i] = SKPath.ParseSvgPathData(svg);
            }

            var sel = Math.Clamp(SelectedIndex, 0, Math.Max(0, n - 1));
            for (int i = 0; i < n; i++)
            {
                _floatProgress[i] = i == sel ? 1.0 : 0.0;
                _floatGoingUp[i]  = i == sel;
            }

            _xCurrent = -1; // force re-init on next arrange/render
            InvalidateVisual();
        }

        private void OnSelectedIndexChanged(int oldIndex, int newIndex)
        {
            var n = _floatProgress.Length;
            if (n == 0) return;

            newIndex = Math.Clamp(newIndex, 0, n - 1);
            oldIndex = Math.Clamp(oldIndex, 0, n - 1);
            if (oldIndex == newIndex) return;

            var now = _clock.Elapsed.TotalSeconds;

            // X: slide bump from old to new position
            if (_xCurrent < 0 && Bounds.Width > 0)
                _xCurrent = IndexToX(oldIndex, Bounds.Width);

            _xStart        = _xCurrent;
            _xTarget       = Bounds.Width > 0 ? IndexToX(newIndex, Bounds.Width) : _xStart;
            _xAnimStartSec = now;

            // Y: dip then elastic bounce
            _yValue         = 1.0;
            _yDipStartSec   = now;
            _yBounceStarted = false;

            // Per-button float
            _floatGoingUp[oldIndex] = false;
            _floatStartSec[oldIndex] = now;
            _floatGoingUp[newIndex]  = true;
            _floatStartSec[newIndex] = now;

            StartAnimation();
        }

        private void StartAnimation()
        {
            if (_animating) return;
            _animating = true;
            _animTimer = new DispatcherTimer(
                TimeSpan.FromSeconds(1.0 / 60.0),
                DispatcherPriority.Render,
                OnAnimTick);
            _animTimer.Start();
        }

        private void StopAnimation()
        {
            _animTimer?.Stop();
            _animTimer  = null;
            _animating  = false;
        }

        private void OnAnimTick(object? sender, EventArgs e)
        {
            var now = _clock.Elapsed.TotalSeconds;
            var anyActive = false;

            var xElapsed = now - _xAnimStartSec;
            if (xElapsed < XAnimDuration)
            {
                _xCurrent = _xStart + (_xTarget - _xStart) * (xElapsed / XAnimDuration);
                anyActive = true;
            }
            else
            {
                _xCurrent = _xTarget;
            }

            var yDipElapsed = now - _yDipStartSec;
            if (yDipElapsed < YDipDuration)
            {
                _yValue   = 1.0 - yDipElapsed / YDipDuration;
                anyActive = true;
            }
            else
            {
                _yValue = 0.0;

                if (!_yBounceStarted && yDipElapsed >= YBounceDelay)
                {
                    _yBounceStarted  = true;
                    _yBounceStartSec = now;
                }

                if (_yBounceStarted)
                {
                    var bt = now - _yBounceStartSec;
                    if (bt < YBounceDuration)
                    {
                        _yValue   = ElasticOut(bt / YBounceDuration, 0.38);
                        anyActive = true;
                    }
                    else
                    {
                        _yValue = 1.0;
                    }
                }
            }

            for (int i = 0; i < _floatProgress.Length; i++)
            {
                var elapsed  = now - _floatStartSec[i];
                var duration = _floatGoingUp[i] ? FloatUpDuration : FloatDownDuration;
                if (elapsed < duration)
                {
                    var t = elapsed / duration;
                    _floatProgress[i] = _floatGoingUp[i] ? t : 1.0 - t;
                    anyActive = true;
                }
                else
                {
                    _floatProgress[i] = _floatGoingUp[i] ? 1.0 : 0.0;
                }
            }

            InvalidateVisual();

            if (!anyActive)
                StopAnimation();
        }

        private double IndexToX(int index, double width)
        {
            var n = Items?.Count ?? 1;
            if (n <= 0) n = 1;
            return (index + 0.5) * (width / n);
        }


        internal static double ElasticOut(double t, double period = 0.4)
        {
            if (t <= 0) return 0;
            if (t >= 1) return 1;
            var s = period / 4.0;
            return Math.Pow(2.0, -10.0 * t) * Math.Sin((t - s) * 2.0 * Math.PI / period) + 1.0;
        }

        private static double CenteredElasticOut(double t, double period = 0.4)
        {
            return Math.Pow(2.0, -10.0 * t) * Math.Sin(t * 2.0 * Math.PI / period) + 0.5;
        }

        private static double CenteredElasticIn(double t, double period = 0.4)
        {
            return -Math.Pow(2.0, 10.0 * (t - 1.0)) * Math.Sin((t - 1.0) * 2.0 * Math.PI / period) + 0.5;
        }

        internal static double LinearPoint(double x, double pIn, double pOut)
        {
            if (pIn <= 0) return pOut;
            var lowerScale = pOut / pIn;
            var upperScale = (1.0 - pOut) / (1.0 - pIn);
            var upperOff   = 1.0 - upperScale;
            return x < pIn ? x * lowerScale : x * upperScale + upperOff;
        }

        private static double EaseInQuint(double t) => t * t * t * t * t;

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            StopAnimation();
        }

        private sealed class FluidNavBarRenderOp : ICustomDrawOperation
        {
            private readonly float _w, _h, _xCenter, _normY;
            private readonly double[] _slots, _floatOff, _scaleY, _fill;
            private readonly SKPath?[] _paths;
            private readonly Color _bar, _btn, _active, _inactive;

            public Rect Bounds { get; }

            public FluidNavBarRenderOp(
                Rect bounds,
                float w, float h,
                float xCenter, float normY,
                double[] slots, double[] floatOff, double[] scaleY, double[] fill,
                SKPath?[] paths,
                Color bar, Color btn, Color active, Color inactive)
            {
                Bounds   = bounds;
                _w       = w; _h = h;
                _xCenter = xCenter; _normY = normY;
                _slots   = slots; _floatOff = floatOff;
                _scaleY  = scaleY; _fill = fill;
                _paths   = paths;
                _bar     = bar; _btn = btn;
                _active  = active; _inactive = inactive;
            }

            public bool HitTest(Point p) => false;
            public bool Equals(ICustomDrawOperation? other) => false;
            public void Dispose() { }

            public void Render(ImmediateDrawingContext context)
            {
                var lease = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
                if (lease == null) return;

                using var l = lease.Lease();
                var canvas = l.SkCanvas;

                int save = canvas.Save();
                try
                {
                    DrawBackground(canvas);
                    for (int i = 0; i < _slots.Length; i++)
                        DrawButton(canvas, i);
                }
                finally
                {
                    canvas.RestoreToCount(save);
                }
            }

            private void DrawBackground(SKCanvas canvas)
            {
                const float rTop  = 54f, rBot  = 44f;
                const float hcTop = 0.6f, hcBot = 0.5f;
                const float pcTop = 0.35f, pcBot = 0.85f;
                const float tY = -10f, bY = 54f;
                const float tD = 0f,   bD = 6f;

                float norm = (float)(LinearPoint(_normY, 0.5, 2.0) / 2.0);

                float r     = Lerp(rTop,  rBot,  norm);
                float anchr = Lerp(r * hcTop, r * hcBot, (float)LinearPoint(norm, 0.5, 0.75));
                float dipc  = Lerp(r * pcTop, r * pcBot, (float)LinearPoint(norm, 0.5, 0.80));
                float y     = Lerp(tY, bY, (float)LinearPoint(norm, 0.2, 0.70));
                float dist  = Lerp(tD, bD, (float)LinearPoint(norm, 0.5, 0.00));
                float x0    = _xCenter - dist / 2f;
                float x1    = _xCenter + dist / 2f;

                using var pathBuilder = new SKPathBuilder();
                pathBuilder.MoveTo(0, 0);
                pathBuilder.LineTo(x0 - r, 0);
                pathBuilder.CubicTo(x0 - r + anchr, 0,  x0 - dipc, y,  x0, y);
                pathBuilder.LineTo(x1, y);
                pathBuilder.CubicTo(x1 + dipc, y,  x1 + r - anchr, 0,  x1 + r, 0);
                pathBuilder.LineTo(_w, 0);
                pathBuilder.LineTo(_w, _h);
                pathBuilder.LineTo(0, _h);
                pathBuilder.Close();
                using var path = pathBuilder.Detach();

                using var paint = new SKPaint { Color = ToSK(_bar), IsAntialias = true };
                canvas.DrawPath(path, paint);
            }

            private void DrawButton(SKCanvas canvas, int i)
            {
                var cx  = (float)_slots[i];
                var cy  = _h / 2f;
                var fo  = (float)_floatOff[i];
                var sy  = (float)_scaleY[i];
                var fa  = (float)_fill[i];

                const float r = (float)CircleRadius;

                // Circle — just translated up, not scaled
                using var cp = new SKPaint { Color = ToSK(_btn), IsAntialias = true };
                canvas.DrawCircle(cx, cy - fo, r, cp);

                // Icon
                if (i < _paths.Length && _paths[i] != null)
                    DrawIcon(canvas, _paths[i]!, cx, cy - fo, sy, fa);
            }

            private void DrawIcon(SKCanvas canvas, SKPath path, float cx, float cy,
                                   float scaleY, float fillAmount)
            {
                const float s = (float)IconDrawScale;

                int save = canvas.Save();
                canvas.Translate(cx, cy);
                canvas.Scale(s, s * scaleY);

                // Grey background stroke (full path, unselected look)
                using var bg = new SKPaint
                {
                    Style       = SKPaintStyle.Stroke,
                    StrokeWidth = 2.4f,
                    StrokeCap   = SKStrokeCap.Round,
                    StrokeJoin  = SKStrokeJoin.Round,
                    Color       = ToSK(_inactive),
                    IsAntialias = true
                };
                canvas.DrawPath(path, bg);

                // Foreground stroke, trimmed progressively with SKPathMeasure
                if (fillAmount > 0f)
                {
                    using var fg = new SKPaint
                    {
                        Style       = SKPaintStyle.Stroke,
                        StrokeWidth = 2.4f,
                        StrokeCap   = SKStrokeCap.Round,
                        StrokeJoin  = SKStrokeJoin.Round,
                        Color       = ToSK(_active),
                        IsAntialias = true
                    };
                    DrawTrimmedPath(canvas, path, fillAmount, fg);
                }

                canvas.RestoreToCount(save);
            }

            // Iterates all contours and draws each trimmed to fillAmount of its length.
            // Direct port of Flutter's extractPartialPath behavior.
            private static void DrawTrimmedPath(SKCanvas canvas, SKPath path,
                                                 float fillAmount, SKPaint paint)
            {
                using var measure = new SKPathMeasure(path, false);
                do
                {
                    var len = measure.Length;
                    if (len <= 0f) continue;

                    using var segBuilder = new SKPathBuilder();
                    if (measure.GetSegment(0f, len * fillAmount, segBuilder, true))
                    {
                        using var seg = segBuilder.Detach();
                        canvas.DrawPath(seg, paint);
                    }
                }
                while (measure.NextContour());
            }

            private static float Lerp(float a, float b, float t) => a + (b - a) * t;

            private static double LinearPoint(double x, double pIn, double pOut)
            {
                if (pIn <= 0) return pOut;
                var lo = pOut / pIn;
                var hi = (1.0 - pOut) / (1.0 - pIn);
                return x < pIn ? x * lo : x * hi + (1.0 - hi);
            }

            private static SKColor ToSK(Color c) => new SKColor(c.R, c.G, c.B, c.A);
        }
    }
}
