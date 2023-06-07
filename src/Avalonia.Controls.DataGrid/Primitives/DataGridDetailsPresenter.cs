// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Media;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Used within the template of a <see cref="T:Avalonia.Controls.DataGrid" /> to specify the location in the control's visual tree 
    /// where the row details are to be added.
    /// </summary>
    public sealed class DataGridDetailsPresenter : Panel
    {
        public static readonly StyledProperty<double> ContentHeightProperty =
            AvaloniaProperty.Register<DataGridDetailsPresenter, double>(nameof(ContentHeight));

        /// <summary>
        /// Gets or sets the height of the content.
        /// </summary>
        /// <returns>
        /// The height of the content.
        /// </returns>
        public double ContentHeight
        {
            get { return GetValue(ContentHeightProperty); }
            set { SetValue(ContentHeightProperty, value); }
        }

        internal DataGridRow OwningRow
        {
            get;
            set;
        }
        private DataGrid OwningGrid => OwningRow?.OwningGrid;

        public DataGridDetailsPresenter()
        {
            AffectsMeasure<DataGridDetailsPresenter>(ContentHeightProperty);
        }

        /// <summary>
        /// Arranges the content of the <see cref="T:Avalonia.Controls.Primitives.DataGridDetailsPresenter" />.
        /// </summary>
        /// <returns>
        /// The actual size used by the <see cref="T:Avalonia.Controls.Primitives.DataGridDetailsPresenter" />.
        /// </returns>
        /// <param name="finalSize">
        /// The final area within the parent that this element should use to arrange itself and its children.
        /// </param>
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (OwningGrid == null)
            {
                return base.ArrangeOverride(finalSize);
            }
            double rowGroupSpacerWidth = OwningGrid.ColumnsInternal.RowGroupSpacerColumn.Width.Value;
            double leftEdge = rowGroupSpacerWidth;
            double xClip = OwningGrid.AreRowGroupHeadersFrozen ? rowGroupSpacerWidth : 0;
            double width;
            if (OwningGrid.AreRowDetailsFrozen)
            {
                leftEdge += OwningGrid.HorizontalOffset;
                width = OwningGrid.CellsWidth;
            }
            else
            {
                xClip += OwningGrid.HorizontalOffset;
                width = Math.Max(OwningGrid.CellsWidth, OwningGrid.ColumnsInternal.VisibleEdgedColumnsWidth);
            }
            // Details should not extend through the indented area
            width -= rowGroupSpacerWidth;
            double height = Math.Max(0, double.IsNaN(ContentHeight) ? 0 : ContentHeight);

            foreach (Control child in Children)
            {
                child.Arrange(new Rect(leftEdge, 0, width, height));
            }

            if (OwningGrid.AreRowDetailsFrozen)
            {
                // Frozen Details should not be clipped, similar to frozen cells
                Clip = null;
            }
            else
            {
                // Clip so Details doesn't obstruct elements to the left (the RowHeader by default) as we scroll to the right
                Clip = new RectangleGeometry
                {
                    Rect = new Rect(xClip, 0, Math.Max(0, width - xClip + rowGroupSpacerWidth), height)
                };
            }

            return finalSize;
        }

        /// <summary>
        /// Measures the children of a <see cref="T:Avalonia.Controls.Primitives.DataGridDetailsPresenter" /> to 
        /// prepare for arranging them during the <see cref="M:System.Windows.FrameworkElement.ArrangeOverride(System.Windows.Size)" /> pass.
        /// </summary>
        /// <param name="availableSize">
        /// The available size that this element can give to child elements. Indicates an upper limit that child elements should not exceed.
        /// </param>
        /// <returns>
        /// The size that the <see cref="T:Avalonia.Controls.Primitives.DataGridDetailsPresenter" /> determines it needs during layout, based on its calculations of child object allocated sizes.
        /// </returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (OwningGrid == null || Children.Count == 0)
            {
                return default;
            }

            double desiredWidth = OwningGrid.AreRowDetailsFrozen ?
                OwningGrid.CellsWidth :
                Math.Max(OwningGrid.CellsWidth, OwningGrid.ColumnsInternal.VisibleEdgedColumnsWidth);

            desiredWidth -= OwningGrid.ColumnsInternal.RowGroupSpacerColumn.Width.Value;

            foreach (Control child in Children)
            {
                child.Measure(new Size(desiredWidth, double.PositiveInfinity));
            }

            double desiredHeight = Math.Max(0, double.IsNaN(ContentHeight) ? 0 : ContentHeight);

            return new Size(desiredWidth, desiredHeight);
        }
    }
}
