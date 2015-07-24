// -----------------------------------------------------------------------
// <copyright file="RangeBase.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Primitives
{
    /// <summary>
    /// Base class for controls that display a value within a range.
    /// </summary>
    public abstract class RangeBase : TemplatedControl
    {
        /// <summary>
        /// Defines the <see cref="Minimum"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> MinimumProperty =
            PerspexProperty.Register<RangeBase, double>("Minimum");

        /// <summary>
        /// Defines the <see cref="Maximum"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> MaximumProperty =
            PerspexProperty.Register<RangeBase, double>("Maximum", defaultValue: 100.0);

        /// <summary>
        /// Defines the <see cref="Value"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> ValueProperty =
            PerspexProperty.Register<RangeBase, double>("Value");

        /// <summary>
        /// Gets or sets the minimum value.
        /// </summary>
        public double Minimum
        {
            get { return this.GetValue(MinimumProperty); }
            set { this.SetValue(MinimumProperty, value); }
        }

        /// <summary>
        /// Gets or sets the maximum value.
        /// </summary>
        public double Maximum
        {
            get { return this.GetValue(MaximumProperty); }
            set { this.SetValue(MaximumProperty, value); }
        }

        /// <summary>
        /// Gets or sets the current value.
        /// </summary>
        public double Value
        {
            get { return this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }
    }
}
