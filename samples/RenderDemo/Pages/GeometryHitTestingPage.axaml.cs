using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;

namespace RenderDemo.Pages;

public class GeometryHitTestingPage : UserControl
{
    private readonly IBrush _hitStroke = Brushes.Yellow;
    private readonly TranslateTransform _probePosition = new();
    private readonly Dictionary<Shape, IBrush?> _strokes = new();
    private readonly List<Shape> _hits = new();
    private readonly Geometry _probeGeometry;
    private readonly Panel _scene;
    private readonly Path _probe;
    private readonly TextBlock _status;

    public GeometryHitTestingPage()
    {
        InitializeComponent();

        _scene = this.Get<Panel>("Scene");
        _probe = this.Get<Path>("Probe");
        _status = this.Get<TextBlock>("Status");

        _probeGeometry = CreateProbeGeometry();
        _probeGeometry.Transform = _probePosition;
        _probe.Data = _probeGeometry;

        foreach (var shape in _scene.Children.OfType<Shape>())
        {
            if (shape != _probe)
                _strokes.Add(shape, shape.Stroke);
        }

        _scene.PointerEntered += SceneOnPointerMoved;
        _scene.PointerMoved += SceneOnPointerMoved;
        _scene.PointerExited += SceneOnPointerExited;

        UpdateStatus();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private Geometry CreateProbeGeometry()
        => new PolylineGeometry([new(0, -42), new(38, 26), new(-38, 26)], true);

    private void SceneOnPointerMoved(object? sender, PointerEventArgs e)
    {
        _probe.IsVisible = true;
        MoveProbe(e.GetPosition(_scene));
    }

    private void MoveProbe(Point position)
    {
        _probePosition.X = position.X;
        _probePosition.Y = position.Y;

        ClearHits();

        foreach (var element in _scene.GetInputElementsAt(_probeGeometry))
        {
            if (element is Shape shape && _strokes.ContainsKey(shape))
            {
                shape.Stroke = _hitStroke;
                _hits.Add(shape);
            }
        }

        UpdateStatus();
    }

    private void SceneOnPointerExited(object? sender, PointerEventArgs e)
    {
        _probe.IsVisible = false;
        ClearHits();
        UpdateStatus();
    }

    private void ClearHits()
    {
        foreach (var shape in _hits)
        {
            shape.Stroke = _strokes[shape];
        }

        _hits.Clear();
    }

    private void UpdateStatus()
    {
        if (_hits.Count == 0)
        {
            _status.Text = "No intersection";
            return;
        }

        _status.Text = "Intersecting, " + string.Join(", ", _hits.Select(shape => $"{shape.Name} ({GetIntersectionDetail(shape)})"));
    }

    private IntersectionDetail? GetIntersectionDetail(Shape shape)
    {
        if (shape.RenderedGeometry is not { } geometry || _scene.TransformToVisual(shape) is not { } transform)
        {
            return null;
        }

        var probe = CreateProbeGeometry();
        probe.Transform = new MatrixTransform(_probePosition.Value * transform);

        return geometry.FillContains(probe);
    }
}

