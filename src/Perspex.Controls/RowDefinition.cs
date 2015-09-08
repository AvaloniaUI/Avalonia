// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Controls
{
    /// <summary>
    /// Holds a row definitions for a <see cref="Grid"/>.
    /// </summary>
    public class RowDefinition : DefinitionBase
    {
        /// <summary>
        /// Defines the <see cref="MaxHeight"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> MaxHeightProperty =
            PerspexProperty.Register<RowDefinition, double>("MaxHeight", double.PositiveInfinity);

        /// <summary>
        /// Defines the <see cref="MinHeight"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> MinHeightProperty =
            PerspexProperty.Register<RowDefinition, double>("MinHeight");

        /// <summary>
        /// Defines the <see cref="Height"/> property.
        /// </summary>
        public static readonly PerspexProperty<GridLength> HeightProperty =
            PerspexProperty.Register<RowDefinition, GridLength>("Height", new GridLength(1, GridUnitType.Star));

        /// <summary>
        /// Initializes a new instance of the <see cref="RowDefinition"/> class.
        /// </summary>
        public RowDefinition()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RowDefinition"/> class.
        /// </summary>
        /// <param name="value">The height of the row.</param>
        /// <param name="type">The height unit of the column.</param>
        public RowDefinition(double value, GridUnitType type)
        {
            this.Height = new GridLength(value, type);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RowDefinition"/> class.
        /// </summary>
        /// <param name="height">The height of the column.</param>
        public RowDefinition(GridLength height)
        {
            this.Height = height;
        }

        /// <summary>
        /// Gets the actual calculated height of the row.
        /// </summary>
        public double ActualHeight
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets or sets the maximum height of the row in DIPs.
        /// </summary>
        public double MaxHeight
        {
            get { return this.GetValue(MaxHeightProperty); }
            set { this.SetValue(MaxHeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets the minimum height of the row in DIPs.
        /// </summary>
        public double MinHeight
        {
            get { return this.GetValue(MinHeightProperty); }
            set { this.SetValue(MinHeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets the height of the row.
        /// </summary>
        public GridLength Height
        {
            get { return this.GetValue(HeightProperty); }
            set { this.SetValue(HeightProperty, value); }
        }
    }
}