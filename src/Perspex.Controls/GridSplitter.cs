





namespace Perspex.Controls
{
    using System;
    using Perspex.Controls.Primitives;
    using Perspex.Input;
    using Perspex.Rendering;
    using Perspex.VisualTree;

    public class GridSplitter : Thumb
    {
        private Grid grid;

        /// <summary>
        /// Initializes a new instance of the <see cref="GridSplitter"/> class.
        /// </summary>
        public GridSplitter()
        {
            this.Cursor = new Cursor(StandardCursorType.SizeWestEast);
        }

        protected override void OnDragDelta(VectorEventArgs e)
        {
            int col = this.GetValue(Grid.ColumnProperty);

            if (this.grid != null && col > 0)
            {
                this.grid.ColumnDefinitions[col - 1].Width = new GridLength(
                    this.grid.ColumnDefinitions[col - 1].ActualWidth + e.Vector.X,
                    GridUnitType.Pixel);
            }
        }

        protected override void OnAttachedToVisualTree(IRenderRoot root)
        {
            base.OnAttachedToVisualTree(root);
            this.grid = this.GetVisualParent<Grid>();
        }
    }
}
