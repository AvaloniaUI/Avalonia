// -----------------------------------------------------------------------
// <copyright file="ColumnDefinition.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    public class ColumnDefinition : DefinitionBase
    {
        public static readonly PerspexProperty<double> MaxWidthProperty =
            PerspexProperty.Register<ColumnDefinition, double>("MaxWidth", double.PositiveInfinity);

        public static readonly PerspexProperty<double> MinWidthProperty =
            PerspexProperty.Register<ColumnDefinition, double>("MinWidth");

        public static readonly PerspexProperty<GridLength> WidthProperty =
            PerspexProperty.Register<ColumnDefinition, GridLength>("Width", new GridLength(1, GridUnitType.Star));

        public ColumnDefinition()
        {
        }

        public ColumnDefinition(double value, GridUnitType type)
        {
            this.Width = new GridLength(value, type);
        }

        public ColumnDefinition(GridLength width)
        {
            this.Width = width;
        }

        public double ActualWidth
        {
            get;
            internal set;
        }

        public double MaxWidth
        {
            get { return this.GetValue(MaxWidthProperty); }
            set { this.SetValue(MaxWidthProperty, value); }
        }

        public double MinWidth
        {
            get { return this.GetValue(MinWidthProperty); }
            set { this.SetValue(MinWidthProperty, value); }
        }

        public GridLength Width
        {
            get { return this.GetValue(WidthProperty); }
            set { this.SetValue(WidthProperty, value); }
        }
    }
}
