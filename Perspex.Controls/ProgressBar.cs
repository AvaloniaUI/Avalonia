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
        protected override Size ArrangeOverride(Size finalSize)
        {
            var size = base.ArrangeOverride(finalSize);
            var b = this.Bounds;

            var indicator = this.GetTemplateChild<Border>("PART_Indicator");
            indicator.Width = Math.Max(this.Minimum, finalSize.Width * (this.Value / this.Maximum));

            return size;
        }
    }
}
