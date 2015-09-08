





namespace Perspex.Controls
{
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Templates;

    /// <summary>
    /// A control used to indicate the progress of an operation.
    /// </summary>
    public class ProgressBar : RangeBase
    {
        private Border indicator;

        static ProgressBar()
        {
            ValueProperty.Changed.AddClassHandler<ProgressBar>(x => x.ValueChanged);
        }

        /// <inheritdoc/>
        protected override void OnTemplateApplied()
        {
            this.indicator = this.GetTemplateChild<Border>("PART_Indicator");
        }

        private void ValueChanged(PerspexPropertyChangedEventArgs e)
        {
            if (this.indicator != null)
            {
                double percent = this.Maximum == this.Minimum ? 1.0 : ((double)e.NewValue - this.Minimum) / (this.Maximum - this.Minimum);
                this.indicator.Width = this.Bounds.Width * percent;
            }
        }
    }
}
