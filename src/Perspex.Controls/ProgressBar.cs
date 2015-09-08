// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
        protected override void OnTemplateApplied()
        {
            _indicator = this.GetTemplateChild<Border>("PART_Indicator");
        }

        private void ValueChanged(PerspexPropertyChangedEventArgs e)
        {
            if (_indicator != null)
            {
                double percent = this.Maximum == this.Minimum ? 1.0 : ((double)e.NewValue - this.Minimum) / (this.Maximum - this.Minimum);
                _indicator.Width = this.Bounds.Width * percent;
            }
        }
    }
}
