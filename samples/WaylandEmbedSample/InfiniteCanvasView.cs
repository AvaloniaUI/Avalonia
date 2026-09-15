using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Wayland.Embedding;
using Avalonia.Wayland.Embedding.Hosting;

namespace WaylandEmbedSample;

// An "infinite canvas" of live GTK tiles: 10 GTK toplevels each embedded (scenario 1) into a host control on a
// pannable/zoomable world. Each tile can be moved, resized (re-configures the real GTK window) and rotated.
// Input rides the Avalonia transforms — pointer events reach a host in its post-transform LOCAL coords, which the
// host forwards to GTK, so the embedded widgets stay interactive under pan/zoom/rotation.
internal sealed class InfiniteCanvasView : Border
{
    private const int TileCount = 10;
    private const int TileWidth = 200;
    private const int TileHeight = 150;

    private readonly Canvas _world = new();
    private readonly ScaleTransform _zoom = new(1, 1);
    private readonly TranslateTransform _pan = new(0, 0);
    private bool _initialized;

    // Background pan drag state.
    private bool _panning;
    private Point _panLastScreen;

    public InfiniteCanvasView()
    {
        Background = Brushes.DimGray; // also makes the empty area hit-testable for panning
        ClipToBounds = true;
        _world.RenderTransform = new TransformGroup { Children = { _zoom, _pan } };
        _world.RenderTransformOrigin = RelativePoint.TopLeft;
        Child = _world;

        PointerWheelChanged += OnZoom;
        PointerPressed += OnBackgroundPressed;
        PointerMoved += OnBackgroundMoved;
        PointerReleased += OnBackgroundReleased;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_initialized)
            return;
        _initialized = true;
        for (var i = 0; i < TileCount; i++)
        {
            var item = new CanvasItem(_world, i);
            Canvas.SetLeft(item, 40 + i % 5 * (TileWidth + 60));
            Canvas.SetTop(item, 40 + i / 5 * (TileHeight + 90));
            _world.Children.Add(item);
            item.EmbedGtkTile(); // create a GTK toplevel + embed it into this tile's host
        }
    }

    private void OnZoom(object? sender, PointerWheelEventArgs e)
    {
        var screen = e.GetPosition(this);
        // World point currently under the cursor (screen = world*scale + pan), kept fixed across the zoom.
        var worldX = (screen.X - _pan.X) / _zoom.ScaleX;
        var worldY = (screen.Y - _pan.Y) / _zoom.ScaleY;
        var factor = e.Delta.Y > 0 ? 1.1 : 1 / 1.1;
        var scale = Math.Clamp(_zoom.ScaleX * factor, 0.2, 5.0);
        _zoom.ScaleX = _zoom.ScaleY = scale;
        _pan.X = screen.X - worldX * scale;
        _pan.Y = screen.Y - worldY * scale;
        e.Handled = true;
    }

    private void OnBackgroundPressed(object? sender, PointerPressedEventArgs e)
    {
        // Only pan when the press landed on the empty background (an item handles its own drags first).
        if (e.Source is not Border b || !ReferenceEquals(b, this))
            return;
        _panning = true;
        _panLastScreen = e.GetPosition(this);
        e.Pointer.Capture(this);
    }

    private void OnBackgroundMoved(object? sender, PointerEventArgs e)
    {
        if (!_panning)
            return;
        var p = e.GetPosition(this);
        _pan.X += p.X - _panLastScreen.X;
        _pan.Y += p.Y - _panLastScreen.Y;
        _panLastScreen = p;
    }

    private void OnBackgroundReleased(object? sender, PointerReleasedEventArgs e)
    {
        _panning = false;
        e.Pointer.Capture(null);
    }

    // A single movable / resizable / rotatable tile hosting one embedded GTK toplevel.
    private sealed class CanvasItem : Border
    {
        private readonly Canvas _world;
        private readonly int _index;
        // Draw the GTK buffer 1:1; canvas zoom is an Avalonia RenderTransform on the world (it scales the whole tile,
        // including this 1:1 content), and the resize flush keeps the buffer in step with the tile's layout size.
        private readonly WaylandSubcompositorControlHost _host = new() { StretchContent = false };
        private readonly RotateTransform _rotate = new(0);
        private Gtk.Window? _gtk;

        private string _drag = "";          // "move" | "rotate" | "resize" | ""
        private Point _grabWorld;            // pointer-at-press, world coords (move)
        private double _grabLeft, _grabTop;  // item position at press (move)
        private double _grabAngle, _grabPointerAngle; // rotate baseline

        public CanvasItem(Canvas world, int index)
        {
            _world = world;
            _index = index;
            Width = TileWidth;
            Height = TileHeight + 26;
            BorderBrush = Brushes.Gainsboro;
            BorderThickness = new Thickness(1);
            Background = Brushes.Black;
            RenderTransform = _rotate;
            RenderTransformOrigin = RelativePoint.Center;

            var titleBar = new Border
            {
                Height = 26,
                Background = Brushes.SteelBlue,
                Child = new TextBlock
                {
                    Text = $"GTK tile #{index}  (drag · ⟳ corner · ⤡ corner)",
                    Foreground = Brushes.White,
                    Margin = new Thickness(6, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 11,
                },
            };
            titleBar.PointerPressed += (_, e) => BeginDrag("move", e);

            var hostFrame = new Border { Child = _host };
            Grid.SetRow(hostFrame, 1);

            var rotateHandle = Handle(Brushes.Orange, HorizontalAlignment.Left, "rotate");
            var resizeHandle = Handle(Brushes.LimeGreen, HorizontalAlignment.Right, "resize");
            Grid.SetRow(rotateHandle, 1);
            Grid.SetRow(resizeHandle, 1);

            var grid = new Grid { RowDefinitions = new RowDefinitions("26,*") };
            Grid.SetRow(titleBar, 0);
            grid.Children.Add(titleBar);
            grid.Children.Add(hostFrame);
            grid.Children.Add(rotateHandle);
            grid.Children.Add(resizeHandle);
            Child = grid;

            PointerMoved += OnPointerMoved;
            PointerReleased += OnPointerReleased;
        }

        private Border Handle(IBrush color, HorizontalAlignment side, string mode)
        {
            var h = new Border
            {
                Width = 14,
                Height = 14,
                Background = color,
                CornerRadius = new CornerRadius(7),
                HorizontalAlignment = side,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(2),
            };
            h.PointerPressed += (_, e) => { BeginDrag(mode, e); e.Handled = true; };
            return h;
        }

        public void EmbedGtkTile()
        {
            _gtk = SampleGtk.CanvasTile(_index, TileWidth, TileHeight);
            GtkClientGlue.Embed(_host, _gtk); // shows the tile window + embeds it into this tile's host
        }

        private void BeginDrag(string mode, PointerPressedEventArgs e)
        {
            _drag = mode;
            _grabWorld = e.GetPosition(_world);
            _grabLeft = Canvas.GetLeft(this);
            _grabTop = Canvas.GetTop(this);
            _grabAngle = _rotate.Angle;
            _grabPointerAngle = AngleFromCenter(_grabWorld);
            e.Pointer.Capture(this);
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            switch (_drag)
            {
                case "move":
                    var w = e.GetPosition(_world);
                    Canvas.SetLeft(this, _grabLeft + (w.X - _grabWorld.X));
                    Canvas.SetTop(this, _grabTop + (w.Y - _grabWorld.Y));
                    break;
                case "rotate":
                    var cur = AngleFromCenter(e.GetPosition(_world));
                    _rotate.Angle = _grabAngle + (cur - _grabPointerAngle);
                    break;
                case "resize":
                    // Local coords account for the item's rotation, so the corner tracks the pointer correctly.
                    var local = e.GetPosition(this);
                    var newW = Math.Clamp(local.X, 80, 1200);
                    var newH = Math.Clamp(local.Y - 26, 60, 1000); // minus the title bar row
                    Width = newW;
                    Height = newH + 26;
                    _host.RequestResize((int)newW, (int)newH); // re-configure the real GTK window to match
                    break;
            }
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _drag = "";
            e.Pointer.Capture(null);
        }

        // Angle (degrees) of a world-space point around this item's (layout) center.
        private double AngleFromCenter(Point world)
        {
            var cx = Canvas.GetLeft(this) + Width / 2;
            var cy = Canvas.GetTop(this) + Height / 2;
            return Math.Atan2(world.Y - cy, world.X - cx) * 180 / Math.PI;
        }
    }
}
