// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Utilities;

namespace Perspex.Controls
{
    /// <summary>
    /// A control used to indicate the progress of an operation.
    /// </summary>
    public class ProgressBar : RangeBase
    {
        internal Border Indicator;

        static ProgressBar()
        {
            ValueProperty.Changed.AddClassHandler<ProgressBar>(x => x.ValueChanged);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            UpdateIndicator(finalSize);
            return base.ArrangeOverride(finalSize);
        }

        /// <inheritdoc/>
        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            Indicator = e.NameScope.Get<Border>("PART_Indicator");
            UpdateIndicator(Bounds.Size);
        }

        private void UpdateIndicator(Size bounds)
        {
            if (Indicator != null)
            {
                double percent = MathUtilities.Equal(Maximum, Minimum) ? 1.0 : (Value - Minimum) / (Maximum - Minimum);
                Indicator.Width = bounds.Width * percent;
            }
        }

        private void ValueChanged(PerspexPropertyChangedEventArgs e)
        {
            UpdateIndicator(Bounds.Size);
        }
    }
}
