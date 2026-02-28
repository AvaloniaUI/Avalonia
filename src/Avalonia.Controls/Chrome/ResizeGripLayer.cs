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

        // Corner size for usability
        var cw = Math.Max(Math.Max(gt.Left, gt.Right), 8);
        var ch = Math.Max(Math.Max(gt.Top, gt.Bottom), 8);

        // Edges — positioned at the outer edges, NOT overlapping client area
        _top.Arrange(new Rect(cw, 0, Math.Max(0, w - 2 * cw), gt.Top));
        _bottom.Arrange(new Rect(cw, h - gt.Bottom, Math.Max(0, w - 2 * cw), gt.Bottom));
        _left.Arrange(new Rect(0, ch, gt.Left, Math.Max(0, h - 2 * ch)));
        _right.Arrange(new Rect(w - gt.Right, ch, gt.Right, Math.Max(0, h - 2 * ch)));

        // Corners
        _topLeft.Arrange(new Rect(0, 0, cw, ch));
        _topRight.Arrange(new Rect(w - cw, 0, cw, ch));
        _bottomLeft.Arrange(new Rect(0, h - ch, cw, ch));
        _bottomRight.Arrange(new Rect(w - cw, h - ch, cw, ch));

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
