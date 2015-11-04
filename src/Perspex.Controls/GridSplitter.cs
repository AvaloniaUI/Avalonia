// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Controls.Primitives;
using Perspex.Input;
using Perspex.Rendering;
using Perspex.VisualTree;

namespace Perspex.Controls
{
    public class GridSplitter : Thumb
    {
        private Grid _grid;

        /// <summary>
        /// Initializes a new instance of the <see cref="GridSplitter"/> class.
        /// </summary>
        public GridSplitter()
        {
            Cursor = new Cursor(StandardCursorType.SizeWestEast);
        }

        protected override void OnDragDelta(VectorEventArgs e)
        {
            int col = GetValue(Grid.ColumnProperty);

            if (_grid != null && col > 0)
            {
                var size = _grid.ColumnDefinitions[col - 1].ActualWidth + e.Vector.X;               
                _grid.ColumnDefinitions[col - 1].Width = new GridLength(
                    Math.Max(0, size),
                    GridUnitType.Pixel);
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _grid = this.GetVisualParent<Grid>();
        }
    }
}
