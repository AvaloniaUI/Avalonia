namespace Perspex.Controls
{
    using System;
    using System.Reactive;
    using System.Reactive.Linq;
    using Perspex.Controls.Templates;
    using Perspex.Controls.Primitives;

    public class ProgressBar : RangeBase
    {
        public static readonly PerspexProperty<bool> IsIndeterminateProperty =
            PerspexProperty.Register<ProgressBar, bool>("IsIndeterminate", defaultValue: false);

        public static readonly PerspexProperty<Orientation> OrientationProperty =
            PerspexProperty.Register<ProgressBar, Orientation>("Orientation", defaultValue: Orientation.Horizontal);

        protected override Size ArrangeOverride(Size finalSize)
        {
            var size = base.ArrangeOverride(finalSize);
            var b = this.Bounds;

            var indicator = this.GetTemplateChild<Border>("PART_Indicator");
            indicator.Width = finalSize.Width * (this.Value / 100);

            return size;
        }
    }
}
