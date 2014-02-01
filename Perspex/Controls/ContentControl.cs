namespace Perspex.Controls
{
    using System;
    using System.Linq;

    public abstract class ContentControl : TemplatedControl
    {
        public static readonly PerspexProperty<object> ContentProperty =
            PerspexProperty.Register<ContentControl, object>("Content");

        public ContentControl()
        {
            this.GetObservable(ContentProperty).Subscribe(x =>
            {
                Control control = x as Control;

                if (control != null)
                {
                    control.SetValue(ParentPropertyRW, this);
                }
            });
        }

        public object Content
        {
            get { return this.GetValue(ContentProperty); }
            set { this.SetValue(ContentProperty, value); }
        }

        protected override Size ArrangeContent(Size finalSize)
        {
            Control child = this.VisualChildren.SingleOrDefault() as Control;

            if (child != null)
            {
                child.Arrange(new Rect(finalSize));
                return child.Bounds.Size;
            }
            else
            {
                return new Size();
            }
        }

        protected override Size MeasureContent(Size availableSize)
        {
            Control child = this.VisualChildren.SingleOrDefault() as Control;

            if (child != null)
            {
                child.Measure(availableSize);
                return child.DesiredSize.Value;
            }
            else
            {
                return new Size();
            }
        }
    }
}
