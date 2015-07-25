// -----------------------------------------------------------------------
// <copyright file="ProgressBar.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Templates;
    using System;

    /// <summary>
    /// A control used to indicate the progress of an operation.
    /// </summary>
    public class ProgressBar : RangeBase
    {
        static ProgressBar()
        {
            ValueProperty.Changed.AddClassHandler<ProgressBar>(x => x.ValueChanged);
        }

        private Border indicator;

        /// <inheritdoc/>
        /// 
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
