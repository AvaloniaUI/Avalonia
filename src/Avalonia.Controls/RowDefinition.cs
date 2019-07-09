// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Controls
{
    /// <summary>
    /// Holds a row definitions for a <see cref="Grid"/>.
    /// </summary>
    public class RowDefinition : DefinitionBase
    {
        /// <summary>
        /// Defines the <see cref="MaxHeight"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MaxHeightProperty =
            AvaloniaProperty.Register<RowDefinition, double>(nameof(MaxHeight), double.PositiveInfinity);

        /// <summary>
        /// Defines the <see cref="MinHeight"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MinHeightProperty =
            AvaloniaProperty.Register<RowDefinition, double>(nameof(MinHeight));

        /// <summary>
        /// Defines the <see cref="Height"/> property.
        /// </summary>
        public static readonly StyledProperty<GridLength> HeightProperty =
            AvaloniaProperty.Register<RowDefinition, GridLength>(nameof(Height), new GridLength(1, GridUnitType.Star));

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
            Height = new GridLength(value, type);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RowDefinition"/> class.
        /// </summary>
        /// <param name="height">The height of the column.</param>
        public RowDefinition(GridLength height)
        {
            Height = height;
        }

        /// <summary>
        /// Gets the actual calculated height of the row.
        /// </summary>
        public double ActualHeight => Parent?.GetFinalRowDefinitionHeight(Index) ?? 0d;

        /// <summary>
        /// Gets or sets the maximum height of the row in DIPs.
        /// </summary>
        public double MaxHeight
        {
            get
            {
                return GetValue(MaxHeightProperty);
            }
            set
            {
                Parent?.InvalidateMeasure();
                SetValue(MaxHeightProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the minimum height of the row in DIPs.
        /// </summary>
        public double MinHeight
        {
            get
            {
                return GetValue(MinHeightProperty);
            }
            set
            {
                Parent?.InvalidateMeasure();
                SetValue(MinHeightProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the height of the row.
        /// </summary>
        public GridLength Height
        {
            get
            {
                return GetValue(HeightProperty);
            }
            set
            {
                Parent?.InvalidateMeasure();
                SetValue(HeightProperty, value);
            }
        }

        internal override GridLength UserSizeValueCache => this.Height;
        internal override double UserMinSizeValueCache => this.MinHeight;
        internal override double UserMaxSizeValueCache => this.MaxHeight;
    }
}
