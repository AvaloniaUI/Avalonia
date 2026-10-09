#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

namespace ControlCatalog.Pages;

/// <summary>
/// Draws every pointer point it receives, including hover and intermediate points,
/// with input rate, batching and delay statistics.
/// </summary>
public partial class PointerCanvas : Control
{
    private const int MaxPoints = 20000;
    private const double ContactRadius = 12;
    private static readonly TimeSpan s_loadPeriod = TimeSpan.FromMilliseconds(100);

    [GeneratedStyledProperty]
    public partial int ThreadSleep { get; set; }

    [GeneratedStyledProperty]
    public partial int LoadWork { get; set; }

    /// <summary>One-line summary: input and frame rates, average delay.</summary>
    [GeneratedDirectProperty]
    public partial string? Status { get; private set; }

    /// <summary>Batching and frame details plus the last pointer's properties, always the same number of lines.</summary>
    [GeneratedDirectProperty]
    public partial string? StatusDetails { get; private set; }

    private enum PointKind
    {
        Pressed,
        Intermediate,
        Primary,
        Released
    }

    private record struct CanvasPoint(PointKind Kind, Point Position, bool InContact);

    // Messages to the render thread handler.
    private sealed record ContactMessage(int PointerId, Point? Position);
    private static readonly object s_clearMessage = new();

    /// <summary>Frame statistics written by the render thread and collected by the UI thread.</summary>
    private sealed class FrameStats
    {
        public int Frames;
        public int Events;
        public int MaxEventsPerFrame;
    }

    private readonly HashSet<int> _contacts = new();
    private CompositionCustomVisual? _pointsVisual;
    private readonly FrameStats _frameStats = new();
    private TopLevel? _topLevel;

    private readonly Stopwatch _stopwatch = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private int _events;
    private int _points;
    private int _maxBatch;
    private double _delaySum;
    private double _maxDelay;
    private double? _minClockOffset;
    private PointerPointProperties? _lastProperties;
    private PointerUpdateKind? _lastNonOtherUpdateKind;
    private IDisposable? _statusTimer;
    private DispatcherTimer? _loadTimer;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);

        if (ElementComposition.GetElementVisual(this)?.Compositor is { } compositor)
        {
            _pointsVisual = compositor.CreateCustomVisual(new PointsHandler(_frameStats));
            _pointsVisual.Size = new Vector(Bounds.Width, Bounds.Height);
            ElementComposition.SetElementChildVisual(this, _pointsVisual);
        }

        _stopwatch.Restart();
        _statusTimer = DispatcherTimer.Run(() =>
        {
            UpdateStatus();
            return true;
        }, TimeSpan.FromMilliseconds(250));
        UpdateStatus();
        UpdateLoadTimer();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        ElementComposition.SetElementChildVisual(this, null);
        _pointsVisual = null;
        _topLevel = null;
        _contacts.Clear();

        _statusTimer?.Dispose();
        _statusTimer = null;
        UpdateLoadTimer();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LoadWorkProperty)
            UpdateLoadTimer();
        else if (change.Property == BoundsProperty && _pointsVisual is not null)
            _pointsVisual.Size = new Vector(Bounds.Width, Bounds.Height);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.ClickCount == 2)
        {
            Clear();
            return;
        }

        HandleEvent(e, PointKind.Pressed);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        HandleEvent(e, PointKind.Primary);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        HandleEvent(e, PointKind.Released);
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        RemoveContact(e.Pointer.Id);
    }

    public void Clear()
    {
        _contacts.Clear();
        _pointsVisual?.SendHandlerMessage(s_clearMessage);
        _minClockOffset = null;
    }

    private void HandleEvent(PointerEventArgs e, PointKind kind)
    {
        e.Handled = true;
        e.PreventGestureRecognition();

        _events++;
        TrackDelay(e.Timestamp);

        if (ThreadSleep > 0)
            Thread.Sleep(ThreadSleep);

        // Positions come in top-level coordinates and are transformed once per event.
        var toCanvas = _topLevel?.TransformToVisual(this) ?? Matrix.Identity;
        var current = e.GetCurrentPoint(null);
        var position = current.Position.Transform(toCanvas);
        _lastProperties = current.Properties;
        if (current.Properties.PointerUpdateKind != PointerUpdateKind.Other)
            _lastNonOtherUpdateKind = current.Properties.PointerUpdateKind;

        CanvasPoint[] points;
        if (kind == PointKind.Primary)
        {
            var inContact = IsInContact(current.Properties);
            var rawPoints = e.GetIntermediatePoints(null);
            points = new CanvasPoint[rawPoints.Count];
            for (var i = 0; i < points.Length; i++)
            {
                points[i] = new CanvasPoint(i == points.Length - 1 ? PointKind.Primary : PointKind.Intermediate,
                    rawPoints[i].Position.Transform(toCanvas), inContact);
            }

            if (_contacts.Contains(e.Pointer.Id))
                _pointsVisual?.SendHandlerMessage(new ContactMessage(e.Pointer.Id, position));
        }
        else
        {
            // The release point still ends the stroke, even though no button is pressed anymore.
            points = [new CanvasPoint(kind, position, true)];

            if (kind == PointKind.Pressed)
            {
                _contacts.Add(e.Pointer.Id);
                _pointsVisual?.SendHandlerMessage(new ContactMessage(e.Pointer.Id, position));
            }
            else
            {
                RemoveContact(e.Pointer.Id);
            }
        }

        _pointsVisual?.SendHandlerMessage(points);
        _points += points.Length;
        _maxBatch = Math.Max(_maxBatch, points.Length);
    }

    private void RemoveContact(int pointerId)
    {
        if (_contacts.Remove(pointerId))
            _pointsVisual?.SendHandlerMessage(new ContactMessage(pointerId, null));
    }

    private static bool IsInContact(PointerPointProperties properties) =>
        properties.IsLeftButtonPressed || properties.IsRightButtonPressed || properties.IsMiddleButtonPressed;

    /// <summary>
    /// Event timestamps come from a platform clock that can't be compared with ours directly,
    /// so the delay is measured relative to the fastest delivered event seen so far.
    /// </summary>
    private void TrackDelay(ulong timestamp)
    {
        var offset = _clock.Elapsed.TotalMilliseconds - timestamp;
        if (_minClockOffset is not { } min || offset < min)
            _minClockOffset = min = offset;

        var delay = offset - min;
        _delaySum += delay;
        _maxDelay = Math.Max(_maxDelay, delay);
    }

    private void UpdateLoadTimer()
    {
        _loadTimer?.Stop();
        _loadTimer = null;
        if (LoadWork <= 0 || _statusTimer is null)
            return;

        // Leave a gap in every period, so the load can't lock the UI completely.
        var work = TimeSpan.FromMilliseconds(Math.Min(LoadWork, s_loadPeriod.TotalMilliseconds - 10));
        _loadTimer = new DispatcherTimer(s_loadPeriod, DispatcherPriority.Default, (_, _) =>
        {
            // Busy-wait instead of sleeping: Thread.Sleep isn't available on single-threaded browser.
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < work)
                Thread.SpinWait(100);
        });
        _loadTimer.Start();
    }

    private void UpdateStatus()
    {
        var seconds = Math.Max(_stopwatch.Elapsed.TotalSeconds, 0.001);
        int frames, frameEvents, maxEventsPerFrame;
        lock (_frameStats)
        {
            (frames, frameEvents, maxEventsPerFrame) = (_frameStats.Frames, _frameStats.Events, _frameStats.MaxEventsPerFrame);
            _frameStats.Frames = _frameStats.Events = _frameStats.MaxEventsPerFrame = 0;
        }

        var avgDelay = _events == 0 ? 0 : _delaySum / _events;
        Status = $"{_events / seconds:F0} events/s · {_points / seconds:F0} points/s · {frames / seconds:F0} frames/s · delay {avgDelay:F1} ms";

        var details =
            $"""
             Points per event: avg {(_events == 0 ? 0 : (double)_points / _events):F1}, max {_maxBatch}
             Events per frame: avg {(frames == 0 ? 0 : (double)frameEvents / frames):F1}, max {maxEventsPerFrame} · delay max {_maxDelay:F1} ms
             """;
        if (_lastProperties is { } p)
        {
            details +=
                $"""

                 Update: {p.PointerUpdateKind} (last non-Other: {_lastNonOtherUpdateKind}) · buttons: {FormatFlags([
                            (p.IsLeftButtonPressed, "Left"), (p.IsRightButtonPressed, "Right"), (p.IsMiddleButtonPressed, "Middle"),
                            (p.IsXButton1Pressed, "X1"), (p.IsXButton2Pressed, "X2"), (p.IsBarrelButtonPressed, "Barrel")])} · pen: {FormatFlags([(p.IsEraser, "Eraser"), (p.IsInverted, "Inverted")])}
                 Pressure {p.Pressure:F3} · tilt {p.XTilt:F0}/{p.YTilt:F0} · twist {p.Twist:F0}
                 """;
        }
        else
        {
            details +=
                """

                Update: – · buttons: – · pen: –
                Pressure – · tilt – · twist –
                """;
        }
        StatusDetails = details;

        _stopwatch.Restart();
        _events = _points = _maxBatch = 0;
        _delaySum = _maxDelay = 0;

        static string FormatFlags((bool isSet, string name)[] flags)
        {
            var result = "";
            foreach (var (isSet, name) in flags)
                if (isSet)
                    result += result.Length == 0 ? name : ", " + name;
            return result.Length == 0 ? "none" : result;
        }
    }

    public override void Render(DrawingContext context)
    {
        context.FillRectangle(Brushes.White, new Rect(Bounds.Size));
    }

    /// <summary>
    /// Runs on the render thread: keeps the point history and redraws only invalidated areas.
    /// </summary>
    private sealed class PointsHandler(FrameStats frameStats) : CompositionCustomVisualHandler
    {
        private static readonly IImmutableBrush s_hoverBrush = new ImmutableSolidColorBrush(Colors.LightGray);
        private static readonly IImmutableBrush s_pressedBrush = new ImmutableSolidColorBrush(Colors.Green);
        private static readonly IImmutableBrush s_releasedBrush = new ImmutableSolidColorBrush(Colors.Red);
        private static readonly IImmutableBrush s_primaryBrush = new ImmutableSolidColorBrush(Colors.Blue);
        private static readonly IImmutableBrush s_intermediateBrush = new ImmutableSolidColorBrush(Colors.Black);
        private static readonly ImmutablePen[] s_contactPens =
        [
            new(Colors.DodgerBlue.ToUInt32(), 2), new(Colors.Crimson.ToUInt32(), 2), new(Colors.ForestGreen.ToUInt32(), 2),
            new(Colors.Orange.ToUInt32(), 2), new(Colors.MediumPurple.ToUInt32(), 2), new(Colors.Teal.ToUInt32(), 2),
            new(Colors.Goldenrod.ToUInt32(), 2)
        ];

        // All pointers share one buffer, the oldest points are overwritten first.
        private readonly CanvasPoint[] _buffer = new CanvasPoint[MaxPoints];
        private int _next;
        private int _count;
        private readonly Dictionary<int, Point> _contacts = new();
        private int _eventsThisFrame;

        public override void OnMessage(object message)
        {
            switch (message)
            {
                case CanvasPoint[] points:
                    // One points message per input event; count the frames that had input.
                    if (_eventsThisFrame++ == 0)
                        RegisterForNextAnimationFrameUpdate();
                    AddPoints(points);
                    break;
                case ContactMessage contact:
                    if (contact.Position is { } position)
                    {
                        _contacts[contact.PointerId] = position;
                        Invalidate();
                    }
                    else if (_contacts.Remove(contact.PointerId, out _))
                    {
                        Invalidate();
                    }
                    break;
                default:
                    if (message == s_clearMessage)
                    {
                        _next = _count = 0;
                        _contacts.Clear();
                        Invalidate();
                    }
                    break;
            }
        }

        private void AddPoints(CanvasPoint[] points)
        {
            foreach (var point in points)
            {
                _buffer[_next] = point;
                _next = (_next + 1) % MaxPoints;
                _count = Math.Min(_count + 1, MaxPoints);
            }

            Invalidate();
        }

        public override void OnAnimationFrameUpdate()
        {
            lock (frameStats)
            {
                frameStats.Frames++;
                frameStats.Events += _eventsThisFrame;
                frameStats.MaxEventsPerFrame = Math.Max(frameStats.MaxEventsPerFrame, _eventsThisFrame);
            }
            _eventsThisFrame = 0;
        }

        public override void OnRender(ImmediateDrawingContext context)
        {
            var start = (_next - _count + MaxPoints) % MaxPoints;
            for (var i = 0; i < _count; i++)
            {
                var point = _buffer[(start + i) % MaxPoints];
                var (brush, radius) = GetStyle(point);
                context.DrawEllipse(brush, null, point.Position, radius, radius);
            }

            foreach (var (id, position) in _contacts)
            {
                var pen = s_contactPens[(id & int.MaxValue) % s_contactPens.Length];
                context.DrawEllipse(null, pen, position, ContactRadius, ContactRadius);
            }
        }

        // Hover points are faint; in contact: green press, red release, blue primary, black intermediate.
        private static (IImmutableBrush brush, double radius) GetStyle(CanvasPoint point) =>
            !point.InContact ? (s_hoverBrush, 1.5) : point.Kind switch
            {
                PointKind.Pressed => (s_pressedBrush, 5.0),
                PointKind.Released => (s_releasedBrush, 5.0),
                PointKind.Primary => (s_primaryBrush, 3.0),
                _ => (s_intermediateBrush, 1.5)
            };
    }
}
