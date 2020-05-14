// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Data;

namespace Avalonia.Layout
{
    /// <summary>
    /// Arranges child elements into a staggered grid pattern where items are added to the column that has used least amount of space.
    /// </summary>
    public class StaggeredLayout : VirtualizingLayout
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StaggeredLayout"/> class.
        /// </summary>
        public StaggeredLayout()
        {
        }

        /// <summary>
        /// Gets or sets the desired width for each column.
        /// </summary>
        /// <remarks>
        /// The width of columns can exceed the DesiredColumnWidth if the HorizontalAlignment is set to Stretch.
        /// </remarks>
        public double DesiredColumnWidth
        {
            get { return (double)GetValue(DesiredColumnWidthProperty); }
            set { SetValue(DesiredColumnWidthProperty, value); }
        }                   

        /// <summary>
        /// Identifies the <see cref="DesiredColumnWidth"/> dependency property.
        /// </summary>
        /// <returns>The identifier for the <see cref="DesiredColumnWidth"/> dependency property.</returns>
        public static readonly StyledProperty<double> DesiredColumnWidthProperty = AvaloniaProperty.Register<StaggeredLayout, double>(
            nameof(DesiredColumnWidth), 250d);

        /// <summary>
        /// Gets or sets the spacing between columns of items.
        /// </summary>
        public double ColumnSpacing
        {
            get { return (double)GetValue(ColumnSpacingProperty); }
            set { SetValue(ColumnSpacingProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ColumnSpacing"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> ColumnSpacingProperty = AvaloniaProperty.Register<StaggeredLayout, double>(
            nameof(ColumnSpacing),
            0d);            

        /// <summary>
        /// Gets or sets the spacing between rows of items.
        /// </summary>
        public double RowSpacing
        {
            get { return (double)GetValue(RowSpacingProperty); }
            set { SetValue(RowSpacingProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="RowSpacing"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> RowSpacingProperty = AvaloniaProperty.Register<StaggeredLayout, double>(
            nameof(RowSpacing),
            0d);            

        /// <inheritdoc/>
        protected internal override void InitializeForContextCore(VirtualizingLayoutContext context)
        {
            context.LayoutState = new StaggeredLayoutState(context);
            base.InitializeForContextCore(context);
        }

        /// <inheritdoc/>
        protected internal override void UninitializeForContextCore(VirtualizingLayoutContext context)
        {
            context.LayoutState = null;
            base.UninitializeForContextCore(context);
        }

        /// <inheritdoc/>
        protected internal override void OnItemsChangedCore(VirtualizingLayoutContext context, object source, NotifyCollectionChangedEventArgs args)
        {
            var state = (StaggeredLayoutState)context.LayoutState;

            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    state.RemoveFromIndex(args.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    state.RemoveFromIndex(args.NewStartingIndex);

                    // We must recycle the element to ensure that it gets the correct context
                    state.RecycleElementAt(args.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Move:
                    int minIndex = Math.Min(args.NewStartingIndex, args.OldStartingIndex);
                    int maxIndex = Math.Max(args.NewStartingIndex, args.OldStartingIndex);
                    state.RemoveRange(minIndex, maxIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    state.RemoveFromIndex(args.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    state.Clear();
                    break;
            }

            base.OnItemsChangedCore(context, source, args);
        }

        /// <inheritdoc/>
        protected internal override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
        {
            if (context.ItemCount == 0)
            {
                return new Size(availableSize.Width, 0);
            }

            if ((context.RealizationRect.Width == 0) && (context.RealizationRect.Height == 0))
            {
                return new Size(availableSize.Width, 0.0);
            }

            var state = (StaggeredLayoutState)context.LayoutState;

            double availableWidth = availableSize.Width;
            double availableHeight = availableSize.Height;

            double columnWidth = Math.Min(DesiredColumnWidth, availableWidth);
            if (columnWidth != state.ColumnWidth)
            {
                // The items will need to be remeasured
                state.Clear();
            }

            state.ColumnWidth = Math.Min(DesiredColumnWidth, availableWidth);
            int numColumns = Math.Max(1, (int)Math.Floor(availableWidth / state.ColumnWidth));

            // adjust for column spacing on all columns expect the first
            double totalWidth = state.ColumnWidth + ((numColumns - 1) * (state.ColumnWidth + ColumnSpacing));
            if (totalWidth > availableWidth)
            {
                numColumns--;
            }
            else if (double.IsInfinity(availableWidth))
            {
                availableWidth = totalWidth;
            }

            if (numColumns != state.NumberOfColumns)
            {
                // The items will not need to be remeasured, but they will need to go into new columns
                state.ClearColumns();
            }

            if (RowSpacing != state.RowSpacing)
            {
                // If the RowSpacing changes the height of the rows will be different.
                // The columns stores the height so we'll want to clear them out to
                // get the proper height
                state.ClearColumns();
                state.RowSpacing = RowSpacing;
            }

            var columnHeights = new double[numColumns];
            var itemsPerColumn = new int[numColumns];
            var deadColumns = new HashSet<int>();

            for (int i = 0; i < context.ItemCount; i++)
            {
                var columnIndex = GetColumnIndex(columnHeights);

                bool measured = false;
                StaggeredItem item = state.GetItemAt(i);
                if (item.Height == 0)
                {
                    // Item has not been measured yet. Get the element and store the values
                    item.Element = context.GetOrCreateElementAt(i);
                    item.Element.Measure(new Size(state.ColumnWidth, availableHeight));
                    item.Height = item.Element.DesiredSize.Height;
                    measured = true;
                }

                double spacing = itemsPerColumn[columnIndex] > 0 ? RowSpacing : 0;
                item.Top = columnHeights[columnIndex] + spacing;
                double bottom = item.Top + item.Height;
                columnHeights[columnIndex] = bottom;
                itemsPerColumn[columnIndex]++;
                state.AddItemToColumn(item, columnIndex);

                if (bottom < context.RealizationRect.Top)
                {
                    // The bottom of the element is above the realization area
                    if (item.Element != null)
                    {
                        context.RecycleElement(item.Element);
                        item.Element = null;
                    }
                }
                else if (item.Top > context.RealizationRect.Bottom)
                {
                    // The top of the element is below the realization area
                    if (item.Element != null)
                    {
                        context.RecycleElement(item.Element);
                        item.Element = null;
                    }

                    deadColumns.Add(columnIndex);
                }
                else if (measured == false)
                {
                    // We ALWAYS want to measure an item that will be in the bounds
                    item.Element = context.GetOrCreateElementAt(i);
                    item.Element.Measure(new Size(state.ColumnWidth, availableHeight));
                    if (item.Height != item.Element.DesiredSize.Height)
                    {
                        // this item changed size; we need to recalculate layout for everything after this
                        state.RemoveFromIndex(i + 1);
                        item.Height = item.Element.DesiredSize.Height;
                        columnHeights[columnIndex] = item.Top + item.Height;
                    }
                }

                if (deadColumns.Count == numColumns)
                {
                    break;
                }
            }

            double desiredHeight = state.GetHeight();

            return new Size(availableWidth, desiredHeight);
        }

        /// <inheritdoc/>
        protected internal override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
        {
            if ((context.RealizationRect.Width == 0) && (context.RealizationRect.Height == 0))
            {
                return finalSize;
            }

            var state = (StaggeredLayoutState)context.LayoutState;

            // Cycle through each column and arrange the items that are within the realization bounds
            for (int columnIndex = 0; columnIndex < state.NumberOfColumns; columnIndex++)
            {
                StaggeredColumnLayout layout = state.GetColumnLayout(columnIndex);
                for (int i = 0; i < layout.Count; i++)
                {
                    StaggeredItem item = layout[i];

                    double bottom = item.Top + item.Height;
                    if (bottom < context.RealizationRect.Top)
                    {
                        // element is above the realization bounds
                        continue;
                    }

                    if (item.Top <= context.RealizationRect.Bottom)
                    {
                        double itemHorizontalOffset = (state.ColumnWidth * columnIndex) + (ColumnSpacing * columnIndex);

                        Rect bounds = new Rect(itemHorizontalOffset, item.Top, state.ColumnWidth, item.Height);
                        var element = context.GetOrCreateElementAt(item.Index);
                        element.Arrange(bounds);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return finalSize;
        }

        protected override void OnPropertyChanged<T>(AvaloniaProperty<T> property, Optional<T> oldValue, BindingValue<T> newValue, BindingPriority priority)
        {
            base.OnPropertyChanged(property, oldValue, newValue, priority);

            if(property == DesiredColumnWidthProperty || property == ColumnSpacingProperty || property == RowSpacingProperty)
            {
                InvalidateMeasure();
            }
        }

        private static int GetColumnIndex(double[] columnHeights)
        {
            int columnIndex = 0;
            double height = columnHeights[0];
            for (int j = 1; j < columnHeights.Length; j++)
            {
                if (columnHeights[j] < height)
                {
                    columnIndex = j;
                    height = columnHeights[j];
                }
            }

            return columnIndex;
        }
    }
}
