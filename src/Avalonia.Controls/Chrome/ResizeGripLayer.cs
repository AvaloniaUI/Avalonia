using System;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Avalonia.Controls.Chrome;

/// <summary>
/// An invisible layer that provides resize grip hit-test zones at window edges.
/// Grips only cover the frame/shadow area outside the client area.
/// </summary>
internal class ResizeGripLayer : Control
{
    private readonly Control _top = CreateGrip(WindowDecorationsElementRole.ResizeN, StandardCursorType.TopSide);
    private readonly Control _bottom = CreateGrip(WindowDecorationsElementRole.ResizeS, StandardCursorType.BottomSide);
    private readonly Control _left = CreateGrip(WindowDecorationsElementRole.ResizeW, StandardCursorType.LeftSide);
    private readonly Control _right = CreateGrip(WindowDecorationsElementRole.ResizeE, StandardCursorType.RightSide);
    private readonly Control _topLeft = CreateGrip(WindowDecorationsElementRole.ResizeNW, StandardCursorType.TopLeftCorner);
    private readonly Control _topRight = CreateGrip(WindowDecorationsElementRole.ResizeNE, StandardCursorType.TopRightCorner);
    private readonly Control _bottomLeft = CreateGrip(WindowDecorationsElementRole.ResizeSW, StandardCursorType.BottomLeftCorner);
    private readonly Control _bottomRight = CreateGrip(WindowDecorationsElementRole.ResizeSE, StandardCursorType.BottomRightCorner);

    private Thickness _gripThickness;

    public ResizeGripLayer()
    {
        IsHitTestVisible = true;
        VisualChildren.Add(_top);
        VisualChildren.Add(_bottom);
        VisualChildren.Add(_left);
        VisualChildren.Add(_right);
        VisualChildren.Add(_topLeft);
        VisualChildren.Add(_topRight);
        VisualChildren.Add(_bottomLeft);
        VisualChildren.Add(_bottomRight);
    }

    /// <summary>
    /// The thickness of the resize grip area at each edge.
    /// Grips are placed outside the client area (covering frame + shadow).
    /// </summary>
    internal Thickness GripThickness
    {
        get => _gripThickness;
        set
        {
            if (_gripThickness != value)
            {
                _gripThickness = value;
                InvalidateArrange();
            }
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var gt = _gripThickness;
        var w = finalSize.Width;
        var h = finalSize.Height;

        // Hide all grips when thickness is zero (e.g. maximized/fullscreen)
        var hasGrips = gt.Left > 0 || gt.Top > 0 || gt.Right > 0 || gt.Bottom > 0;
        IsHitTestVisible = hasGrips;
        if (!hasGrips)
        {
            var empty = new Rect();
            _top.Arrange(empty);
            _bottom.Arrange(empty);
            _left.Arrange(empty);
            _right.Arrange(empty);
            _topLeft.Arrange(empty);
            _topRight.Arrange(empty);
            _bottomLeft.Arrange(empty);
            _bottomRight.Arrange(empty);
            return finalSize;
        }

        // Edges fill the space between their adjacent corners
        _top.Arrange(new Rect(gt.Left, 0, Math.Max(0, w - gt.Left - gt.Right), gt.Top));
        _bottom.Arrange(new Rect(gt.Left, h - gt.Bottom, Math.Max(0, w - gt.Left - gt.Right), gt.Bottom));
        _left.Arrange(new Rect(0, gt.Top, gt.Left, Math.Max(0, h - gt.Top - gt.Bottom)));
        _right.Arrange(new Rect(w - gt.Right, gt.Top, gt.Right, Math.Max(0, h - gt.Top - gt.Bottom)));

        // Corners use the thickness of their adjacent edges
        _topLeft.Arrange(new Rect(0, 0, gt.Left, gt.Top));
        _topRight.Arrange(new Rect(w - gt.Right, 0, gt.Right, gt.Top));
        _bottomLeft.Arrange(new Rect(0, h - gt.Bottom, gt.Left, gt.Bottom));
        _bottomRight.Arrange(new Rect(w - gt.Right, h - gt.Bottom, gt.Right, gt.Bottom));

        return finalSize;
    }

    private static Control CreateGrip(WindowDecorationsElementRole role, StandardCursorType cursorType)
    {
        var grip = new Border
        {
            Background = Brushes.Transparent,
            Cursor = new Cursor(cursorType),
            IsHitTestVisible = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        WindowDecorationProperties.SetElementRole(grip, role);
        return grip;
    }
}
