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
        /// <inheritdoc/>
        /// 
        private Border indicator;
        protected override Size ArrangeOverride(Size finalSize)
        {
            var size = base.ArrangeOverride(finalSize);
            this.indicator = this.indicator ?? this.GetTemplateChild<Border>("PART_Indicator");
            
            double percent = this.Maximum == this.Minimum ? 1.0 : (this.Value - this.Minimum) / (this.Maximum - this.Minimum);
            indicator.Width = finalSize.Width * percent;

            return size;
        }
    }
}
