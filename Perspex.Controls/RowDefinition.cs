// -----------------------------------------------------------------------
// <copyright file="RowDefinition.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    public class RowDefinition : DefinitionBase
    {
        public static readonly PerspexProperty<double> MaxHeightProperty =
            PerspexProperty.Register<RowDefinition, double>("MaxHeight", double.PositiveInfinity);

        public static readonly PerspexProperty<double> MinHeightProperty =
            PerspexProperty.Register<RowDefinition, double>("MinHeight");

        public static readonly PerspexProperty<GridLength> HeightProperty =
            PerspexProperty.Register<RowDefinition, GridLength>("Height", new GridLength(1, GridUnitType.Star));

        public RowDefinition()
        {
        }

        public RowDefinition(GridLength height)
        {
            this.Height = height;
        }

        public double ActualHeight
        {
            get;
            internal set;
        }

        public double MaxHeight
        {
            get { return this.GetValue(MaxHeightProperty); }
            set { this.SetValue(MaxHeightProperty, value); }
        }

        public double MinHeight
        {
            get { return this.GetValue(MinHeightProperty); }
            set { this.SetValue(MinHeightProperty, value); }
        }

        public GridLength Height
        {
            get { return this.GetValue(HeightProperty); }
            set { this.SetValue(HeightProperty, value); }
        }
    }
}
