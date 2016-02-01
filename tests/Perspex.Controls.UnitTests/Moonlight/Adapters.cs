using System;
using Perspex.Controls.Presenters;
using Perspex.Controls.Templates;
using Perspex.Layout;
using Perspex.Media;
using Perspex.VisualTree;
using Xunit;

namespace Perspex.Controls.UnitTests.Moonlight
{
    internal class TestClass : Attribute
    {
    }

    internal class TestMethod : FactAttribute
    {
    }

    internal class MinRuntimeVersionAttribute : Attribute
    {
        public MinRuntimeVersionAttribute(int value)
        {

        }
    }

    internal class MoonlightBugAttribute : Attribute
    {
        public MoonlightBugAttribute(string message = null)
        {
        }
    }

    internal class SilverlightBugAttribute : Attribute
    {
        public SilverlightBugAttribute(string message)
        {
        }
    }

    internal class AsynchronousAttribute : Attribute
    {
    }

    internal class Border : Perspex.Controls.Border
    {
        public double ActualWidth => Bounds.Width;
        public double ActualHeight => Bounds.Height;
        public Size RenderSize => Bounds.Size;
    }

    internal class ContentControl : Perspex.Controls.ContentControl
    {
        public ContentControl()
        {
            Template = new FuncControlTemplate<MyContentControl>(parent =>
            {
                return new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = parent[!MyContentControl.ContentProperty]
                };
            });
        }

        public double ActualWidth => Bounds.Width;
        public double ActualHeight => Bounds.Height;
        public Size RenderSize => Bounds.Size;

        protected override Size MeasureOverride(Size availableSize)
        {
            ((ContentPresenter)Presenter).UpdateChild();
            return base.MeasureOverride(availableSize);
        }
    }

    internal class Grid : Perspex.Controls.Grid
    {
        public static readonly StyledProperty<bool> ShowGridLinesProperty =
            PerspexProperty.Register<Grid, bool>("ShowGridLines");

        public double ActualWidth => Bounds.Width;
        public double ActualHeight => Bounds.Height;
        public Size RenderSize => Bounds.Size;
        public bool ShowGridLines { get; set; }

        public void UpdateLayout()
        {
        }
    }

    internal class Panel : Perspex.Controls.Panel
    {
        public double ActualWidth => Bounds.Width;
        public double ActualHeight => Bounds.Height;
        public Size RenderSize => Bounds.Size;
    }

    internal class Rectangle : Perspex.Controls.Shapes.Rectangle
    {
        public double ActualWidth => Bounds.Width;
        public double ActualHeight => Bounds.Height;
        public double RadiusX { get; set; }
        public double RadiusY { get; set; }
        public Size RenderSize => Bounds.Size;
    }

    internal class RectangleGeometry
    {
        public RectangleGeometry(Rect rect)
        {
            Rect = rect;
        }

        public Rect Rect { get; }
    }

    internal class RadialGradientBrush : Perspex.Media.RadialGradientBrush
    {
        public RadialGradientBrush(Color color1, Color color2)
        {
        }
    }

    internal class ScrollBar : Perspex.Controls.Primitives.ScrollBar
    {
        public double ActualWidth => Bounds.Width;
        public double ActualHeight => Bounds.Height;
        public Size RenderSize => Bounds.Size;
    }

    internal struct Thickness
    {
        private Perspex.Thickness _inner;

        public Thickness(double uniform)
        {
            _inner = new Perspex.Thickness(uniform);
        }

        public Thickness(double left, double top, double right, double bottom)
        {
            _inner = new Perspex.Thickness(left, top, right, bottom);
        }

        public double Left
        {
            get { return _inner.Left; }
            set { _inner = new Perspex.Thickness(value, _inner.Top, _inner.Right, _inner.Bottom); }
        }

        public double Top
        {
            get { return _inner.Top; }
            set { _inner = new Perspex.Thickness(_inner.Left, value, _inner.Right, _inner.Bottom); }
        }

        public double Right
        {
            get { return _inner.Right; }
            set { _inner = new Perspex.Thickness(_inner.Left, _inner.Top, value, _inner.Bottom); }
        }

        public double Bottom
        {
            get { return _inner.Bottom; }
            set { _inner = new Perspex.Thickness(_inner.Left, _inner.Top, _inner.Right, value); }
        }

        public static implicit operator Perspex.Thickness(Thickness o)
        {
            return o._inner;
        }
    }

    internal static class AdapterExtensions
    {
        public static IControl FindName(this IControl control, string name)
        {
            return control.FindControl<Control>(name);
        }

        public static void InvalidateSubtree(this IControl control)
        {
            foreach (IControl i in control.GetSelfAndVisualDescendents())
            {
                i.InvalidateArrange();
                i.InvalidateMeasure();
            }
        }
    }

    internal static class LayoutInformation
    {
        public static Rect GetLayoutSlot(IControl control)
        {
            return control.Bounds;
        }

        public static RectangleGeometry GetLayoutClip(ILayoutable control)
        {
            if (control.LayoutClip.HasValue)
            {
                return new RectangleGeometry(control.LayoutClip.Value);
            }
            else
            {
                return null;
            }
        }
    }

    internal static class VisualTreeHelper
    {
        public static int GetChildrenCount(IVisual control)
        {
            return control.VisualChildren.Count;
        }

        public static IVisual GetChild(IVisual control, int index)
        {
            return control.VisualChildren[index];
        }
    }
}
