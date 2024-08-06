using System;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// A <see cref="Panel"/> with uniform column and row sizes.
    /// </summary>
    public class UniformGrid : Panel
    {
        /// <summary>
        /// Defines the <see cref="Rows"/> property.
        /// </summary>
        public static readonly StyledProperty<int> RowsProperty =
            AvaloniaProperty.Register<UniformGrid, int>(nameof(Rows));

        /// <summary>
        /// Defines the <see cref="Columns"/> property.
        /// </summary>
        public static readonly StyledProperty<int> ColumnsProperty =
            AvaloniaProperty.Register<UniformGrid, int>(nameof(Columns));

        /// <summary>
        /// Defines the <see cref="FirstColumn"/> property.
        /// </summary>
        public static readonly StyledProperty<int> FirstColumnProperty =
            AvaloniaProperty.Register<UniformGrid, int>(nameof(FirstColumn));

        private int _rows;
        private int _columns;

        static UniformGrid()
        {
            AffectsMeasure<UniformGrid>(RowsProperty, ColumnsProperty, FirstColumnProperty);
        }

        /// <summary>
        /// Specifies the row count. If set to 0, row count will be calculated automatically.
        /// </summary>
        public int Rows
        {
            get => GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        /// <summary>
        /// Specifies the column count. If set to 0, column count will be calculated automatically.
        /// </summary>
        public int Columns
        {
            get => GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        /// <summary>
        /// Specifies, for the first row, the column where the items should start.
        /// </summary>
        public int FirstColumn
        {
            get => GetValue(FirstColumnProperty);
            set => SetValue(FirstColumnProperty, value);
        }

        /// <summary>
        /// Compute the desired size of this UniformGrid by measuring all of the
        /// children with a constraint equal to a cell's portion of the given
        /// constraint (e.g. for a 2 x 4 grid, the child constraint would be
        /// constraint.Width*0.5 x constraint.Height*0.25).  The maximum child
        /// width and maximum child height are tracked, and then the desired size
        /// is computed by multiplying these maximums by the row and column count
        /// (e.g. for a 2 x 4 grid, the desired size for the UniformGrid would be
        /// maxChildDesiredWidth*2 x maxChildDesiredHeight*4).
        /// </summary>
        /// <param name="constraint">Constraint</param>
        /// <returns>Desired size</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            UpdateComputedValues();

            Size childConstraint = new Size(constraint.Width / _columns, constraint.Height / _rows);
            double maxChildDesiredWidth = 0.0;
            double maxChildDesiredHeight = 0.0;

            //  Measure each child, keeping track of maximum desired width and height.
            for (int i = 0, count = Children.Count; i < count; ++i)
            {
                var child = Children[i];

                // Measure the child.
                child.Measure(childConstraint);
                Size childDesiredSize = child.DesiredSize;

                if (maxChildDesiredWidth < childDesiredSize.Width)
                {
                    maxChildDesiredWidth = childDesiredSize.Width;
                }

                if (maxChildDesiredHeight < childDesiredSize.Height)
                {
                    maxChildDesiredHeight = childDesiredSize.Height;
                }
            }

            return new Size((maxChildDesiredWidth * _columns),(maxChildDesiredHeight * _rows));
        }

        /// <summary>
        /// Arrange the children of this UniformGrid by distributing space evenly 
        /// among all the children, making each child the size equal to a cell's
        /// portion of the given arrangeSize (e.g. for a 2 x 4 grid, the child size
        /// would be arrangeSize*0.5 x arrangeSize*0.25)
        /// </summary>
        /// <param name="arrangeSize">Arrange size</param>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            double childBoundsX = 0; 
            double childBoundsY = 0;
            double childBoundsWidth = arrangeSize.Width / _columns;
            double childBoundsHeight = arrangeSize.Height / _rows;
            
            double xStep = childBoundsWidth;
            double xBound = arrangeSize.Width - 1.0;
            
            childBoundsX += childBoundsWidth * FirstColumn;

            // Arrange and Position each child to the same cell size
            for (int i = 0, count = Children.Count; i < count; ++i)
            {
                var child = Children[i];

                child.Arrange(new Rect(childBoundsX, childBoundsY, childBoundsWidth, childBoundsHeight));

                // only advance to the next grid cell if the child was not collapsed
                if (child.IsEffectivelyVisible)
                {
                    childBoundsX += xStep;
                    if (childBoundsX >= xBound)
                    {
                        childBoundsY += childBoundsHeight;
                        childBoundsX = 0;
                    }
                }
            }

            return arrangeSize;
        } 

        /// <summary>
        /// If either Rows or Columns are set to 0, then dynamically compute these
        /// values based on the actual number of non-collapsed children.
        ///
        /// In the case when both Rows and Columns are set to 0, then make Rows 
        /// and Columns be equal, thus laying out in a square grid.
        /// </summary>
        private void UpdateComputedValues()
        {
            _columns = Columns;
            _rows = Rows;

            //parameter checking. 
            if (FirstColumn >= _columns)
            {
                SetCurrentValue(FirstColumnProperty, 0);
            }

            if ((_rows == 0) || (_columns == 0))
            {
                int nonCollapsedCount = 0;

                // First compute the actual # of non-collapsed children to be laid out
                for (int i = 0, count = Children.Count; i < count; ++i)
                {
                    var child = Children[i];
                    if (child.IsEffectivelyVisible)
                    {
                        nonCollapsedCount++;
                    }
                }

                // to ensure that we have at leat one row & column, make sure
                // that nonCollapsedCount is at least 1
                if (nonCollapsedCount == 0)
                {
                    nonCollapsedCount = 1;
                }

                if (_rows == 0)
                {
                    if (_columns > 0)
                    {
                        // take FirstColumn into account, because it should really affect the result
                        _rows = (nonCollapsedCount + FirstColumn + (_columns - 1)) / _columns;
                    }
                    else
                    {
                        // both rows and columns are unset -- lay out in a square
                        _rows = (int)Math.Sqrt(nonCollapsedCount);
                        if ((_rows * _rows) < nonCollapsedCount)
                        {
                            _rows++;
                        }
                        _columns = _rows;
                    }
                }
                else if (_columns == 0)
                {
                    // guaranteed that _rows is not 0, because we're in the else clause of the check for _rows == 0
                    _columns = (nonCollapsedCount + (_rows - 1)) / _rows;
                }
            }
        }
    }
}
