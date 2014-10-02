// -----------------------------------------------------------------------
// <copyright file="GridSplitter.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using Perspex.Controls.Primitives;
    using Perspex.Input;

    public class GridSplitter : Thumb
    {
        private Grid grid;

        protected override void OnDragDelta(VectorEventArgs e)
        {
            int col = this.GetValue(Grid.ColumnProperty);

            if (grid != null && col > 0)
            {
                grid.ColumnDefinitions[col - 1].Width = new GridLength(
                    grid.ColumnDefinitions[col - 1].ActualWidth + e.Vector.X,
                    GridUnitType.Pixel);
            }
        }

        protected override void OnVisualParentChanged(Visual oldParent)
        {
            this.grid = this.GetVisualParent<Grid>();
        }
    }
}
