// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;           // List<T>
using System.Collections.Specialized;       // NotifyCollectionChangedAction
using System.ComponentModel;                // DesignerSerializationVisibility
using Avalonia.Controls.Primitives;   // GridViewRowPresenterBase
using Avalonia.Data;                  // Binding
using System.Windows.Input;                 // MouseEventArgs
using Avalonia.Media;                 // SolidColorBrush
using System.Diagnostics;

using Avalonia.Styling;
//using Avalonia.Markup.Xaml.Templates;
using Avalonia.Input;
using Avalonia.Utilities;
using Avalonia.Controls.Templates;

namespace Avalonia.Controls
{
    /// <summary>
    /// GridViewHeaderRowPresenter is used within the style to denote the headers place
    /// in GridView's visual tree
    /// </summary>
    public class GridViewHeaderRowPresenter : GridViewRowPresenterBase
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        #region ColumnHeaderTemplate

        /// <summary>
        /// ColumnHeaderTemplate DependencyProperty
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> ColumnHeaderTemplateProperty = GridView.ColumnHeaderTemplateProperty.AddOwner<GridViewHeaderRowPresenter>();
        /// callback property changed
        /// <summary>
        /// column header template
        /// </summary>
        public IDataTemplate ColumnHeaderTemplate
        {
            get { return (IDataTemplate)GetValue(ColumnHeaderTemplateProperty); }
            set { SetValue(ColumnHeaderTemplateProperty, value); }
        }

        #endregion  ColumnHeaderTemplate


        #region ColumnHeaderStringFormat

        /// <summary>
        /// ColumnHeaderStringFormat DependencyProperty
        /// </summary>
        public static readonly StyledProperty<string> ColumnHeaderStringFormatProperty = GridView.ColumnHeaderStringFormatProperty.AddOwner<GridViewHeaderRowPresenter>();
        /// callback property changed

        /// <summary>
        /// header template selector
        /// </summary>
        /// <remarks>
        ///     This property is ignored if <seealso cref="ColumnHeaderTemplate"/> is set.
        /// </remarks>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public String ColumnHeaderStringFormat
        {
            get { return (String)GetValue(ColumnHeaderStringFormatProperty); }
            set { SetValue(ColumnHeaderStringFormatProperty, value); }
        }

        #endregion ColumnHeaderStringFormat

        #region AllowsColumnReorder

        /// <summary>
        /// AllowsColumnReorder DependencyProperty
        /// </summary>
        public static readonly StyledProperty<bool> AllowsColumnReorderProperty = GridView.AllowsColumnReorderProperty.AddOwner<GridViewHeaderRowPresenter>();

        /// <summary>
        /// Allow column re-order or not
        /// </summary>
        public bool AllowsColumnReorder
        {
            get { return (bool)GetValue(AllowsColumnReorderProperty); }
            set { SetValue(AllowsColumnReorderProperty, value); }
        }

        #endregion  AllowsColumnReorder

        #region ColumnHeaderContextMenu

        /// <summary>
        /// ColumnHeaderContextMenu DependencyProperty
        /// </summary>
        public static readonly StyledProperty<ContextMenu> ColumnHeaderContextMenuProperty =
            GridView.ColumnHeaderContextMenuProperty.AddOwner<GridViewHeaderRowPresenter>();
        // callback PropertyChanged

        /// <summary>
        /// ColumnHeaderContextMenu
        /// </summary>
        public ContextMenu ColumnHeaderContextMenu
        {
            get { return GetValue(ColumnHeaderContextMenuProperty); }
            set { SetValue(ColumnHeaderContextMenuProperty, value); }
        }

        #endregion  ColumnHeaderContextMenu

        #region ColumnHeaderToolTip

        /// <summary>
        /// ColumnHeaderToolTip DependencyProperty
        /// </summary>
        public static readonly StyledProperty<ToolTip> ColumnHeaderToolTipProperty =
            GridView.ColumnHeaderToolTipProperty.AddOwner<GridViewHeaderRowPresenter>();
        // callback Propertychanged

        /// <summary>
        /// ColumnHeaderToolTip
        /// </summary>
        public object ColumnHeaderToolTip
        {
            get { return GetValue(ColumnHeaderToolTipProperty); }
            set { SetValue(ColumnHeaderToolTipProperty, value); }
        }

        private static void PropertyChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            GridViewHeaderRowPresenter presenter = (GridViewHeaderRowPresenter)d;

            presenter.UpdateAllHeaders(e.Property);
        }

        #endregion  ColumnHeaderToolTip

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
        /// <returns>The GridViewHeaderRowPresenter's desired size.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            GridViewColumnCollection columns = Columns;

            Controls children = InternalChildren;

            double maxHeight = 0.0;           // Max height of children.
            double accumulatedWidth = 0.0;    // Total width consumed by children.
            double constraintHeight = constraint.Height;
            bool desiredWidthListEnsured = false;

            if (columns != null)
            {
                // Measure working headers
                for (int i = 0; i < columns.Count; ++i)
                {
                    IControl child = children[GetVisualIndex(i)];
                    if (child == null) { continue; }

                    double childConstraintWidth = Math.Max(0.0, constraint.Width - accumulatedWidth);

                    GridViewColumn column = columns[i];

                    if (column.State == ColumnMeasureState.Init)
                    {
                        if (!desiredWidthListEnsured)
                        {
                            EnsureDesiredWidthList();
                            LayoutUpdated += new EventHandler(OnLayoutUpdated);
                            desiredWidthListEnsured = true;
                        }

                        child.Measure(new Size(childConstraintWidth, constraintHeight));

                        DesiredWidthList[column.ActualIndex] = column.EnsureWidth(child.DesiredSize.Width);

                        accumulatedWidth += column.DesiredWidth;
                    }
                    else if (column.State == ColumnMeasureState.Headered
                        || column.State == ColumnMeasureState.Data)
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
            }

            // Measure padding header
            Debug.Assert(_paddingHeader != null, "padding header is null");
            _paddingHeader.Measure(new Size(0.0, constraintHeight));
            maxHeight = Math.Max(maxHeight, _paddingHeader.DesiredSize.Height);

            // reserve space for padding header next to the last column
            accumulatedWidth += c_PaddingHeaderMinWidth;

            // Measure indicator & floating header in re-ordering
            if (_isHeaderDragging)
            {
                Debug.Assert(_indicator != null, "_indicator is null");
                Debug.Assert(_floatingHeader != null, "_floatingHeader is null");

                // Measure indicator
                _indicator.Measure(constraint);

                // Measure floating header
                _floatingHeader.Measure(constraint);
            }

            return (new Size(accumulatedWidth, maxHeight));
        }

        /// <summary>
        /// GridViewHeaderRowPresenter computes the position of its children inside each child's Margin and calls Arrange
        /// on each child.
        /// </summary>
        /// <param name="arrangeSize">Size the GridViewHeaderRowPresenter will assume.</param>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            GridViewColumnCollection columns = Columns;

            Controls children = InternalChildren;

            double accumulatedWidth = 0.0;
            double remainingWidth = arrangeSize.Width;
            Rect rect;

            HeadersPositionList.Clear();

            if (columns != null)
            {
                // Arrange working headers
                for (int i = 0; i < columns.Count; ++i)
                {
                    IControl child = children[GetVisualIndex(i)];
                    if (child == null) { continue; }

                    GridViewColumn column = columns[i];

                    // has a given value or 'auto'
                    double childArrangeWidth = Math.Min(remainingWidth, ((column.State == ColumnMeasureState.SpecificWidth) ? column.Width : column.DesiredWidth));

                    // calculate the header rect
                    rect = new Rect(accumulatedWidth, 0.0, childArrangeWidth, arrangeSize.Height);

                    // arrange header
                    child.Arrange(rect);

                    //Store rect in HeadersPositionList as i-th column position
                    HeadersPositionList.Add(rect);

                    remainingWidth -= childArrangeWidth;
                    accumulatedWidth += childArrangeWidth;
                }

                // check width to hide previous header's right half gripper, from the first working header to padding header
                // only happens after column delete, insert, move
                if (_isColumnChangedOrCreated)
                {
                    for (int i = 0; i < columns.Count; ++i)
                    {
                        GridViewColumnHeader header = children[GetVisualIndex(i)] as GridViewColumnHeader;

                        header.CheckWidthForPreviousHeaderGripper();
                    }

                    _paddingHeader.CheckWidthForPreviousHeaderGripper();

                    _isColumnChangedOrCreated = false;
                }
            }

            // Arrange padding header
            Debug.Assert(_paddingHeader != null, "padding header is null");
            rect = new Rect(accumulatedWidth, 0.0, Math.Max(remainingWidth, 0.0), arrangeSize.Height);
            _paddingHeader.Arrange(rect);
            HeadersPositionList.Add(rect);

            // if re-order started, arrange floating header & indicator
            if (_isHeaderDragging)
            {
                _floatingHeader.Arrange(new Rect(new Point(_currentPos.X - _relativeStartPos.X, 0), HeadersPositionList[_startColumnIndex].Size));

                Point pos = FindPositionByIndex(_desColumnIndex);
                _indicator.Arrange(new Rect(pos, new Size(_indicator.DesiredSize.Width, arrangeSize.Height)));
            }

            return arrangeSize;
        }

        /// <summary>
        /// This is the method that responds to the MouseButtonEvent event.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(null).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
            {
                GridViewColumnHeader header = e.Source as GridViewColumnHeader;

                if (header != null && AllowsColumnReorder)
                {
                    PrepareHeaderDrag(header, e.GetPosition(this), e.GetPosition(header), false);

                    MakeParentItemsControlGotFocus();
                }

                e.Handled = true;
            }
            base.OnPointerPressed(e);
        }

        /// <summary>
        /// This is the method that responds to the MouseButtonEvent event.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (e.GetCurrentPoint(null).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
            {
                // Important to clean the prepare dragging state
                _prepareDragging = false;

                if (_isHeaderDragging)
                {
                    FinishHeaderDrag(false);
                }

                e.Handled = true;
            }

            base.OnPointerReleased(e);
        }

        /// <summary>
        /// This is the method that responds to the MouseEvent event.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            {
                // if prepare re-order or already re-order
                if (_prepareDragging)
                {
                    Debug.Assert(_draggingSrcHeader != null, "_draggingSrcHeader is null");

                    _currentPos = e.GetPosition(this);
                    _desColumnIndex = FindIndexByPosition(_currentPos, true);

                    if (!_isHeaderDragging)
                    {
                        // Re-order begins only if horizontal move exceeds threshold
                        if (CheckStartHeaderDrag(_currentPos, _startPos))
                        {
                            // header dragging start
                            StartHeaderDrag();

                            // need to measure indicator because floating header is updated
                            InvalidateMeasure();
                        }
                    }
                    else // NOTE: Not-Dragging/Dragging should be divided into two stages in MouseMove
                    {
                        // Check floating & indicator visibility
                        // Display floating header if vertical move not exceeds header.Height * 2
                        bool isDisplayingFloatingHeader = IsMousePositionValid(_floatingHeader, _currentPos, 2.0);

                        // floating header and indicator are visibile/invisible at the same time
                        _indicator.IsVisible = _floatingHeader.IsVisible = isDisplayingFloatingHeader;

                        InvalidateArrange();
                    }
                }
            }

            e.Handled = true;
        }

        /// <summary>
        /// Cancel header dragging if we lost capture
        /// </summary>
        /// <param name="e"></param>
        /*protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);

            // OnLostMouseCapture is invoked before OnMouseLeftButtonUp, so we need to distinguish
            // the cause of capture lose
            //      if LeftButton is pressed when lost mouse capture, we treat it as cancel
            //      Because GridViewHeaderRowPresenter never capture Mouse (GridViewColumnHeader did this),
            //      the Mouse.Captured is always null
            if (e.LeftButton == MouseButtonState.Pressed && _isHeaderDragging)
            {
                FinishHeaderDrag(true);
            }

            // Important to clean the prepare dragging state
            _prepareDragging = false;
        }*/

        #endregion Protected Methods

        //-------------------------------------------------------------------
        //
        // Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// create the headers tree
        /// </summary>
        /*internal override void OnPreApplyTemplate()
        {
            //  +-- GridViewHeaderRowPresenter ----------------------------+
            //  |                                                          |
            //  |  +- Header1 ---+ +- Header2 ---+ +- PaddingHeader -+     |
            //  |  |         +--------+      +--------+            |       |
            //  |  |         +Gripper +      +Gripper +            |  ...  |
            //  |  |         +--------+      +--------+            |       |
            //  |  +-------------+ +-------------+ +---------------+       |
            //  +----------------------------------------------------------+

            //
            //  Given a column collection with 3 columns
            //
            //      { A, B, C },
            //
            //  Visually, GridViewHeaderRowPresenter will them present as:
            //
            //      { A, B, C, Padding }
            //
            //  If in Reorder-Mode, there will be 2 more be visible:
            //
            //      { A, [Indctr],     B,      C,    Padding    }
            //      {           [-- Float --]                 }
            //
            //  And internally the visual child collection is in order of:
            //
            //      { Padding, C, B, A, Indicator, Float}
            //
            //  Method GetVisualIndex() is for coverting a Columns based
            //  index to visual child collection based index.
            //
            //  E.g., Columns based index of column A is 0, while
            //  and it lives 3rd in the visual collection, so
            //
            //      GetVisualIndex(0) = 3.
            //

            base.OnPreApplyTemplate();

            if (NeedUpdateVisualTree)
            {
                // build the whole collection from draft.

                // IMPORTANT!
                // The correct sequence to build the VisualTree in Z-order:
                // 1. Padding header
                // 2. The working Column header (if any)
                // 3. Indicator
                // 4. Floating header
                //

                UIElementCollection children = InternalChildren;
                GridViewColumnCollection columns = Columns;

                // renew ScrollViewer, ScrollChanged event, ItemsControl and KeyDown event
                RenewEvents();

                if (children.Count == 0)
                {
                    // Build and add the padding header, even if no GridViewColumn is defined
                    AddPaddingColumnHeader();

                    // Create and add indicator
                    AddIndicator();

                    // Create and add floating header
                    AddFloatingHeader(null);
                }
                else if (children.Count > 3)
                {
                    // clear column headers left from last view.
                    int count = children.Count - 3;
                    for (int i = 0; i < count; i++)
                    {
                        RemoveHeader(null, 1);
                    }
                }

                UpdatePaddingHeader(_paddingHeader);

                //
                // Build the column header.
                // The interesting thing is headers must be built from right to left,
                // in order to make the left header's gripper overlay the right header
                //
                if (columns != null)
                {
                    int visualIndex = 1;

                    for (int columnIndex = columns.Count - 1; columnIndex >= 0; columnIndex--)
                    {
                        GridViewColumn column = columns[columnIndex];

                        GridViewColumnHeader header = CreateAndInsertHeader(column, visualIndex++);
                    }
                }

                // Link headers
                BuildHeaderLinks();

                NeedUpdateVisualTree = false;

                _isColumnChangedOrCreated = true;
            }
        }*/

        /// <summary>
        /// Override column's PropertyChanged event handler. Update  correspondent
        /// property if change is of Width / Header /
        /// HeaderContainerStyle / Template / Selector.
        /// </summary>
        internal override void OnColumnPropertyChanged(GridViewColumn column, string propertyName)
        {
            Debug.Assert(column != null);
            if (column.ActualIndex >= 0)
            {
                GridViewColumnHeader header = FindHeaderByColumn(column);
                if (header != null)
                {
                    if (GridViewColumn.WidthProperty.Name.Equals(propertyName)
                        || GridViewColumn.c_ActualWidthName.Equals(propertyName))
                    {
                        InvalidateMeasure();
                    }
                    else if (GridViewColumn.HeaderProperty.Name.Equals(propertyName))
                    {
                        if (!header.IsInternalGenerated /* the old header is its own container */
                            || column.Header is GridViewColumnHeader /* the new header is its own container */)
                        {
                            // keep the header index in Children collection
                            int i = InternalChildren.IndexOf(header);

                            // Remove the old header
                            RemoveHeader(header, -1);

                            // Insert a (the) new header
                            GridViewColumnHeader newHeader = CreateAndInsertHeader(column, i);

                            // Link headers
                            BuildHeaderLinks();
                        }
                        else
                        {
                            UpdateHeaderContent(header);
                        }
                    }
                    else
                    {
                        AvaloniaProperty columnDP = GetColumnDPFromName(propertyName);

                        if (columnDP != null)
                        {
                            UpdateHeaderProperty(header, columnDP);
                        }
                    }
                }
            }
        }

        internal override void OnColumnCollectionChanged(GridViewColumnCollectionChangedEventArgs e)
        {
            base.OnColumnCollectionChanged(e);
            
            int index;
            GridViewColumnHeader header;
            Controls children = Children;
            GridViewColumn column;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Move:
                    int start = GetVisualIndex(e.OldStartingIndex);
                    int end = GetVisualIndex(e.NewStartingIndex);

                    header = (GridViewColumnHeader)children[start];
                    children.RemoveAt(start);
                    children.Insert(end, header);

                    break;

                case NotifyCollectionChangedAction.Add:
                    index = GetVisualIndex(e.NewStartingIndex);
                    column = (GridViewColumn)(e.NewItems[0]);

                    CreateAndInsertHeader(column, index + 1); // index + 1 because visual index is reversed from column index

                    break;

                case NotifyCollectionChangedAction.Remove:
                    RemoveHeader(null, GetVisualIndex(e.OldStartingIndex));

                    break;

                case NotifyCollectionChangedAction.Replace:
                    index = GetVisualIndex(e.OldStartingIndex);
                    RemoveHeader(null, index);

                    column = (GridViewColumn)(e.NewItems[0]);
                    CreateAndInsertHeader(column, index);

                    break;

                case NotifyCollectionChangedAction.Reset:
                    int count = e.ClearedColumns.Count;
                    for (int i = 0; i < count; i++)
                    {
                        RemoveHeader(null, 1);
                    }

                    break;
            }

            // Link headers
            BuildHeaderLinks();
            _isColumnChangedOrCreated = true;
        }

        // Make the parent got focus if it's ItemsControl
        // make this method internal, so GVCH can call it when header is invoked through access key
        internal void MakeParentItemsControlGotFocus()
        {
            /*if (_itemsControl != null && !_itemsControl.IsKeyboardFocusWithin)
            {
                // send focus to item.
                ListBox parent = _itemsControl as ListBox;
                if (parent != null && parent.LastActionItem != null)
                {
                    parent.LastActionItem.Focus();
                }
                else
                {
                    _itemsControl.Focus();
                }
            }*/
        }

        /// <summary>
        /// Refresh a dp of a header. Column is the 1st priority source, GridView 2nd.
        /// </summary>
        /// <param name="header">the header to update</param>
        /// <param name="property">the DP which trigger this update</param>
        internal void UpdateHeaderProperty(GridViewColumnHeader header, AvaloniaProperty property)
        {
            AvaloniaProperty gvDP, columnDP, headerDP;
            GetMatchingDPs(property, out gvDP, out columnDP, out headerDP);

            UpdateHeaderProperty(header, headerDP, columnDP, gvDP);
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

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
                        if (column.State == ColumnMeasureState.Init)
                        {
                            column.State = ColumnMeasureState.Headered;
                        }

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

        // Map column collection index to header collection index in visual tree
        private int GetVisualIndex(int columnIndex)
        {
            // Elements in visual tree: working headers, padding header, indicator, and floating header
            int index = InternalChildren.Count - 3 - columnIndex;
            Debug.Assert(index >= 0 && index < InternalChildren.Count, "Error index when GetVisualIndex");

            return index;
        }

        // Link headers from right to left
        private void BuildHeaderLinks()
        {
            GridViewColumnHeader lastHeader = null;

            if (Columns != null)
            {
                // link working headers.
                for (int i = 0; i < Columns.Count; i++)
                {
                    GridViewColumnHeader header = (GridViewColumnHeader)InternalChildren[GetVisualIndex(i)];
                    header.PreviousVisualHeader = lastHeader;
                    lastHeader = header;
                }
            }

            // link padding header to last header
            if (_paddingHeader != null)
            {
                _paddingHeader.PreviousVisualHeader = lastHeader;
            }
        }

        //
        // This method will do following tasks:
        //  1. Disconnect header from visual parent and logical parent, if any.
        //  2. Create a new header or use the header directly if the GridViewColumn.Header property
        //      is qualify for its own container;
        //  3. Insert the header in the InternalChildren collection
        //  4. Perform routine update and hookup jobs
        //
        private GridViewColumnHeader CreateAndInsertHeader(GridViewColumn column, int index)
        {
            object header = column.Header;
            GridViewColumnHeader headerContainer = header as GridViewColumnHeader;

            //
            // NOTE: when theme chagned, all properties, templates and styles will be reevaluated.
            // But there are 2 cases we need to handle:
            //
            //  1: header property is a qualified container for itself
            //    <GridViewColumn>
            //        <GridViewColumnHeader />
            //    </GridViewColumn>
            //
            //  2: header property is a Visual element
            //    <GridViewColumn>
            //        <Button />
            //    </GridViewColumn>
            //
            // In both cases, we need to diconnect them from the visual and logical tree before
            // they got inserted into the new tree.
            //
            if (header != null)
            {
                AvaloniaObject d = header as AvaloniaObject;

                if (d != null)
                {
                    // disconnect from visual tree
                    Visual headerAsVisual = d as Visual;

                    if (headerAsVisual != null)
                    {
                        Visual parent = headerAsVisual.Parent as Visual;

                        if (parent != null)
                        {
                            if (headerContainer != null)
                            {
                                // case 1
                                GridViewHeaderRowPresenter parentAsGVHRP = parent as GridViewHeaderRowPresenter;
                                if (parentAsGVHRP != null)
                                {
                                    parentAsGVHRP.InternalChildren.Remove(headerContainer);
                                }
                                else
                                {
                                    Debug.Assert(false, "Head is container for itself, but parent is neither GridViewHeaderRowPresenter nor null.");
                                }
                            }
                            else
                            {
                                // case 2
                                GridViewColumnHeader parentAsGVCH = parent as GridViewColumnHeader;
                                if (parentAsGVCH != null)
                                {
                                    parentAsGVCH.ClearValue(ContentControl.ContentProperty);
                                }
                            }
                        }
                    }
// TODO something is off here
                    /*// disconnect from logical tree
                    AvaloniaObject logicalParent = d.Parent;

                    if (logicalParent != null)
                    {
                        LogicalTreeHelper.RemoveLogicalChild(logicalParent, header);
                    }*/
                }
            }

            if (headerContainer == null)
            {
                headerContainer = new GridViewColumnHeader();
                headerContainer.IsInternalGenerated = true;
            }

            // Pass column reference to GridViewColumnHeader
            headerContainer.SetValue(GridViewColumnHeader.ColumnProperty, column);

            // Hookup _itemsControl.KeyDown event for canceling resizing if user press 'Esc'.
            HookupItemsControlKeyboardEvent(headerContainer);

            InternalChildren.Insert(index, headerContainer);

            // NOTE: the order here is important!
            // Need to add headerContainer into visual tree first, then apply Style, Template etc.
            UpdateHeader(headerContainer);

            _gvHeadersValid = false;

            return headerContainer;
        }

        private void RemoveHeader(GridViewColumnHeader header, int index)
        {
            Debug.Assert(header != null || index != -1);

            _gvHeadersValid = false;

            if (header != null)
            {
                InternalChildren.Remove(header);
            }
            else
            {
                header = (GridViewColumnHeader)InternalChildren[index];
                InternalChildren.RemoveAt(index);
            }

            UnhookItemsControlKeyboardEvent(header);
        }

        // find needed elements and hook up events
        private void RenewEvents()
        {
            ScrollViewer oldHeaderSV = _headerSV;
            _headerSV = Parent as ScrollViewer;
            if (oldHeaderSV != _headerSV)
            {
                if (oldHeaderSV != null)
                {
                    oldHeaderSV.ScrollChanged -= OnHeaderScrollChanged;
                }
                if (_headerSV != null)
                {
                    _headerSV.ScrollChanged += OnHeaderScrollChanged;
                }
            }

            ScrollViewer oldSV = _mainSV; // backup the old value
            _mainSV = TemplatedParent as ScrollViewer;

            if (oldSV != _mainSV)
            {
                if (oldSV != null)
                {
                    oldSV.ScrollChanged -= OnMasterScrollChanged;
                }

                if (_mainSV != null)
                {
                    _mainSV.ScrollChanged += OnMasterScrollChanged;
                }
            }

            // hook up key down event from ItemsControl,
            // because GridViewColumnHeader and GridViewHeaderRowPresenter can not get focus
            ItemsControl oldIC = _itemsControl; // backup the old value
            _itemsControl = FindItemsControlThroughTemplatedParent(this);

            if (oldIC != _itemsControl)
            {
                if (oldIC != null)
                {
                    // NOTE: headers have unhooked the KeyDown event in RemoveHeader.

                    oldIC.KeyDown -= OnColumnHeadersPresenterKeyDown;
                }

                if (_itemsControl != null)
                {
                    // register to HeadersPresenter to cancel dragging
                    _itemsControl.KeyDown += OnColumnHeadersPresenterKeyDown;

                    // NOTE: headers will hookup the KeyDown event latter in CreateAndInsertHeader.
                }
            }

            //Set GridViewHeaderRowPresenter to ListView
            ListView lv = _itemsControl as ListView;
            if (lv != null && lv.View != null && lv.View is GridView)
            {
                ((GridView)lv.View).HeaderRowPresenter = this;
            }
        }


        private void UnhookItemsControlKeyboardEvent(GridViewColumnHeader header)
        {
            Debug.Assert(header != null);
            if (_itemsControl != null)
            {
                _itemsControl.KeyDown -= header.OnColumnHeaderKeyDown;
            }
        }

        private void HookupItemsControlKeyboardEvent(GridViewColumnHeader header)
        {
            Debug.Assert(header != null);
            if (_itemsControl != null)
            {
                _itemsControl.KeyDown += header.OnColumnHeaderKeyDown;
            }
        }

        // The following two scroll changed methods will not be called recursively and lead to dead loop.
        // When scrolling _masterSV, OnMasterScrollChanged will be called, so _headerSV also scrolled
        // to the same offset. Then, OnHeaderScrollChanged be called, and try to scroll _masterSV, but
        // it's already scrolled to that offset, so OnMasterScrollChanged will not be called.

        // When master scroll viewer changed its offset, change header scroll viewer accordingly
        private void OnMasterScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_headerSV != null && _mainSV == e.Source)
            {
               /// _headerSV.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
        }

        // When header scroll viewer changed its offset, change master scroll viewer accordingly
        private void OnHeaderScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_mainSV != null && _headerSV == e.Source)
            {
               /// _mainSV.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
        }

        // Create the last padding column header in GridViewHeaderRowPresenter
        private void AddPaddingColumnHeader()
        {
            GridViewColumnHeader paddingHeader = new GridViewColumnHeader();
            paddingHeader.IsInternalGenerated = true;
            paddingHeader.SetValue(GridViewColumnHeader.RoleProperty, GridViewColumnHeaderRole.Padding);

            paddingHeader.Content = null;
            paddingHeader.ContentTemplate = null;
            paddingHeader.MinWidth = 0;
            paddingHeader.Padding = new Thickness(0.0);
            paddingHeader.Width = double.NaN;
            paddingHeader.HorizontalAlignment = Layout.HorizontalAlignment.Stretch;

            InternalChildren.Add(paddingHeader);
            _paddingHeader = paddingHeader;
        }

        // Create the indicator for column re-ordering
        private void AddIndicator()
        {
            Separator indicator = new Separator();
            indicator.IsVisible = false;

            // Indicator style:
            //
            // <Setter Property="Margin" Value="0" />
            // <Setter Property="Width" Value="2" />
            // <Setter Property="Template">
            //   <Setter.Value>
            //     <ControlTemplate TargetType="{x:Type Separator}">
            //        <Border Background="#FF000080"/>
            //     </ControlTemplate>
            //   </Setter.Value>
            // </Setter>

            indicator.Margin = new Thickness(0);
            indicator.Width = 2.0;

            //FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            var border = new Border();
            border.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromUInt32(0xFF000080)));

            //TODO
            //ControlTemplate template = new ControlTemplate();
            //template.TargetType = typeof(Separator);
            //template.Content = border;
            //template.Seal();

            //indicator.Template = template;

            InternalChildren.Add(indicator);
            _indicator = indicator;
        }

        // Create the floating header
        private void AddFloatingHeader(GridViewColumnHeader srcHeader)
        {
            GridViewColumnHeader header;

            // #1426973: Because users may put subclassed header as the column header,
            //           we need to create the same type per source header as the floating one
            // Get source header's type
            Type headerType = (srcHeader != null ? srcHeader.GetType() : typeof(GridViewColumnHeader));

            try
            {
                // Instantiate the same type for floating header
                header = Activator.CreateInstance(headerType) as GridViewColumnHeader;
            }
            catch (MissingMethodException e)
            {
                throw new ArgumentException("Missing parameterless constructor for header type");
            }

            Debug.Assert(header != null, "Cannot instantiate GridViewColumnHeader in AddFloatingHeader");

            header.IsInternalGenerated = true;
            header.SetValue(GridViewColumnHeader.RoleProperty, GridViewColumnHeaderRole.Floating);
            header.IsVisible = false;

            InternalChildren.Add(header);
            _floatingHeader = header;
        }

        // Fill necessary properties in floating header
        private void UpdateFloatingHeader(GridViewColumnHeader srcHeader)
        {
            Debug.Assert(srcHeader != null, "srcHeader is null");
            Debug.Assert(_floatingHeader != null, "floating header is null");

            _floatingHeader.Classes = srcHeader.Classes;

            _floatingHeader.FloatSourceHeader = srcHeader;
            _floatingHeader.Width = srcHeader.Bounds.Width;
            _floatingHeader.Height = srcHeader.Bounds.Height;
            _floatingHeader.SetValue(GridViewColumnHeader.ColumnProperty, srcHeader.Column);
            _floatingHeader.IsVisible = false;

            // override floating header's MinWidth/MinHeight to disable users to change floating header's Width/Height
            _floatingHeader.MinWidth = srcHeader.MinWidth;
            _floatingHeader.MinHeight = srcHeader.MinHeight;

            object template = srcHeader.GetValue(GridViewColumnHeader.ContentTemplateProperty);
            if ((template != AvaloniaProperty.UnsetValue) && (template != null))
            {
                _floatingHeader.ContentTemplate = srcHeader.ContentTemplate;
            }

            if (!(srcHeader.Content is Visual))
            {
                _floatingHeader.Content = srcHeader.Content;
            }
        }

        /// <summary>
        /// This method is invoked when mouse move and left button is pressed.
        /// This method judges if the mouse move exceeds a threshold to start re-order.
        /// </summary>
        /// <param name="currentPos">The current mouse position, relative to GridViewHeaderRowPresenter</param>
        /// <param name="originalPos">The original start position, relative to GridViewHeaderRowPresenter</param>
        /// <returns></returns>
        private bool CheckStartHeaderDrag(Point currentPos, Point originalPos)
        {
            return (MathUtilities.GreaterThan(Math.Abs(currentPos.X - originalPos.X), c_thresholdX));
        }

        // Find ItemsControl through TemplatedParent
        private static ItemsControl FindItemsControlThroughTemplatedParent(GridViewHeaderRowPresenter presenter)
        {
            Control fe = presenter.TemplatedParent as Control;
            ItemsControl itemsControl = null;

            while (fe != null)
            {
                itemsControl = fe as ItemsControl;
                if (itemsControl != null)
                {
                    break;
                }
                fe = fe.TemplatedParent as Control;
            }

            return itemsControl;
        }

        private void OnColumnHeadersPresenterKeyDown(object sender, KeyEventArgs e)
        {
            // if press Escape when re-ordering, cancel re-order
            if (e.Key == Key.Escape && _isHeaderDragging)
            {
                // save the source header b/c FinishHeaderDrag will clear it
                GridViewColumnHeader srcHeader = _draggingSrcHeader;

                FinishHeaderDrag(true);
                PrepareHeaderDrag(srcHeader, _currentPos, _relativeStartPos, true);
                InvalidateArrange();
            }
        }

        // Find the column header from the visual tree by column
        private GridViewColumnHeader FindHeaderByColumn(GridViewColumn column)
        {
            GridViewColumnCollection columns = Columns;
            Controls children = InternalChildren;

            if (columns != null && children.Count > columns.Count)
            {
                int index = columns.IndexOf(column);

                if (index != -1)
                {
                    // Becuase column headers is generated from right to left
                    int visualIndex = GetVisualIndex(index);

                    GridViewColumnHeader header = children[visualIndex] as GridViewColumnHeader;

                    if (header.Column != column)
                    {
                        // NOTE: if user change the GridViewColumn.Header/HeaderStyle
                        // in the event handler of column move. And in such case, the header
                        // we found by the algorithm above will be fail. So we turn to below
                        // a more reliable one.

                        for (int i = 1; i < children.Count; i++)
                        {
                            header = children[i] as GridViewColumnHeader;

                            if (header != null && header.Column == column)
                            {
                                return header;
                            }
                        }
                    }
                    else
                    {
                        return header;
                    }
                }
            }

            return null;
        }

        // Find logic column index by position
        // If parameter 'findNearestColumn' is true, find the nearest column relative to mouse position
        private int FindIndexByPosition(Point startPos, bool findNearestColumn)
        {
            int index = -1;

            if (startPos.X < 0.0)
            {
                return 0;
            }

            for (int i = 0; i < HeadersPositionList.Count; i++)
            {
                index++;

                Rect rect = HeadersPositionList[i];
                double startX = rect.X;
                double endX = startX + rect.Width;

                if (MathUtilities.GreaterThanOrClose(startPos.X, startX) &&
                    MathUtilities.LessThanOrClose(startPos.X, endX))
                {
                    if (findNearestColumn)
                    {
                        double midX = (startX + endX) * 0.5;
                        if (MathUtilities.GreaterThanOrClose(startPos.X, midX))
                        {
                            // if not the padding header
                            if (i != HeadersPositionList.Count - 1)
                            {
                                index++;
                            }
                        }
                    }

                    break;
                }
            }

            return index;
        }

        // Find position by logic column index
        private Point FindPositionByIndex(int index)
        {
            Debug.Assert(index >= 0 && index < HeadersPositionList.Count, "wrong index");
            return new Point(HeadersPositionList[index].X, 0);
        }

        // Update header Content, Style, ContentTemplate, ContentTemplateSelector, ContentStringFormat, ContextMenu and ToolTip
        private void UpdateHeader(GridViewColumnHeader header)
        {
            UpdateHeaderContent(header);

            for (int i = 0, n = s_DPList[0].Length; i < n; i++)
            {
                UpdateHeaderProperty(header, s_DPList[2][i] /* header */,
                    s_DPList[1][i] /* column */, s_DPList[0][i] /* GV */);
            }
        }

        // Update Content of GridViewColumnHeader
        private void UpdateHeaderContent(GridViewColumnHeader header)
        {
            if (header != null && header.IsInternalGenerated)
            {
                GridViewColumn column = header.Column;
                if (column != null)
                {
                    if (column.Header == null)
                    {
                        header.ClearValue(ContentControl.ContentProperty);
                    }
                    else
                    {
                        header.Content = column.Header;
                    }
                }
            }
        }

        // Update Style, ContextMenu and ToolTip properties for padding header.
        // GridView.ColumnHeaderTemplate(Selector) and GridView.ColumnHeaderStringFormat
        // don't work on padding header.
        private void UpdatePaddingHeader(GridViewColumnHeader header)
        {
            ///UpdateHeaderProperty(header, ColumnHeaderContainerStyleProperty);

            UpdateHeaderProperty(header, ColumnHeaderContextMenuProperty);

            UpdateHeaderProperty(header, ColumnHeaderToolTipProperty);
        }

        private void UpdateAllHeaders(AvaloniaProperty dp)
        {
            AvaloniaProperty gvDP, columnDP, headerDP;
            GetMatchingDPs(dp, out gvDP, out columnDP, out headerDP);

            int iStart, iEnd;
            GetIndexRange(dp, out iStart, out iEnd);

            Controls children = InternalChildren;
            for (int i = iStart; i <= iEnd; i++)
            {
                GridViewColumnHeader header = children[i] as GridViewColumnHeader;
                if (header != null)
                {
                    UpdateHeaderProperty(header, headerDP, columnDP, gvDP);
                }
            }
        }

        /// <summary>
        /// get index range of header for need update
        /// </summary>
        /// <param name="dp">the GridView DP in this update</param>
        /// <param name="iStart">starting index of header need update</param>
        /// <param name="iEnd">ending index of header need update</param>
        private void GetIndexRange(AvaloniaProperty dp, out int iStart, out int iEnd)
        {
            // whether or not include the padding header
            iStart = (//dp == ColumnHeaderTemplateProperty ||
                        dp == ColumnHeaderStringFormatProperty)
                    ? 1 : 0;

            iEnd = InternalChildren.Count - 3;  // skip the floating header and the indicator.
        }

        private void UpdateHeaderProperty(
            GridViewColumnHeader header,    // the header need to update
            AvaloniaProperty targetDP,    // the target DP on header
            AvaloniaProperty columnDP,    // the DP on Column as 1st source, can be Null
            AvaloniaProperty gvDP         // the DP on GridView as 2nd source
            )
        {
            /*if (gvDP == ColumnHeaderContainerStyleProperty
                && header.Role == GridViewColumnHeaderRole.Padding)
            {
                // Because padding header has no chance to be instantiated by a sub-classed GridViewColumnHeader,
                // we ignore GridView.ColumnHeaderContainerStyle silently if its TargetType is not GridViewColumnHeader (or parent)
                // I.e. for padding header, only accept the GridViewColumnHeader as TargetType
                Style style = ColumnHeaderContainerStyle;
                if (style != null && !style.TargetType.IsAssignableFrom(typeof(GridViewColumnHeader)))
                {
                    // use default style for padding header in this case
                    header.Style = null;
                    return;
                }
            }*/

            GridViewColumn column = header.Column;

            object value = null;

            if (column != null /* not the padding one */
                && columnDP != null /* Column doesn't has ContextMenu property*/)
            {
                value = column.GetValue(columnDP);
            }

            if (value == null)
            {
                value = this.GetValue(gvDP);
            }

            header.UpdateProperty(targetDP, value);
        }

        // Prepare column header re-ordering
        private void PrepareHeaderDrag(GridViewColumnHeader header, Point pos, Point relativePos, bool cancelInvoke)
        {
            if (header.Role == GridViewColumnHeaderRole.Normal)
            {
                _prepareDragging = true;
                _isHeaderDragging = false;
                _draggingSrcHeader = header;

                _startPos = pos;
                _relativeStartPos = relativePos;

                if (!cancelInvoke)
                {
                    _startColumnIndex = FindIndexByPosition(_startPos, false);
                }
            }
        }

        // Start header drag
        private void StartHeaderDrag()
        {
            _startPos = _currentPos;
            _isHeaderDragging = true;

            // suppress src header's click event
            _draggingSrcHeader.SuppressClickEvent = true;

            // lock Columns during header dragging
            if (Columns != null)
            {
                Columns.BlockWrite();
            }

            // Remove the old floating header,
            // then create & add the new one per the source header's type
            InternalChildren.Remove(_floatingHeader);
            AddFloatingHeader(_draggingSrcHeader);

            UpdateFloatingHeader(_draggingSrcHeader);
        }

        // Finish header drag
        private void FinishHeaderDrag(bool isCancel)
        {
            // clear related fields
            _prepareDragging = false;
            _isHeaderDragging = false;

            // restore src header's click event
            _draggingSrcHeader.SuppressClickEvent = false;

            _floatingHeader.IsVisible = false;
            _floatingHeader.ResetFloatingHeaderCanvasBackground();

            _indicator.IsVisible = false;

            // unlock Columns during header dragging
            if (Columns != null)
            {
                Columns.UnblockWrite();
            }

            // if cancelled, do nothing
            if (!isCancel)
            {
                // Display floating header if vertical move not exceeds header.Height * 2
                bool isMoveHeader = IsMousePositionValid(_floatingHeader, _currentPos, 2.0);

                Debug.Assert(Columns != null, "Columns is null in OnHeaderDragCompleted");

                // Revise the destinate column index
                int newColumnIndex = (_startColumnIndex >= _desColumnIndex) ? _desColumnIndex : _desColumnIndex - 1;

                if (isMoveHeader)
                {
                    Columns.Move(_startColumnIndex, newColumnIndex);
                }
            }
        }

        // check if the Mouse position is in the given valid area
        private static bool IsMousePositionValid(Control floatingHeader, Point currentPos, double arrange)
        {
            // valid area: - height * arrange <= currentPos.Y <= height * ( arrange + 1)
            return MathUtilities.LessThanOrClose(-floatingHeader.Height * arrange, currentPos.Y) &&
                   MathUtilities.LessThanOrClose(currentPos.Y, floatingHeader.Height * (arrange + 1));
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        // Private Class / Properties / Fields
        //
        //-------------------------------------------------------------------

        #region Private Class / Properties / Fields

        //Return the actual column headers array
        internal List<GridViewColumnHeader> ActualColumnHeaders
        {
            get
            {
                if (_gvHeaders == null || !_gvHeadersValid)
                {
                    _gvHeadersValid = true;
                    _gvHeaders = new List<GridViewColumnHeader>();
                    if (Columns != null)
                    {
                        Controls children = InternalChildren;

                        for (int i = 0, count = Columns.Count; i < count; ++i)
                        {
                            GridViewColumnHeader header = children[GetVisualIndex(i)] as GridViewColumnHeader;
                            if (header != null)
                            {
                                _gvHeaders.Add(header);
                            }
                        }
                    }
                }

                return _gvHeaders;
            }
        }

        private bool _gvHeadersValid;
        private List<GridViewColumnHeader> _gvHeaders;

        // Store the column header's position in visual tree
        // including the padding header
        private List<Rect> HeadersPositionList
        {
            get
            {
                if (_headersPositionList == null)
                {
                    _headersPositionList = new List<Rect>();
                }

                return _headersPositionList;
            }
        }

        private List<Rect> _headersPositionList;

        private ScrollViewer _mainSV;
        private ScrollViewer _headerSV;
        private GridViewColumnHeader _paddingHeader;
        private GridViewColumnHeader _floatingHeader;
        private Separator _indicator;

        // parent ItemsControl
        private ItemsControl _itemsControl;

        // source header when header dragging
        private GridViewColumnHeader _draggingSrcHeader;

        // start position when dragging (position relative to GridViewHeaderRowPresenter)
        private Point _startPos;

        // relative start position when dragging (position relative to Header)
        private Point _relativeStartPos;

        // current mouse position (position relative to GridViewHeaderRowPresenter)
        private Point _currentPos;

        // start column index when begin dragging
        private int _startColumnIndex;

        // destination column index when finish dragging
        private int _desColumnIndex;

        // indicating if header is dragging
        private bool _isHeaderDragging;

        // indicating column is changed or created for the first
        private bool _isColumnChangedOrCreated;

        // indicating a mouse down, ready to drag the header
        private bool _prepareDragging;

        // the threshold for horizontal move when header dragging
        private const double c_thresholdX = 4.0;

        #endregion Private Properties

        #region DP resolve helper

        // resolve dp from name
        private static AvaloniaProperty GetColumnDPFromName(string dpName)
        {
            foreach (AvaloniaProperty dp in s_DPList[1])
            {
                if ((dp != null) && dpName.Equals(dp.Name))
                {
                    return dp;
                }
            }

            return null;
        }

        // resolve matching DPs from one
        private static void GetMatchingDPs(AvaloniaProperty indexDP,
            out AvaloniaProperty gvDP, out AvaloniaProperty columnDP, out AvaloniaProperty headerDP)
        {
            for (int i = 0; i < s_DPList.Length; i++)
            {
                for (int j = 0; j < s_DPList[i].Length; j++)
                {
                    if (indexDP == s_DPList[i][j])
                    {
                        gvDP = s_DPList[0][j];
                        columnDP = s_DPList[1][j];
                        headerDP = s_DPList[2][j];

                        goto found;
                    }
                }
            }

            gvDP = columnDP = headerDP = null;

        found:;
        }

        private static readonly AvaloniaProperty[][] s_DPList = new AvaloniaProperty[][]
        {
            // DPs on GridViewHeaderRowPresenter
            new AvaloniaProperty[] {
                //ColumnHeaderContainerStyleProperty,
                ColumnHeaderTemplateProperty,
                //ColumnHeaderTemplateSelectorProperty,
                ColumnHeaderStringFormatProperty,
                ColumnHeaderContextMenuProperty,
                ColumnHeaderToolTipProperty,
            },

            // DPs on GridViewColumn
            new AvaloniaProperty[] {
                //GridViewColumn.HeaderContainerStyleProperty,
                GridViewColumn.HeaderTemplateProperty,
                //GridViewColumn.HeaderTemplateSelectorProperty,
                GridViewColumn.HeaderStringFormatProperty,
                null,
                null,
            },

            // DPs on GridViewColumnHeader
            new AvaloniaProperty[] {
                //GridViewColumnHeader.StyleProperty,
                GridViewColumnHeader.ContentTemplateProperty,
                //GridViewColumnHeader.ContentTemplateSelectorProperty,
                null, //GridViewColumnHeader.ContentStringFormatProperty,
                GridViewColumnHeader.ContextMenuProperty,
                ToolTip.TipProperty,
            }
        };

        #endregion
    }
}
