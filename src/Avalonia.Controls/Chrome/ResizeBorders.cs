using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Reactive;

namespace Avalonia.Controls.Chrome
{
    /// <summary>
    /// Draws resize borders when managed client decorations are enabled.
    /// </summary>
    [TemplatePart("PART_BorderTopLeft", typeof(Rectangle))]
    [TemplatePart("PART_BorderTop", typeof(Rectangle))]
    [TemplatePart("PART_BorderTopRight", typeof(Rectangle))]
    [TemplatePart("PART_BorderRight", typeof(Rectangle))]
    [TemplatePart("PART_BorderBottomRight", typeof(Rectangle))]
    [TemplatePart("PART_BorderBottom", typeof(Rectangle))]
    [TemplatePart("PART_BorderBottomLeft", typeof(Rectangle))]
    [TemplatePart("PART_BorderLeft", typeof(Rectangle))]
    public class ResizeBorders : TemplatedControl
    {
        private CompositeDisposable? _disposables;
        private Window? _window;

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            if (_window is null)
                return;

            var borderTopLeft = e.NameScope.Get<Rectangle>("PART_BorderTopLeft");
            var borderTop = e.NameScope.Get<Rectangle>("PART_BorderTop");
            var borderTopRight = e.NameScope.Get<Rectangle>("PART_BorderTopRight");
            var borderRight = e.NameScope.Get<Rectangle>("PART_BorderRight");
            var borderBottomRight = e.NameScope.Get<Rectangle>("PART_BorderBottomRight");
            var borderBottom = e.NameScope.Get<Rectangle>("PART_BorderBottom");
            var borderBottomLeft = e.NameScope.Get<Rectangle>("PART_BorderBottomLeft");
            var borderLeft = e.NameScope.Get<Rectangle>("PART_BorderLeft");

            SetupSide(_window, borderTopLeft, StandardCursorType.TopLeftCorner, WindowEdge.NorthWest);
            SetupSide(_window, borderTop, StandardCursorType.TopSide, WindowEdge.North);
            SetupSide(_window, borderTopRight, StandardCursorType.TopRightCorner, WindowEdge.NorthEast);
            SetupSide(_window, borderRight, StandardCursorType.RightSide, WindowEdge.East);
            SetupSide(_window, borderBottomRight, StandardCursorType.BottomRightCorner, WindowEdge.SouthEast);
            SetupSide(_window, borderBottom, StandardCursorType.BottomSide, WindowEdge.South);
            SetupSide(_window, borderBottomLeft, StandardCursorType.BottomLeftCorner, WindowEdge.SouthWest);
            SetupSide(_window, borderLeft, StandardCursorType.LeftSide, WindowEdge.West);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (e.Root is Window window)
            {
                _window = window;
                _disposables = new CompositeDisposable
                {
                    window.GetObservable(Window.OffScreenMarginProperty)
                        .Subscribe(_ => UpdateSize(window)),
                    window.GetObservable(Window.ExtendClientAreaChromeHintsProperty)
                        .Subscribe(_ => UpdateSize(window)),
                    window.GetObservable(Window.WindowStateProperty)
                        .Subscribe(_ => UpdateSize(window)),
                    window.GetObservable(Window.IsExtendedIntoWindowDecorationsProperty)
                        .Subscribe(_ => UpdateSize(window)),
                    window.GetObservable(Window.SystemDecorationsProperty)
                        .Subscribe(_ => UpdateSize(window))
                };
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            _disposables?.Dispose();
        }

        private void UpdateSize(Window window)
        {
            Margin = new Thickness(
                window.OffScreenMargin.Left,
                window.OffScreenMargin.Top,
                window.OffScreenMargin.Right,
                window.OffScreenMargin.Bottom);

            IsVisible = window is { WindowState: WindowState.Normal, IsExtendedIntoWindowDecorations: true, SystemDecorations: >= SystemDecorations.BorderOnly };
        }

        private static void SetupSide(Window window, InputElement border, StandardCursorType cursor, WindowEdge edge)
        {
            border.Cursor = new Cursor(cursor);
            border.PointerPressed += (_, e) => window.BeginResizeDrag(edge, e);
        }
    }
}
