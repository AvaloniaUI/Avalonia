// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Metadata;

namespace Avalonia.Media
{
    /// <summary>
    /// Base class for brushes that draw with a gradient.
    /// </summary>
    public abstract class GradientBrush : Brush, IGradientBrush
    {
        /// <summary>
        /// Defines the <see cref="SpreadMethod"/> property.
        /// </summary>
        public static readonly StyledProperty<GradientSpreadMethod> SpreadMethodProperty =
            AvaloniaProperty.Register<GradientBrush, GradientSpreadMethod>(nameof(SpreadMethod));

        /// <summary>
        /// Defines the <see cref="GradientStops"/> property.
        /// </summary>
        public static readonly StyledProperty<IList<GradientStop>> GradientStopsProperty =
            AvaloniaProperty.Register<GradientBrush, IList<GradientStop>>(nameof(Opacity));

        /// <summary>
        /// Initializes a new instance of the <see cref="GradientBrush"/> class.
        /// </summary>
        public GradientBrush()
        {
            this.GradientStops = new List<GradientStop>();
        }

        /// <summary>
        /// Gets or sets the brush's spread method that defines how to draw a gradient that
        /// doesn't fill the bounds of the destination control.
        /// </summary>
        public GradientSpreadMethod SpreadMethod
        {
            get { return GetValue(SpreadMethodProperty); }
            set { SetValue(SpreadMethodProperty, value); }
        }

        /// <summary>
        /// Gets or sets the brush's gradient stops.
        /// </summary>
        [Content]
        public IList<GradientStop> GradientStops
        {
            get { return GetValue(GradientStopsProperty); }
            set { SetValue(GradientStopsProperty, value); }
        }
    }
}