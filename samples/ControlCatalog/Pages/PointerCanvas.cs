#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace ControlCatalog.Pages;

public class PointerCanvas : Control
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private int _events;
    private IDisposable? _statusUpdated;
    private Dictionary<int, PointerPoints> _pointers = new();
    private PointerPointProperties? _lastProperties;
    private PointerUpdateKind? _lastNonOtherUpdateKind;
    class PointerPoints
    {
        struct CanvasPoint
        {
            public IBrush? Brush;
            public Point Point;
            public double Radius;
            public double? Pressure;
        }

        readonly CanvasPoint[] _points = new CanvasPoint[1000];
        int _index;

        public void Render(DrawingContext context, bool drawPoints)
        {
            CanvasPoint? prev = null;
            for (var c = 0; c < _points.Length; c++)
            {
                var i = (c + _index) % _points.Length;
                var pt = _points[i];
                var pressure = (pt.Pressure ?? prev?.Pressure ?? 0.5);
                var thickness = pressure * 10;
                var radius = pressure * pt.Radius;

                if (drawPoints)
                {
                    if (pt.Brush != null)
                    {
                        context.DrawEllipse(pt.Brush, null, pt.Point, radius, radius);
                    }
                }
                else
                {
                    if (prev.HasValue && prev.Value.Brush != null && pt.Brush != null
                        && prev.Value.Pressure != null && pt.Pressure != null)
                    {
                        var linePen = new Pen(Brushes.Black, thickness, null, PenLineCap.Round, PenLineJoin.Round);
                        context.DrawLine(linePen, prev.Value.Point, pt.Point);
                    }
                }
                prev = pt;
            }

        }

        void AddPoint(Point pt, IBrush brush, double radius, float? pressure = null)
        {
            _points[_index] = new CanvasPoint { Point = pt, Brush = brush, Radius = radius, Pressure = pressure };
            _index = (_index + 1) % _points.Length;
        }

        public void HandleEvent(PointerEventArgs e, Visual v)
        {
            e.Handled = true;
            e.PreventGestureRecognition();
            var currentPoint = e.GetCurrentPoint(v);
            if (e.RoutedEvent == PointerPressedEvent)
                AddPoint(currentPoint.Position, Brushes.Green, 10);
            else if (e.RoutedEvent == PointerReleasedEvent)
                AddPoint(currentPoint.Position, Brushes.Red, 10);
            else
            {
                var pts = e.GetIntermediatePoints(v);
                for (var c = 0; c < pts.Count; c++)
                {
                    var pt = pts[c];
                    AddPoint(pt.Position, c == pts.Count - 1 ? Brushes.Blue : Brushes.Black,
                        c == pts.Count - 1 ? 5 : 2, pt.Properties.Pressure);
                }
            }
        }
    }

    private int _threadSleep;
    public static readonly DirectProperty<PointerCanvas, int> ThreadSleepProperty =
        AvaloniaProperty.RegisterDirect<PointerCanvas, int>(nameof(ThreadSleep), c => c.ThreadSleep, (c, v) => c.ThreadSleep = v);

    public int ThreadSleep
    {
        get => _threadSleep;
        set => SetAndRaise(ThreadSleepProperty, ref _threadSleep, value);
    }

    private bool _drawOnlyPoints;
    public static readonly DirectProperty<PointerCanvas, bool> DrawOnlyPointsProperty =
        AvaloniaProperty.RegisterDirect<PointerCanvas, bool>(nameof(DrawOnlyPoints), c => c.DrawOnlyPoints, (c, v) => c.DrawOnlyPoints = v);

    public bool DrawOnlyPoints
    {
        get => _drawOnlyPoints;
        set => SetAndRaise(DrawOnlyPointsProperty, ref _drawOnlyPoints, value);
    }

    private string? _status;
    public static readonly DirectProperty<PointerCanvas, string?> StatusProperty =
        AvaloniaProperty.RegisterDirect<PointerCanvas, string?>(nameof(Status), c => c.Status, (c, v) => c.Status = v,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public string? Status
    {
        get => _status;
        set => SetAndRaise(StatusProperty, ref _status, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _statusUpdated = DispatcherTimer.Run(() =>
        {
            if (_stopwatch.Elapsed.TotalMilliseconds > 250)
            {
                Status = $@"Events per second: {(_events / _stopwatch.Elapsed.TotalSeconds)}
PointerUpdateKind: {_lastProperties?.PointerUpdateKind}
Last PointerUpdateKind != Other: {_lastNonOtherUpdateKind}
IsLeftButtonPressed: {_lastProperties?.IsLeftButtonPressed}
IsRightButtonPressed: {_lastProperties?.IsRightButtonPressed}
IsMiddleButtonPressed: {_lastProperties?.IsMiddleButtonPressed}
IsXButton1Pressed: {_lastProperties?.IsXButton1Pressed}
IsXButton2Pressed: {_lastProperties?.IsXButton2Pressed}
IsBarrelButtonPressed: {_lastProperties?.IsBarrelButtonPressed}
IsEraser: {_lastProperties?.IsEraser}
IsInverted: {_lastProperties?.IsInverted}
Pressure: {_lastProperties?.Pressure}
XTilt: {_lastProperties?.XTilt}
YTilt: {_lastProperties?.YTilt}
Twist: {_lastProperties?.Twist}";
                _stopwatch.Restart();
                _events = 0;
            }

            return true;
        }, TimeSpan.FromMilliseconds(10));
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        _statusUpdated?.Dispose();
    }

    void HandleEvent(PointerEventArgs e)
    {
        _events++;

        if (_threadSleep != 0)
        {
            Thread.Sleep(_threadSleep);
        }
        InvalidateVisual();

        var lastPointer = e.GetCurrentPoint(this);
        _lastProperties = lastPointer.Properties;

        if (_lastProperties?.PointerUpdateKind != PointerUpdateKind.Other)
        {
            _lastNonOtherUpdateKind = _lastProperties?.PointerUpdateKind;
        }

        if (e.RoutedEvent == PointerReleasedEvent && e.Pointer.Type == PointerType.Touch)
        {
            _pointers.Remove(e.Pointer.Id);
            return;
        }

        if (e.Pointer.Type != PointerType.Pen
            || lastPointer.Properties.Pressure > 0)
        {
            if (!_pointers.TryGetValue(e.Pointer.Id, out var pt))
                _pointers[e.Pointer.Id] = pt = new PointerPoints();
            pt.HandleEvent(e, this);
        }
    }

    public override void Render(DrawingContext context)
    {
        context.FillRectangle(Brushes.White, Bounds);
        foreach (var pt in _pointers.Values)
            pt.Render(context, _drawOnlyPoints);
        base.Render(context);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            _pointers.Clear();
            InvalidateVisual();
            return;
        }

        HandleEvent(e);
        base.OnPointerPressed(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        HandleEvent(e);
        base.OnPointerMoved(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        HandleEvent(e);
        base.OnPointerReleased(e);
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        _lastProperties = null;
        base.OnPointerCaptureLost(e);
    }
}
