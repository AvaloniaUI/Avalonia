// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;

namespace Perspex.Controls
{
    /// <summary>
    /// A control used to indicate the progress of an operation.
    /// </summary>
    public class ProgressBar : RangeBase
    {
        private Border _indicator;

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
        protected override void OnTemplateApplied()
        {
            _indicator = this.GetTemplateChild<Border>("PART_Indicator");
            UpdateIndicator(Bounds.Size);
        }

        private void UpdateIndicator(Size bounds)
        {
            if (_indicator != null)
            {
                double percent = (Maximum - Minimum) < Single.Epsilon ? 1.0 : (Value - Minimum) / (Maximum - Minimum);
                _indicator.Width = bounds.Width * percent;
            }
        }

        private void ValueChanged(PerspexPropertyChangedEventArgs e)
        {
            UpdateIndicator(Bounds.Size);
        }
    }
}
