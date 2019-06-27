// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Controls
{
    /// <summary>
    /// Holds a column definitions for a <see cref="Grid"/>.
    /// </summary>
    public class ColumnDefinition : DefinitionBase
    {
        /// <summary>
        /// Defines the <see cref="MaxWidth"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MaxWidthProperty =
            AvaloniaProperty.Register<ColumnDefinition, double>(nameof(MaxWidth), double.PositiveInfinity);

        /// <summary>
        /// Defines the <see cref="MinWidth"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MinWidthProperty =
            AvaloniaProperty.Register<ColumnDefinition, double>(nameof(MinWidth));

        /// <summary>
        /// Defines the <see cref="Width"/> property.
        /// </summary>
        public static readonly StyledProperty<GridLength> WidthProperty =
            AvaloniaProperty.Register<ColumnDefinition, GridLength>(nameof(Width), new GridLength(1, GridUnitType.Star));

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnDefinition"/> class.
        /// </summary>
        public ColumnDefinition()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnDefinition"/> class.
        /// </summary>
        /// <param name="value">The width of the column.</param>
        /// <param name="type">The width unit of the column.</param>
        public ColumnDefinition(double value, GridUnitType type)
        {
            Width = new GridLength(value, type);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnDefinition"/> class.
        /// </summary>
        /// <param name="width">The width of the column.</param>
        public ColumnDefinition(GridLength width)
        {
            Width = width;
        }

        /// <summary>
        /// Gets the actual calculated width of the column.
        /// </summary>
        public double ActualWidth => Parent?.GetFinalColumnDefinitionWidth(Index) ?? 0d;

        /// <summary>
        /// Gets or sets the maximum width of the column in DIPs.
        /// </summary>
        public double MaxWidth
        {
            get
            {
                return GetValue(MaxWidthProperty);
            }
            set
            {
                Parent?.InvalidateMeasure();
                SetValue(MaxWidthProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the minimum width of the column in DIPs.
        /// </summary>
        public double MinWidth
        {
            get
            {
                return GetValue(MinWidthProperty);
            }
            set
            {
                Parent?.InvalidateMeasure();
                SetValue(MinWidthProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the width of the column.
        /// </summary>
        public GridLength Width
        {
            get
            {
                return GetValue(WidthProperty);
            }
            set
            {
                Parent?.InvalidateMeasure();
                SetValue(WidthProperty, value);
            }
        }

        internal override GridLength UserSizeValueCache => this.Width;
        internal override double UserMinSizeValueCache => this.MinWidth;
        internal override double UserMaxSizeValueCache => this.MaxWidth;
    }
}
