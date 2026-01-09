using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Math = System.Math;

namespace ControlCatalog.Pages;

public partial class CompositionPage : UserControl
{
    private CompositionCustomVisual? _customVisual;
    private CompositionSolidColorVisual? _solidVisual;

    public CompositionPage()
    {
        InitializeComponent();
        AttachAnimatedSolidVisual(SolidVisualHost);
        AttachCustomVisual(CustomVisualHost);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Items.ItemsSource = CreateColorItems();
    }

    private static List<CompositionPageColorItem> CreateColorItems()
    {
        var list = new List<CompositionPageColorItem>();

        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 255, 185, 0)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 231, 72, 86)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 0, 120, 215)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 0, 153, 188)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 122, 117, 116)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 118, 118, 118)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 255, 141, 0)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 232, 17, 35)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 0, 99, 177)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 45, 125, 154)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 93, 90, 88)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 76, 74, 72)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 247, 99, 12)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 234, 0, 94)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 142, 140, 216)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 0, 183, 195)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 104, 118, 138)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 105, 121, 126)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 202, 80, 16)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 195, 0, 82)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 107, 105, 214)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 3, 131, 135)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 81, 92, 107)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 74, 84, 89)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 218, 59, 1)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 227, 0, 140)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 135, 100, 184)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 0, 178, 148)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 86, 124, 115)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 100, 124, 100)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 239, 105, 80)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 191, 0, 119)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 116, 77, 169)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 1, 133, 116)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 72, 104, 96)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 82, 94, 84)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 209, 52, 56)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 194, 57, 179)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 177, 70, 194)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 0, 204, 106)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 73, 130, 5)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 132, 117, 69)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 255, 67, 67)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 154, 0, 137)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 136, 23, 152)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 16, 137, 62)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 16, 124, 16)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 126, 115, 95)));

        return list;
    }
    
    void AttachAnimatedSolidVisual(Visual v)
    {
        void Update()
        {
            if(_solidVisual == null)
                return;
            _solidVisual.Size = new (v.Bounds.Width / 3, v.Bounds.Height / 3);
            _solidVisual.Offset = new (v.Bounds.Width / 3, v.Bounds.Height / 3, 0);
        }
        v.AttachedToVisualTree += delegate
        {
            var compositor = ElementComposition.GetElementVisual(v)?.Compositor;
            if(compositor == null || _solidVisual?.Compositor == compositor)
                return;
            _solidVisual = compositor.CreateSolidColorVisual();
            ElementComposition.SetElementChildVisual(v, _solidVisual);
            _solidVisual.Color = Colors.Red;
            var animation = _solidVisual.Compositor.CreateColorKeyFrameAnimation();
            animation.InsertKeyFrame(0, Colors.Red);
            animation.InsertKeyFrame(0.5f, Colors.Blue);
            animation.InsertKeyFrame(1, Colors.Green);
            animation.Duration = TimeSpan.FromSeconds(5);
            animation.IterationBehavior = AnimationIterationBehavior.Forever;
            animation.Direction = PlaybackDirection.Alternate;
            _solidVisual.StartAnimation("Color", animation);

            _solidVisual.AnchorPoint = new (0, 0);

            var scale = _solidVisual.Compositor.CreateVector3KeyFrameAnimation();
            scale.Duration = TimeSpan.FromSeconds(5);
            scale.IterationBehavior = AnimationIterationBehavior.Forever;
            scale.InsertKeyFrame(0, new Vector3(1, 1, 0));
            scale.InsertKeyFrame(0.5f, new Vector3(1.5f, 1.5f, 0));
            scale.InsertKeyFrame(1, new Vector3(1, 1, 0));

            _solidVisual.StartAnimation("Scale", scale);

            var center =
                _solidVisual.Compositor.CreateExpressionAnimation(
                    "Vector3(this.Target.Size.X * 0.5, this.Target.Size.Y * 0.5, 1)");
            _solidVisual.StartAnimation("CenterPoint", center);
            Update();
        };
        v.PropertyChanged += (_, a) =>
        {
            if (a.Property == BoundsProperty)
                Update();
        };
    }

    void AttachCustomVisual(Visual v)
    {
        void Update()
        {
            if (_customVisual == null)
                return;
            var h = (float)Math.Min(v.Bounds.Height, v.Bounds.Width / 3);
            _customVisual.Size = new (v.Bounds.Width, h);
            _customVisual.Offset = new (0, (v.Bounds.Height - h) / 2, 0);
        }
        v.AttachedToVisualTree += delegate
        {
            var compositor = ElementComposition.GetElementVisual(v)?.Compositor;
            if(compositor == null || _customVisual?.Compositor == compositor)
                return;
            _customVisual = compositor.CreateCustomVisual(new CustomVisualHandler());
            ElementComposition.SetElementChildVisual(v, _customVisual);
            _customVisual.SendHandlerMessage(CustomVisualHandler.StartMessage);
            PreciseDirtyRectsCheckboxCustomVisualChanged(this, new());
            Update();
        };
        
        v.PropertyChanged += (_, a) =>
        {
            if (a.Property == BoundsProperty)
                Update();
        };
    }

    class CustomVisualHandler : CompositionCustomVisualHandler
    {
        private TimeSpan _animationElapsed;
        private TimeSpan? _lastServerTime;
        private bool _running;
        private bool _preciseDirtyRects;

        public static readonly object StopMessage = new(),
            StartMessage = new(),
            UsePreciseDirtyRects = new(),
            UseNonPreciseDirtyRects = new();

        private List<(Point center, double size, ImmutableSolidColorBrush brush)> _ellipses = new();

        void UpdateRects()
        {
            if (_running)
            {
                if (_lastServerTime.HasValue) _animationElapsed += (CompositionNow - _lastServerTime.Value);
                _lastServerTime = CompositionNow;
            }
            
            _ellipses.Clear();
            
            const int cnt = 20;
            var maxPointSizeX = EffectiveSize.X / (cnt * 1.6);
            var maxPointSizeY = EffectiveSize.Y / 4;
            var pointSize = Math.Min(maxPointSizeX, maxPointSizeY);
            var animationLength = TimeSpan.FromSeconds(4);
            var animationStage = _animationElapsed.TotalSeconds / animationLength.TotalSeconds;

            var sinOffset = Math.Cos(_animationElapsed.TotalSeconds) * 1.5;
            
            for (var c = 0; c < cnt; c++)
            {
                var stage = (animationStage + (double)c / cnt) % 1;
                var colorStage =
                    (animationStage + (Math.Sin(_animationElapsed.TotalSeconds * 2) + 1) / 2 + (double)c / cnt) % 1;
                var posX = (EffectiveSize.X + pointSize * 3) * stage - pointSize;
                var posY = (EffectiveSize.Y - pointSize) * (1 + Math.Sin(stage * 3.14 * 3 + sinOffset)) / 2 + pointSize / 2;
                var opacity = Math.Sin(stage * 3.14);

                _ellipses.Add((new Point(posX, posY), pointSize / 2, new ImmutableSolidColorBrush(Color.FromArgb(
                    255,
                    (byte)(255 - 255 * colorStage),
                    (byte)(255 * Math.Abs(0.5 - colorStage) * 2),
                    (byte)(255 * colorStage)
                ), opacity)));
            }
        }
        
        public override void OnRender(ImmediateDrawingContext drawingContext)
        {
            if (_ellipses.Count == 0)
                UpdateRects();
            
            foreach(var e in _ellipses)
                drawingContext.DrawEllipse(e.brush, null, e.center, e.size, e.size);
        }

        public override void OnMessage(object message)
        {
            if (message == StartMessage)
            {
                _running = true;
                _lastServerTime = null;
                RegisterForNextAnimationFrameUpdate();
            }
            else if (message == StopMessage)
                _running = false;
            else if (message == UsePreciseDirtyRects)
                _preciseDirtyRects = true;
            else if (message == UseNonPreciseDirtyRects)
                _preciseDirtyRects = false;
        }

        void InvalidateCurrentEllipseRects()
        {
            foreach (var e in _ellipses)
                Invalidate(new Rect(e.center.X - e.size, e.center.Y - e.size, e.size * 2, e.size * 2));
        }
        
        public override void OnAnimationFrameUpdate()
        {
            if (_running)
            {
                if (_preciseDirtyRects)
                    InvalidateCurrentEllipseRects();
                else
                    Invalidate();
                UpdateRects();
                if(_preciseDirtyRects)
                    InvalidateCurrentEllipseRects();
                RegisterForNextAnimationFrameUpdate();
            }
        }
    }
    
    private void ButtonThreadSleep(object? sender, RoutedEventArgs e)
    {
        Thread.Sleep(10000);
    }

    private void ButtonStartCustomVisual(object? sender, RoutedEventArgs e)
    {
        _customVisual?.SendHandlerMessage(CustomVisualHandler.StartMessage);
    }

    private void ButtonStopCustomVisual(object? sender, RoutedEventArgs e)
    {
        _customVisual?.SendHandlerMessage(CustomVisualHandler.StopMessage);
    }

    private void PreciseDirtyRectsCheckboxCustomVisualChanged(object sender, RoutedEventArgs e)
    {
        _customVisual?.SendHandlerMessage(PreciseDirtyRectsCheckboxCustomVisual?.IsChecked == true
            ? CustomVisualHandler.UsePreciseDirtyRects
            : CustomVisualHandler.UseNonPreciseDirtyRects);
    }
}

public class CompositionPageColorItem
{
    public Color Color { get; private set; }

    public SolidColorBrush ColorBrush
    {
        get { return new SolidColorBrush(Color); }
    }

    public String ColorHexValue
    {
        get { return Color.ToString().Substring(3).ToUpperInvariant(); }
    }

    public CompositionPageColorItem(Color color)
    {
        Color = color;
    }
}
