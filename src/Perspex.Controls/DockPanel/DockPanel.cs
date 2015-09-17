namespace Perspex.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Layout;

    // ReSharper disable once UnusedMember.Global
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class DockPanel : Panel
    {
        public static readonly PerspexProperty<Dock> DockProperty = PerspexProperty.RegisterAttached<DockPanel, Control, Dock>("Dock");

        static DockPanel()
        {
            AffectsArrange(DockProperty);
        }

        public static Dock GetDock(PerspexObject element)
        {
            return element.GetValue(DockProperty);
        }

        public static void SetDock(PerspexObject element, Dock dock)
        {
            element.SetValue(DockProperty, dock);
        }

        public static readonly PerspexProperty<bool> LastChildFillProperty = PerspexProperty.Register<DockPanel, bool>(nameof(DataContext), defaultValue: true);

        public bool LastChildFill
        {
            get { return GetValue(LastChildFillProperty); }
            set { SetValue(LastChildFillProperty, value); }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (!LastChildFill)
            {
                return MeasureItemsThatWillBeDocked(availableSize, Children);
            }

            var sizeRequiredByDockingItems = MeasureItemsThatWillBeDocked(availableSize, Children.WithoutLast());
            var elementThatWillFill = Children.Last();
            elementThatWillFill.Measure(availableSize - sizeRequiredByDockingItems);
            var finalSize = sizeRequiredByDockingItems.Inflate(new Thickness(elementThatWillFill.DesiredSize.Width, elementThatWillFill.DesiredSize.Height));
            return finalSize;
        }

        private static Size MeasureItemsThatWillBeDocked(Size availableSize, IEnumerable<IControl> children)
        {
            var requiredHorizontalLength = 0D;
            var requiredVerticalLength = 0D;

            foreach (var control in children)
            {
                control.Measure(availableSize);

                var dock = control.GetValue(DockProperty);
                if (IsHorizontal(dock))
                {
                    requiredHorizontalLength += control.DesiredSize.Width;
                }
                else
                {
                    requiredVerticalLength += control.DesiredSize.Height;
                }
            }

            return new Size(requiredHorizontalLength, requiredVerticalLength);
        }

        private static bool IsHorizontal(Dock dock)
        {
            return dock == Dock.Left || dock == Dock.Right;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var docker = new DockingArranger();

            if (!LastChildFill)
            {
                return docker.ArrangeChildren(finalSize, Children);
            }

            var requiredSize = docker.ArrangeChildren(finalSize, Children.WithoutLast());

            ArrangeToFill(finalSize, docker.Margins, Children.Last());

            return requiredSize;
        }

        private static void ArrangeToFill(Size availableSize, Margins margins, ILayoutable layoutable)
        {
            var containerRect = new Rect(new Point(0,0), availableSize);
            var marginsCutout = margins.AsThickness();
            var withoutMargins = containerRect.Deflate(marginsCutout);

            var finalSize = GetConstrainedSize(layoutable, withoutMargins);

            var finalRect = withoutMargins.AlignChild(finalSize, Alignment.Middle, Alignment.Middle);

            layoutable.Arrange(finalRect);
        }

        private static Size GetConstrainedSize(ILayoutable layoutable, Rect withoutMargins)
        {
            var width = GetWidth(layoutable.GetLayoutSizes(), withoutMargins);
            var height = GetWidth(layoutable.GetLayoutSizes().Swap(), withoutMargins.Swap());
            var finalSize = new Size(width, height);
            return finalSize;
        }

        private static double GetWidth(LayoutSizes layoutSizes, Rect withoutMargins)
        {
            return layoutSizes.IsWidthSpecified
                ? layoutSizes.Size.Width 
                : GetConstrainedDimension(withoutMargins.Width, layoutSizes.MaxSize.Width, layoutSizes.MinSize.Width);
        }

        private static double GetConstrainedDimension(double toConstrain, double maximum, double minimum)
        {
            return Math.Max(Math.Min(toConstrain, maximum), minimum);
        }
    }
}