// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Collections.Generic;       // List<T>
using System.Collections.Specialized;   // NotifyCollectionChangedAction
using System.ComponentModel;            // PropertyChangedEventArgs
using System.Diagnostics;
using Avalonia.Controls.Primitives;   // GridViewRowPresenterBase
using System;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    /// <summary>
    ///     An GridViewRowPresenter marks the site (in a style) of the panel that controls
    ///     layout of groups or items.
    /// </summary>
    public class GridViewRowPresenter : GridViewRowPresenterBase
    {
        public GridViewRowPresenter()
            {
            ContentProperty.Changed.AddClassHandler<GridViewRowPresenter>(OnContentChanged);
            AffectsMeasure<GridViewRowPresenter>(ContentProperty);
            }

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        ///     The DependencyProperty for the Content property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        // Any change in Content properties affectes layout measurement since
        // a new template may be used. On measurement,
        // ApplyTemplate will be invoked leading to possible application
        // of a new template.
        public static readonly StyledProperty<object> ContentProperty =
                ContentControl.ContentProperty.AddOwner<GridViewRowPresenter>();

        /// <summary>
        ///     Content is the data used to generate the child elements of this control.
        /// </summary>
        public object Content
        {
            get { return GetValue(GridViewRowPresenter.ContentProperty); }
            set { SetValue(GridViewRowPresenter.ContentProperty, value); }
        }

        /// <summary>
        ///     Called when ContentProperty is invalidated on "d."
        /// </summary>
        private static void OnContentChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            GridViewRowPresenter gvrp = (GridViewRowPresenter)d;

            //
            // If the old and new value have the same type then we can save a lot of perf by
            // keeping the existing ContentPresenters
            //

            Type oldType = (e.OldValue != null) ? e.OldValue.GetType() : null;
            Type newType = (e.NewValue != null) ? e.NewValue.GetType() : null;

            // DisconnectedItem doesn't count as a real type change
            //TODO
            /*if (e.NewValue == BindingExpressionBase.DisconnectedItem)
            {
                gvrp._oldContentType = oldType;
                newType = oldType;
            }
            else if (e.OldValue == BindingExpressionBase.DisconnectedItem)
            {
                oldType = gvrp._oldContentType;
            }*/

            if (oldType != newType)
            {
                gvrp.NeedUpdateVisualTree = true;
            }
            else
            {
                gvrp.UpdateCells();
            }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        // Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Override of <seealso cref="FrameworkElement.MeasureOverride" />.
        /// </summary>
        /// <param name="constraint">Constraint size is an "upper limit" that the return value should not exceed.</param>
        /// <returns>The GridViewRowPresenter's desired size.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            GridViewColumnCollection columns = Columns;
            if (columns == null)
            { return new Size(); }

            Controls children = InternalChildren;
            double maxHeight = 0.0;           // Max height of children.
            double accumulatedWidth = 0.0;    // Total width consumed by children.
            double constraintHeight = constraint.Height;
            bool desiredWidthListEnsured = false;

            foreach (GridViewColumn column in columns)
            {
                IControl child = children[column.ActualIndex];
                if (child == null)
                { continue; }

                double childConstraintWidth = Math.Max(0.0, constraint.Width - accumulatedWidth);

                if (column.State == ColumnMeasureState.Init
                    || column.State == ColumnMeasureState.Headered)
                {
                    if (!desiredWidthListEnsured)
                    {
                        EnsureDesiredWidthList();
                        LayoutUpdated += new EventHandler(OnLayoutUpdated);
                        desiredWidthListEnsured = true;
                    }

                    // Measure child.
                    child.Measure(new Size(childConstraintWidth, constraintHeight));

                    // As long as this is the first round of measure that has data participate
                    // the width should be ensured
                    // only element on current page paticipates in calculating the shared width
                    if (IsOnCurrentPage)
                    {
                        column.EnsureWidth(child.DesiredSize.Width);
                    }

                    DesiredWidthList[column.ActualIndex] = column.DesiredWidth;

                    accumulatedWidth += column.DesiredWidth;
                }
                else if (column.State == ColumnMeasureState.Data)
                {
                    childConstraintWidth = Math.Min(childConstraintWidth, column.DesiredWidth);

                    child.Measure(new Size(childConstraintWidth, constraintHeight));

                    accumulatedWidth += column.DesiredWidth;
                }
                else // ColumnMeasureState.SpecificWidth
                {
                    childConstraintWidth = Math.Min(childConstraintWidth, column.Width);

                    child.Measure(new Size(childConstraintWidth, constraintHeight));

                    accumulatedWidth += column.Width;
                }

                maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
            }

            // Reset this flag so that we will re-caculate it on every measure.
            _isOnCurrentPageValid = false;

            // reserve space for dummy header next to the last column
            accumulatedWidth += c_PaddingHeaderMinWidth;

            return (new Size(accumulatedWidth, maxHeight));
        }

        /// <summary>
        /// GridViewRowPresenter computes the position of its children inside each child's Margin and calls Arrange
        /// on each child.
        /// </summary>
        /// <param name="arrangeSize">Size the GridViewRowPresenter will assume.</param>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            GridViewColumnCollection columns = Columns;
            if (columns == null)
            { return arrangeSize; }

            Controls children = InternalChildren;

            double accumulatedWidth = 0.0;
            double remainingWidth = arrangeSize.Width;

            foreach (GridViewColumn column in columns)
            {
                IControl child = children[column.ActualIndex];
                if (child == null)
                { continue; }

                // has a given value or 'auto'
                double childArrangeWidth = Math.Min(remainingWidth, ((column.State == ColumnMeasureState.SpecificWidth) ? column.Width : column.DesiredWidth));

                child.Arrange(new Rect(accumulatedWidth, 0, childArrangeWidth, arrangeSize.Height));

                remainingWidth -= childArrangeWidth;
                accumulatedWidth += childArrangeWidth;
            }

            return arrangeSize;
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------
        //
        // Internal Methods / Properties
        //
        //-------------------------------------------------------------------

        #region Internal Methods / Properties

        /// <summary>
        /// Called when the Template's tree has been generated
        /// </summary>
        /*internal override void OnApplyTemplate()
        {
            // +-- GridViewRowPresenter ------------------------------------+
            // |                                                            |
            // |  +- CtPstr1 ---+   +- CtPstr2 ---+   +- CtPstr3 ---+       |
            // |  |             |   |             |   |             |  ...  |
            // |  +-------------+   +-------------+   +-------------+       |
            // +------------------------------------------------------------+

            base.OnPreApplyTemplate();

            if (NeedUpdateVisualTree)
            {
                InternalChildren.Clear();

                // build the whole collection from draft.
                GridViewColumnCollection columns = Columns;
                if (columns != null)
                {
                    foreach (GridViewColumn column in columns.ColumnCollection)
                    {
                        InternalChildren.AddInternal(CreateCell(column));
                    }
                }

                NeedUpdateVisualTree = false;
            }

            // invalidate viewPort cache
            _viewPortValid = false;
        }*/

        /// <summary>
        /// Handler of column's PropertyChanged event. Update correspondent property
        /// if change is of Width / CellTemplate / CellTemplateSelector.
        /// </summary>
        internal override void OnColumnPropertyChanged(GridViewColumn column, string propertyName)
        {
            Debug.Assert(column != null);
            int index;

            // ActualWidth change is a noise to RowPresenter, so filter it out.
            // Note-on-perf: ActualWidth property change of will fire N x M times
            // on every start up. (N: number of column with Width set to 'auto',
            // M: number of visible items)
            if (GridViewColumn.c_ActualWidthName.Equals(propertyName))
            {
                return;
            }

            // Width is the #1 property that will be changed frequently. The others
            // (DisplayMemberBinding/CellTemplate/Selector) are not.

            if (((index = column.ActualIndex) >= 0) && (index < InternalChildren.Count))
            {
                if (GridViewColumn.WidthProperty.Name.Equals(propertyName))
                {
                    InvalidateMeasure();
                }

                // Priority: DisplayMemberBinding > CellTemplate > CellTemplateSelector
                /*else if (GridViewColumn.c_DisplayMemberBindingName.Equals(propertyName))
                {
                    FrameworkElement cell = InternalChildren[index] as FrameworkElement;
                    if (cell != null)
                    {
                        BindingBase binding = column.DisplayMemberBinding;
                        if (binding != null && cell is TextBlock)
                        {
                            cell.SetBinding(TextBlock.TextProperty, binding);
                        }
                        else
                        {
                            RenewCell(index, column);
                        }
                    }
                }*/
                else
                {
                    ContentPresenter cp = InternalChildren[index] as ContentPresenter;
                    if (cp != null)
                    {
                        if (GridViewColumn.CellTemplateProperty.Name.Equals(propertyName))
                        {
                            IDataTemplate dt;
                            if ((dt = column.CellTemplate) == null)
                            {
                                cp.ClearValue(ContentControl.ContentTemplateProperty);
                            }
                            else
                            {
                                cp.ContentTemplate = dt;
                            }
                        }
                        /*
                        else if (GridViewColumn.CellTemplateSelectorProperty.Name.Equals(propertyName))
                        {
                            DataTemplateSelector dts;
                            if ((dts = column.CellTemplateSelector) == null)
                            {
                                cp.ClearValue(ContentControl.ContentTemplateSelectorProperty);
                            }
                            else
                            {
                                cp.ContentTemplateSelector = dts;
                            }
                        }*/
                    }
                }
            }
        }

        /// <summary>
        /// process GridViewColumnCollection.CollectionChanged event.
        /// </summary>
        internal override void OnColumnCollectionChanged(GridViewColumnCollectionChangedEventArgs e)
        {
            base.OnColumnCollectionChanged(e);

            if (e.Action == NotifyCollectionChangedAction.Move)
            {
                InvalidateArrange();
            }
            else
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        // New child will always be appended to the very last, no matter it
                        // is actually add via 'Insert' or just 'Add'.
                        InternalChildren.Add(CreateCell((GridViewColumn)(e.NewItems[0])));
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        InternalChildren.RemoveAt(e.ActualIndex);
                        break;

                    case NotifyCollectionChangedAction.Replace:
                        InternalChildren.RemoveAt(e.ActualIndex);
                        InternalChildren.Add(CreateCell((GridViewColumn)(e.NewItems[0])));
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        InternalChildren.Clear();
                        break;

                    default:
                        break;
                }

                InvalidateMeasure();
            }
        }

        /*// Used in UIAutomation
        // Return the actual cells array (If user reorder column, the cell in InternalChildren isn't in the correct order)
        internal List<Control> ActualCells
        {
            get
            {
                List<UIElement> list = new List<UIElement>();
                GridViewColumnCollection columns = Columns;
                if (columns != null)
                {
                    UIElementCollection children = InternalChildren;
                    List<int> indexList = columns.IndexList;

                    if (children.Count == columns.Count)
                    {
                        for (int i = 0, count = columns.Count; i < count; ++i)
                        {
                            UIElement cell = children[indexList[i]];
                            if (cell != null)
                            {
                                list.Add(cell);
                            }
                        }
                    }
                }

                return list;
            }
        }*/

        #endregion Internal Methods / Properties

        //-------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /*private void FindViewPort()
        {
            // assume GridViewRowPresenter is in Item's template
            _viewItem = this.TemplatedParent as AvaloniaElement;

            if (_viewItem != null)
            {
                ItemsControl itemsControl = ItemsControl.ItemsControlFromItemContainer(_viewItem) as ItemsControl;

                if (itemsControl != null)
                {
                    ScrollViewer scrollViewer = itemsControl.ScrollHost as ScrollViewer;
                    if (scrollViewer != null)
                    {
                        // check if Virtualizing Panel do works
                        if (itemsControl.ItemsHost is VirtualizingPanel &&
                            scrollViewer.CanContentScroll)
                        {
                            // find the 'PART_ScrollContentPresenter' in GridViewScrollViewer
                            _viewPort = scrollViewer.GetTemplateChild(ScrollViewer.ScrollContentPresenterTemplateName) as FrameworkElement;

                            // in case GridViewScrollViewer is re-styled, say, cannot find PART_ScrollContentPresenter
                            if (_viewPort == null)
                            {
                                _viewPort = scrollViewer;
                            }
                        }
                    }
                }
            }
        }*/

        private bool CheckVisibleOnCurrentPage()
        {
            //TODO
            /*
            if (!_viewPortValid)
            {
                FindViewPort();
            }*/

            bool result = true;

            
            /*if (_viewItem != null && _viewPort != null)
            {
                Rect viewPortBounds = new Rect(new Point(), _viewPort.RenderSize);
                Rect itemBounds = new Rect(new Point(), _viewItem.RenderSize);
                itemBounds = _viewItem.TransformToAncestor(_viewPort).TransformBounds(itemBounds);

                // check if item bounds falls in view port bounds (in height)
                result = CheckContains(viewPortBounds, itemBounds);
            }*/

            return result;
        }

        private bool CheckContains(Rect container, Rect element)
        {
            // Check if ANY part of the element reside in container
            // return true if and only if (either case)
            //
            // +-------------------------------------------+
            // +  #================================#       +
            // +--#--------------------------------#-------+
            //    #                                #
            //    #                                #
            // +--#--------------------------------#-------+
            // +  #                                #       +
            // +--#--------------------------------#-------+
            //    #                                #
            //    #                                #
            // +--#--------------------------------#-------+
            // +  #================================#       +
            // +-------------------------------------------+

            // The tolerance here is to make sure at least 2 pixels are inside container
            const double tolerance = 2.0;

            return ((CheckIsPointBetween(container, element.Top) && CheckIsPointBetween(container, element.Bottom)) ||
                    CheckIsPointBetween(element, container.Top + tolerance) ||
                    CheckIsPointBetween(element, container.Bottom - tolerance));
        }

        private bool CheckIsPointBetween(Rect rect, double pointY)
        {
            // return rect.Top <= pointY <= rect.Bottom
            return (MathUtilities.LessThanOrClose(rect.Top, pointY) &&
                    MathUtilities.LessThanOrClose(pointY, rect.Bottom));
        }

        private void OnLayoutUpdated(object sender, EventArgs e)
        {
            bool desiredWidthChanged = false; // whether the shared minimum width has been changed since last layout

            GridViewColumnCollection columns = Columns;
            if (columns != null)
            {
                foreach (GridViewColumn column in columns)
                {
                    if ((column.State != ColumnMeasureState.SpecificWidth))
                    {
                        column.State = ColumnMeasureState.Data;

                        if (DesiredWidthList == null || column.ActualIndex >= DesiredWidthList.Count)
                        {
                            // How can this happen?
                            // Between the last measure was called and this update is called, there can be a
                            // change done to the ColumnCollection and result in DesiredWidthList out of sync
                            // with the columnn collection. What can we do is end this call asap and the next
                            // measure will fix it.
                            desiredWidthChanged = true;
                            break;
                        }

                        if (!MathUtilities.AreClose(column.DesiredWidth, DesiredWidthList[column.ActualIndex]))
                        {
                            // Update the record because collection operation latter on might
                            // need to verified this list again, e.g. insert an 'auto'
                            // column, so that we won't trigger unnecessary update due to
                            // inconsistency of this column.
                            DesiredWidthList[column.ActualIndex] = column.DesiredWidth;

                            desiredWidthChanged = true;
                        }
                    }
                }
            }

            if (desiredWidthChanged)
            {
                InvalidateMeasure();
            }

            LayoutUpdated -= new EventHandler(OnLayoutUpdated);
        }

        private Control CreateCell(GridViewColumn column)
        {
            Debug.Assert(column != null, "column shouldn't be null");

            Control cell;
            //BindingBase binding;

            // Priority: DisplayMemberBinding > CellTemplate > CellTemplateSelector

            /*if ((binding = column.DisplayMemberBinding) != null)
            {
                cell = new TextBlock();

                // Needed this. Otherwise can't size to content at startup time.
                // The reason is cell.Text is empty after the first round of measure.
                cell.DataContext = Content;

                cell.SetBinding(TextBlock.TextProperty, binding);
            }
            else*/
            {
                ContentPresenter cp = new ContentPresenter();
                cp.Content = Content;

                IDataTemplate dt;
                //DataTemplateSelector dts;
                if ((dt = column.CellTemplate) != null)
                {
                    cp.ContentTemplate = dt;
                }
                /*if ((dts = column.CellTemplateSelector) != null)
                {
                    cp.ContentTemplateSelector = dts;
                }*/

                cell = cp;
            }

            // copy alignment properties from ListViewItem
            // for perf reason, not use binding here
            ContentControl parent;
            if ((parent = TemplatedParent as ContentControl) != null)
            {
                cell.VerticalAlignment = parent.VerticalContentAlignment;
                cell.HorizontalAlignment = parent.HorizontalContentAlignment;
            }

            cell.Margin = _defalutCellMargin;

            return cell;
        }

        private void RenewCell(int index, GridViewColumn column)
        {
            InternalChildren.RemoveAt(index);
            InternalChildren.Insert(index, CreateCell(column));
        }


        /// <summary>
        /// Updates all cells to the latest Content.
        /// </summary>
        private void UpdateCells()
        {
            ContentPresenter cellAsCP;
            Control cell;
            Controls children = InternalChildren;
            ContentControl parent = TemplatedParent as ContentControl;

            for (int i = 0; i < children.Count; i++)
            {
                cell = (Control)children[i];

                if ((cellAsCP = cell as ContentPresenter) != null)
                {
                    cellAsCP.Content = Content;
                }
                else
                {
                    Debug.Assert(cell is TextBlock, "cells are either TextBlocks or ContentPresenters");
                    cell.DataContext = Content;
                }

                if (parent != null)
                {
                    cell.VerticalAlignment = parent.VerticalContentAlignment;
                    cell.HorizontalAlignment = parent.HorizontalContentAlignment;
                }
            }
        }


        #endregion

        //-------------------------------------------------------------------
        //
        // Private Properties / Fields
        //
        //-------------------------------------------------------------------

        #region Private Properties / Fields

        // if RowPresenter is not 'real' visible, it should not participating in measuring column width
        // NOTE: IsVisible is force-inheriting parent's value, that's why we pick IsVisible instead of Visibility
        //       e.g. if RowPresenter's parent is hidden/collapsed (e.g. in ListTreeView),
        //            then RowPresenter.Visiblity = Visible, but RowPresenter.IsVisible = false
        private bool IsOnCurrentPage
        {
            get
            {
                if (!_isOnCurrentPageValid)
                {
                    _isOnCurrentPage = IsVisible && CheckVisibleOnCurrentPage();
                    _isOnCurrentPageValid = true;
                }

                return _isOnCurrentPage;
            }
        }

        private Control _viewPort;
        private Control _viewItem;
        private Type _oldContentType;
        private bool _viewPortValid = false;
        private bool _isOnCurrentPage = false;
        private bool _isOnCurrentPageValid = false;

        private static readonly Thickness _defalutCellMargin = new Thickness(6, 0, 6, 0);

        #endregion Private Properties / Fields
    }
}
