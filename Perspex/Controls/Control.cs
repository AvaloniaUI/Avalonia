namespace Perspex.Controls
{
    using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using Perspex.Layout;
using Perspex.Media;

    public enum HorizontalAlignment
    {
        Stretch,
        Left,
        Center,
        Right,
    }

    public enum VerticalAlignment
    {
        Stretch,
        Top,
        Center,
        Bottom,
    }

    public abstract class Control : Visual, ILayoutable
    {
        internal static readonly PerspexProperty<Control> ParentPropertyRW =
            PerspexProperty.Register<Control, Control>("Parent");

        public static readonly ReadOnlyPerspexProperty<Control> ParentProperty =
            new ReadOnlyPerspexProperty<Control>(ParentPropertyRW);

        public static readonly PerspexProperty<Brush> BackgroundProperty =
            PerspexProperty.Register<Control, Brush>("Background");

        public static readonly PerspexProperty<Brush> BorderBrushProperty =
            PerspexProperty.Register<Control, Brush>("BorderBrush");

        public static readonly PerspexProperty<double> BorderThicknessProperty =
            PerspexProperty.Register<Control, double>("BorderThickness");

        public static readonly PerspexProperty<HorizontalAlignment> HorizontalAlignmentProperty =
            PerspexProperty.Register<Control, HorizontalAlignment>("HorizontalAlignment");

        public static readonly PerspexProperty<VerticalAlignment> VerticalAlignmentProperty =
            PerspexProperty.Register<Control, VerticalAlignment>("VerticalAlignment");

        public static readonly PerspexProperty<Thickness> MarginProperty =
            PerspexProperty.Register<Control, Thickness>("Margin");

        public Control()
        {
            this.Classes = new ObservableCollection<string>();
            this.Styles = new ObservableCollection<Style>();
            this.GetObservableWithHistory(ParentPropertyRW).Subscribe(this.ParentChanged);
        }

        public Brush Background
        {
            get { return this.GetValue(BackgroundProperty); }
            set { this.SetValue(BackgroundProperty, value); }
        }

        public Brush BorderBrush
        {
            get { return this.GetValue(BorderBrushProperty); }
            set { this.SetValue(BorderBrushProperty, value); }
        }

        public double BorderThickness
        {
            get { return this.GetValue(BorderThicknessProperty); }
            set { this.SetValue(BorderThicknessProperty, value); }
        }

        public ObservableCollection<string> Classes
        {
            get;
            private set;
        }

        public IEnumerable<Style> Styles
        {
            get;
            set;
        }

        public Size? DesiredSize
        {
            get;
            set;
        }

        public HorizontalAlignment HorizontalAlignment
        {
            get { return this.GetValue(HorizontalAlignmentProperty); }
            set { this.SetValue(HorizontalAlignmentProperty, value); }
        }

        public VerticalAlignment VerticalAlignment
        {
            get { return this.GetValue(VerticalAlignmentProperty); }
            set { this.SetValue(VerticalAlignmentProperty, value); }
        }

        public Thickness Margin
        {
            get { return this.GetValue(MarginProperty); }
            set { this.SetValue(MarginProperty, value); }
        }

        public Control Parent
        {
            get { return this.GetValue(ParentPropertyRW); }
            internal set { this.SetValue(ParentPropertyRW, value); }
        }

        public ILayoutRoot GetLayoutRoot()
        {
            Control c = this;

            while (c != null && !(c is ILayoutRoot))
            {
                c = c.Parent;
            }

            return (ILayoutRoot)c;
        }

        public void Arrange(Rect rect)
        {
            this.Bounds = new Rect(
                rect.Position,
                this.ArrangeContent(rect.Size.Deflate(this.Margin).Constrain(rect.Size)));
        }

        public void Measure(Size availableSize)
        {
            availableSize = availableSize.Deflate(this.Margin);
            this.DesiredSize = this.MeasureContent(availableSize).Constrain(availableSize);
        }

        public void InvalidateArrange()
        {
            this.GetLayoutRoot().LayoutManager.InvalidateArrange(this);
        }

        public void InvalidateMeasure()
        {
            this.GetLayoutRoot().LayoutManager.InvalidateMeasure(this);
        }

        protected virtual Size ArrangeContent(Size finalSize)
        {
            return finalSize;
        }

        protected abstract Size MeasureContent(Size availableSize);

        private void AttachStyles(Control control)
        {
            if (control.Styles != null)
            {
                foreach (Style style in control.Styles)
                {
                    style.Attach(this);
                }
            }

            Control parent = control.Parent;
            
            if (parent != null)
            {
                this.AttachStyles(parent);
            }
        }

        private void ParentChanged(Tuple<Control, Control> values)
        {
            if (values.Item1 != null)
            {
                //this.DetatchStyles(values.Item1);
            }

            if (values.Item2 != null)
            {
                this.AttachStyles(this);
            }
        }
    }
}
