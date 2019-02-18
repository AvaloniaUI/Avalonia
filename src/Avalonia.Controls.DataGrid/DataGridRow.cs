// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Utils;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Utilities;
using Avalonia.VisualTree;
using System;
using System.Diagnostics;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a <see cref="T:Avalonia.Controls.DataGrid" /> row.
    /// </summary>
    public class DataGridRow : TemplatedControl
    {
        #region Constants

        private const byte DATAGRIDROW_defaultMinHeight = 0;
        internal const int DATAGRIDROW_maximumHeight = 65536;
        internal const double DATAGRIDROW_minimumHeight = 0;

        private const string DATAGRIDROW_elementBottomGridLine = "PART_BottomGridLine";
        private const string DATAGRIDROW_elementCells = "PART_CellsPresenter";
        private const string DATAGRIDROW_elementDetails = "PART_DetailsPresenter";
        internal const string DATAGRIDROW_elementRoot = "PART_Root";
        internal const string DATAGRIDROW_elementRowHeader = "PART_RowHeader";

        #endregion

        /*
        private bool _animatingDetails;
        private Storyboard _detailsVisibleStoryboard;
        private DoubleAnimation _detailsHeightAnimation;
        private double? _detailsHeightAnimationToOverride;
        */

        #region Fields


        private DataGridCellsPresenter _cellsElement;
        private DataGridCell _fillerCell;
        private DataGridRowHeader _headerElement;
        private double _lastHorizontalOffset;
        private int? _mouseOverColumnIndex;
        private bool _isValid = true;
        private Rectangle _bottomGridLine;
        private bool _areHandlersSuspended;


        // In the case where Details scales vertically when it's arranged at a different width, we
        // get the wrong height measurement so we need to check it again after arrange
        private bool _checkDetailsContentHeight;
        // Optimal height of the details based on the Element created by the DataTemplate
        private double _detailsDesiredHeight;
        private bool _detailsLoaded;
        private bool _detailsVisibilityNotificationPending;
        private IControl _detailsContent;
        private IDisposable _detailsContentSizeSubscription;
        private DataGridDetailsPresenter _detailsElement;
        // Locally cache whether or not details are visible so we don't run redundant storyboards
        // The Details Template that is actually applied to the Row
        private IDataTemplate _appliedDetailsTemplate;
        private bool? _appliedDetailsVisibility;

        #endregion

        #region Avalonia Properties

        /// <summary>
        /// Identifies the Header dependency property.
        /// </summary>
        public static readonly StyledProperty<object> HeaderProperty =
            AvaloniaProperty.Register<DataGridRow, object>(nameof(Header));

        /// <summary>
        /// Gets or sets the row header.
        /// </summary>
        public object Header
        {
            get { return GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }
        
        public static readonly DirectProperty<DataGridRow, bool> IsValidProperty =
            AvaloniaProperty.RegisterDirect<DataGridRow, bool>(
                nameof(IsValid),
                o => o.IsValid);

        /// <summary>
        /// Gets a value that indicates whether the data in a row is valid. 
        /// </summary>
        public bool IsValid
        {
            get { return _isValid; }
            internal set { SetAndRaise(IsValidProperty, ref _isValid, value); }
        }


        public static readonly StyledProperty<IDataTemplate> DetailsTemplateProperty =
            AvaloniaProperty.Register<DataGridRow, IDataTemplate>(nameof(DetailsTemplate));

        /// <summary>
        /// Gets or sets the template that is used to display the details section of the row.
        /// </summary>
        public IDataTemplate DetailsTemplate
        {
            get { return GetValue(DetailsTemplateProperty); }
            set { SetValue(DetailsTemplateProperty, value); }
        }


        public static readonly StyledProperty<bool> AreDetailsVisibleProperty =
            AvaloniaProperty.Register<DataGridRow, bool>(nameof(AreDetailsVisible));

        /// <summary>
        /// Gets or sets a value that indicates when the details section of the row is displayed.
        /// </summary>
        public bool AreDetailsVisible
        {
            get { return GetValue(AreDetailsVisibleProperty); }
            set { SetValue(AreDetailsVisibleProperty, value); }
        }
        
        #endregion


        static DataGridRow()
        {
            HeaderProperty.Changed.AddClassHandler<DataGridRow>(x => x.OnHeaderChanged);
            DetailsTemplateProperty.Changed.AddClassHandler<DataGridRow>(x => x.OnDetailsTemplateChanged);
            AreDetailsVisibleProperty.Changed.AddClassHandler<DataGridRow>(x => x.OnAreDetailsVisibleChanged);

            PointerPressedEvent.AddClassHandler<DataGridRow>(x => x.DataGridRow_PointerPressed, handledEventsToo: true);
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Avalonia.Controls.DataGridRow" /> class. 
        /// </summary>
        public DataGridRow()
        {
            MinHeight = DATAGRIDROW_defaultMinHeight;

            Index = -1;
            IsValid = true;
            Slot = -1;
            _mouseOverColumnIndex = null;
            _detailsDesiredHeight = double.NaN;
            _detailsLoaded = false;
            _appliedDetailsVisibility = false;
            Cells = new DataGridCellCollection(this);
            Cells.CellAdded += DataGridCellCollection_CellAdded;
            Cells.CellRemoved += DataGridCellCollection_CellRemoved;
        }

        private void SetValueNoCallback<T>(AvaloniaProperty<T> property, T value, BindingPriority priority = BindingPriority.LocalValue)
        {
            _areHandlersSuspended = true;
            try
            {
                SetValue(property, value, priority);
            }
            finally
            {
                _areHandlersSuspended = false;
            }
        }

        private void OnHeaderChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (_headerElement != null)
            {
                _headerElement.Content = e.NewValue;
            }
        }

        private void OnDetailsTemplateChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var oldValue = (IDataTemplate)e.OldValue;
            var newValue = (IDataTemplate)e.NewValue;
            
            if (!_areHandlersSuspended && OwningGrid != null)
            {
                IDataTemplate actualDetailsTemplate(IDataTemplate template) => (template ?? OwningGrid.RowDetailsTemplate);

                // We don't always want to apply the new Template because they might have set the same one
                // we inherited from the DataGrid
                if (actualDetailsTemplate(newValue) != actualDetailsTemplate(oldValue))
                {
                    ApplyDetailsTemplate(initializeDetailsPreferredHeight: false);
                }
            }
        }

        private void OnAreDetailsVisibleChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (!_areHandlersSuspended)
            {
                if (OwningGrid == null)
                {
                    throw DataGridError.DataGrid.NoOwningGrid(this.GetType());
                }
                if (Index == -1)
                {
                    throw DataGridError.DataGridRow.InvalidRowIndexCannotCompleteOperation();
                }

                var newValue = (bool)e.NewValue;
                OwningGrid.OnRowDetailsVisibilityPropertyChanged(Index, newValue);
                SetDetailsVisibilityInternal(newValue, raiseNotification: true, animate: true);
            }
        }

        #region Properties

        internal DataGrid OwningGrid
        {
            get;
            set;
        }
        
        /// <summary>
        /// Index of the row
        /// </summary>
        internal int Index
        {
            get;
            set;
        }
        
        internal double ActualBottomGridLineHeight
        {
            get
            {
                if (_bottomGridLine != null && OwningGrid != null && OwningGrid.AreRowBottomGridLinesRequired)
                {
                    // Unfortunately, _bottomGridLine has no size yet so we can't get its actualheight
                    return DataGrid.HorizontalGridLinesThickness;
                }
                return 0;
            }
        }

        internal DataGridCellCollection Cells
        {
            get;
            private set;
        }

        //TODO Styles
        internal DataGridCell FillerCell
        {
            get
            {
                if (_fillerCell == null)
                {
                    _fillerCell = new DataGridCell
                    {
                        IsVisible = false,
                        OwningRow = this
                    };
                    //_fillerCell.EnsureStyle(null);
                    if (_cellsElement != null)
                    {
                        _cellsElement.Children.Add(_fillerCell);
                    }
                }
                return _fillerCell;
            }
        }

        internal bool HasBottomGridLine
        {
            get
            {
                return _bottomGridLine != null;
            }
        }

        internal bool HasHeaderCell
        {
            get
            {
                return _headerElement != null;
            }
        }

        internal DataGridRowHeader HeaderCell
        {
            get
            {
                return _headerElement;
            }
        }

        internal bool IsEditing => OwningGrid != null && OwningGrid.EditingRow == this;

        /// <summary>
        /// Layout when template is applied
        /// </summary>
        internal bool IsLayoutDelayed
        {
            get;
            private set;
        }
        
        internal bool IsMouseOver
        {
            get
            {
                return OwningGrid != null && OwningGrid.MouseOverRowIndex == Index;
            }
            set
            {
                if (OwningGrid != null && value != IsMouseOver)
                {
                    if (value)
                    {
                        OwningGrid.MouseOverRowIndex = Index;
                    }
                    else
                    {
                        OwningGrid.MouseOverRowIndex = null;
                    }
                }
            }
        }

        internal bool IsRecycled
        {
            get;
            private set;
        }
        
        internal bool IsRecyclable
        {
            get
            {
                if (OwningGrid != null)
                {
                    return OwningGrid.IsRowRecyclable(this);
                }
                return true;
            }
        }
        
        internal bool IsSelected
        {
            get
            {
                if (OwningGrid == null || Slot == -1)
                {
                    // The Slot can be -1 if we're about to reuse or recycle this row, but the layout cycle has not
                    // passed so we don't know the outcome yet.  We don't care whether or not it's selected in this case
                    return false;
                }
                return OwningGrid.GetRowSelection(Slot);
            }
        }
        
        internal int? MouseOverColumnIndex
        {
            get
            {
                return _mouseOverColumnIndex;
            }
            set
            {
                if (_mouseOverColumnIndex != value)
                {
                    DataGridCell oldMouseOverCell = null;
                    if (_mouseOverColumnIndex != null && OwningGrid.IsSlotVisible(Slot))
                    {
                        if (_mouseOverColumnIndex > -1)
                        {
                            oldMouseOverCell = Cells[_mouseOverColumnIndex.Value];
                        }
                    }
                    _mouseOverColumnIndex = value;
                    if (oldMouseOverCell != null && IsVisible)
                    {
                        oldMouseOverCell.UpdatePseudoClasses();
                    }
                    if (_mouseOverColumnIndex != null && OwningGrid != null && OwningGrid.IsSlotVisible(Slot))
                    {
                        if (_mouseOverColumnIndex > -1)
                        {
                            Cells[_mouseOverColumnIndex.Value].UpdatePseudoClasses();
                        }
                    }
                }
            }
        } 

        internal Panel RootElement
        {
            get;
            private set;
        } 

        internal int Slot
        {
            get;
            set;
        }

        // Height that the row will eventually end up at after a possible detalis animation has completed
        internal double TargetHeight
        {
            get
            {
                if (!double.IsNaN(Height))
                {
                    return Height;
                }
                else if (_detailsElement != null && _appliedDetailsVisibility == true && _appliedDetailsTemplate != null)
                {
                    Debug.Assert(!double.IsNaN(_detailsElement.ContentHeight));
                    Debug.Assert(!double.IsNaN(_detailsDesiredHeight));
                    return DesiredSize.Height + _detailsDesiredHeight - _detailsElement.ContentHeight;
                }
                else
                {
                    return DesiredSize.Height;
                }
            }
        }

        #endregion

        #region Methods

        #region Public
        
        /// <summary>
        /// Returns the index of the current row.
        /// </summary>
        /// <returns>
        /// The index of the current row.
        /// </returns>
        public int GetIndex()
        {
            return Index;
        }

        /// <summary>
        /// Returns the row which contains the given element
        /// </summary>
        /// <param name="element">element contained in a row</param>
        /// <returns>Row that contains the element, or null if not found
        /// </returns>
        public static DataGridRow GetRowContainingElement(Control element)
        {
            // Walk up the tree to find the DataGridRow that contains the element
            IVisual parent = element;
            DataGridRow row = parent as DataGridRow;
            while ((parent != null) && (row == null))
            {
                parent = parent.GetVisualParent();
                row = parent as DataGridRow;
            }
            return row;
        }

        #endregion

        #region Protected
        
        /// <summary>
        /// Arranges the content of the <see cref="T:Avalonia.Controls.DataGridRow" />.
        /// </summary>
        /// <returns>
        /// The actual size used by the <see cref="T:Avalonia.Controls.DataGridRow" />.
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

            // If the DataGrid was scrolled horizontally after our last Arrange, we need to make sure
            // the Cells and Details are Arranged again
            if (_lastHorizontalOffset != OwningGrid.HorizontalOffset)
            {
                _lastHorizontalOffset = OwningGrid.HorizontalOffset;
                InvalidateHorizontalArrange();
            }

            Size size = base.ArrangeOverride(finalSize);

            if (_checkDetailsContentHeight)
            {
                _checkDetailsContentHeight = false;
                EnsureDetailsContentHeight();
            }

            if (RootElement != null)
            {
                foreach (Control child in RootElement.Children)
                {
                    if (DataGridFrozenGrid.GetIsFrozen(child))
                    {
                        TranslateTransform transform = new TranslateTransform();
                        // Automatic layout rounding doesn't apply to transforms so we need to Round this
                        transform.X = Math.Round(OwningGrid.HorizontalOffset);
                        child.RenderTransform = transform;
                    }
                }
            }

            if (_bottomGridLine != null)
            {
                RectangleGeometry gridlineClipGeometry = new RectangleGeometry();
                gridlineClipGeometry.Rect = new Rect(OwningGrid.HorizontalOffset, 0, Math.Max(0, DesiredSize.Width - OwningGrid.HorizontalOffset), _bottomGridLine.DesiredSize.Height);
                _bottomGridLine.Clip = gridlineClipGeometry;
            }

            return size;
        }

        /// <summary>
        /// Measures the children of a <see cref="T:Avalonia.Controls.DataGridRow" /> to 
        /// prepare for arranging them during the <see cref="M:System.Windows.FrameworkElement.ArrangeOverride(System.Windows.Size)" /> pass.
        /// </summary>
        /// <param name="availableSize">
        /// The available size that this element can give to child elements. Indicates an upper limit that child elements should not exceed.
        /// </param>
        /// <returns>
        /// The size that the <see cref="T:Avalonia.Controls.Primitives.DataGridRow" /> determines it needs during layout, based on its calculations of child object allocated sizes.
        /// </returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (OwningGrid == null)
            {
                return base.MeasureOverride(availableSize);
            }

            //Allow the DataGrid specific componets to adjust themselves based on new values
            if (_headerElement != null)
            {
                _headerElement.InvalidateMeasure();
            }
            if (_cellsElement != null)
            {
                _cellsElement.InvalidateMeasure();
            }
            if (_detailsElement != null)
            {
                _detailsElement.InvalidateMeasure();
            }

            Size desiredSize = base.MeasureOverride(availableSize);
            return desiredSize.WithWidth(Math.Max(desiredSize.Width, OwningGrid.CellsWidth));
        }

        /// <summary>
        /// Builds the visual tree for the column header when a new template is applied.
        /// </summary>
        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);

            RootElement = e.NameScope.Find<Panel>(DATAGRIDROW_elementRoot);
            if (RootElement != null)
            {
                EnsureBackground();
                UpdatePseudoClasses();
            }

            bool updateVerticalScrollBar = false;
            if (_cellsElement != null)
            {
                // If we're applying a new template, we  want to remove the cells from the previous _cellsElement
                _cellsElement.Children.Clear();
                updateVerticalScrollBar = true;
            }

            _cellsElement = e.NameScope.Find<DataGridCellsPresenter>(DATAGRIDROW_elementCells);
            if (_cellsElement != null)
            {
                _cellsElement.OwningRow = this;
                // Cells that were already added before the Template was applied need to
                // be added to the Canvas
                if (Cells.Count > 0)
                {
                    foreach (DataGridCell cell in Cells)
                    {
                        _cellsElement.Children.Add(cell);
                    }
                }
            }

            _detailsElement = e.NameScope.Find<DataGridDetailsPresenter>(DATAGRIDROW_elementDetails);
            if (_detailsElement != null && OwningGrid != null)
            {
                _detailsElement.OwningRow = this;
                if (ActualDetailsVisibility && ActualDetailsTemplate != null && _appliedDetailsTemplate == null)
                {
                    // Apply the DetailsTemplate now that the row template is applied.
                    SetDetailsVisibilityInternal(ActualDetailsVisibility, raiseNotification: _detailsVisibilityNotificationPending, animate: false);
                    _detailsVisibilityNotificationPending = false;
                }
            }

            _bottomGridLine = e.NameScope.Find<Rectangle>(DATAGRIDROW_elementBottomGridLine);
            EnsureGridLines();

            _headerElement = e.NameScope.Find<DataGridRowHeader>(DATAGRIDROW_elementRowHeader);
            if (_headerElement != null)
            {
                _headerElement.Owner = this;
                if (Header != null)
                {
                    _headerElement.Content = Header;
                }
                EnsureHeaderStyleAndVisibility(null);
            }

            //The height of this row might have changed after applying a new style, so fix the vertical scroll bar
            if (OwningGrid != null && updateVerticalScrollBar)
            {
                OwningGrid.UpdateVerticalScrollBar();
            }
        }

        protected override void OnPointerEnter(PointerEventArgs e)
        {
            base.OnPointerEnter(e);
            IsMouseOver = true;
        }
        protected override void OnPointerLeave(PointerEventArgs e)
        {
            IsMouseOver = false;
            base.OnPointerLeave(e);
        }

        #endregion

        #region Internal

        internal void ApplyCellsState()
        {
            foreach (DataGridCell dataGridCell in Cells)
            {
                dataGridCell.UpdatePseudoClasses();
            }
        }

        internal void ApplyHeaderStatus()
        {
            if (_headerElement != null && OwningGrid.AreRowHeadersVisible)
            {
                _headerElement.ApplyOwnerStatus();
            }
        }

        //TODO Implement
        internal void UpdatePseudoClasses()
        {
            PseudoClasses.Set(":selected", IsSelected);
            PseudoClasses.Set(":editing", IsEditing);
            if (RootElement != null && OwningGrid != null && IsVisible)
            {
                //Debug.Assert(Index != -1);
                //byte idealStateMappingIndex = 0;
                //if (IsSelected || IsEditing)
                //{
                //    idealStateMappingIndex += 8;
                //}
                //if (IsEditing)
                //{
                //    idealStateMappingIndex += 4;
                //}
                //if (IsMouseOver)
                //{
                //    idealStateMappingIndex += 2;
                //}
                //if (OwningGrid.ContainsFocus)
                //{
                //    idealStateMappingIndex += 1;
                //}

                //byte stateCode = _idealStateMapping[idealStateMappingIndex];
                //Debug.Assert(stateCode != DATAGRIDROW_stateNullCode);

                //string storyboardName;
                //string legacyStoryboardName;
                //while (stateCode != DATAGRIDROW_stateNullCode)
                //{
                //    if (stateCode == DATAGRIDROW_stateNormalCode)
                //    {
                //        if (Index % 2 == 1)
                //        {
                //            storyboardName = DATAGRIDROW_stateAlternate;
                //            legacyStoryboardName = DATAGRIDROW_stateAlternateLegacy;
                //        }
                //        else
                //        {
                //            storyboardName = DATAGRIDROW_stateNormal;
                //            legacyStoryboardName = DATAGRIDROW_stateNormal;
                //        }
                //    }
                //    else
                //    {
                //        storyboardName = _stateNames[stateCode];
                //        legacyStoryboardName = _legacyStateNames[stateCode];
                //    }
                //    if (VisualStateManager.GoToState(this, storyboardName, animate) || VisualStateManager.GoToState(this, legacyStoryboardName, animate))
                //    {
                //        break;
                //    }
                //    else
                //    {
                //        // The state wasn't implemented so fall back to the next one
                //        stateCode = _fallbackStateMapping[stateCode];
                //    }
                //}

                //if (IsValid)
                //{
                //    VisualStates.GoToState(this, animate, VisualStates.StateValid);
                //}
                //else
                //{
                //    VisualStates.GoToState(this, animate, VisualStates.StateInvalid, VisualStates.StateValid);
                //}

                ApplyHeaderStatus();
            } 
        }
        
        //TODO Animation
        internal void DetachFromDataGrid(bool recycle)
        {
            UnloadDetailsTemplate(recycle);

            if (recycle)
            {
                IsRecycled = true;

                if (_cellsElement != null)
                {
                    _cellsElement.Recycle();
                }

                _checkDetailsContentHeight = false;

                // Clear out the old Details cache so it won't be reused for other data
                //_detailsDesiredHeight = double.NaN;
                if (_detailsElement != null)
                {
                    _detailsElement.ClearValue(DataGridDetailsPresenter.ContentHeightProperty);
                }
            }

            //StopDetailsAnimation();

            Slot = -1;
        }

        // Make sure the row's background is set to its correct value.  It could be explicity set or inherit
        // DataGrid.RowBackground or DataGrid.AlternatingRowBackground
        internal void EnsureBackground()
        {
            // Inherit the DataGrid's RowBackground properties only if this row doesn't explicity have a background set
            if (RootElement != null && OwningGrid != null)
            {
                IBrush newBackground = null;
                if (Background == null)
                {
                    if (Index % 2 == 0 || OwningGrid.AlternatingRowBackground == null)
                    {
                        // Use OwningGrid.RowBackground if the index is even or if the OwningGrid.AlternatingRowBackground is null
                        if (OwningGrid.RowBackground != null)
                        {
                            newBackground = OwningGrid.RowBackground;
                        }
                    }
                    else
                    {
                        // Alternate row
                        if (OwningGrid.AlternatingRowBackground != null)
                        {
                            newBackground = OwningGrid.AlternatingRowBackground;
                        }
                    }
                }
                else
                {
                    newBackground = Background;
                }

                if (RootElement.Background != newBackground)
                {
                    RootElement.Background = newBackground;
                }
            }
        }

        internal void EnsureFillerVisibility()
        {
            if (_cellsElement != null)
            {
                _cellsElement.EnsureFillerVisibility();
            }
        }

        internal void EnsureGridLines()
        {
            if (OwningGrid != null)
            {
                if (_bottomGridLine != null)
                {
                    // It looks like setting Visibility sometimes has side effects so make sure the value is actually
                    // diffferent before setting it
                    bool newVisibility = OwningGrid.GridLinesVisibility == DataGridGridLinesVisibility.Horizontal || OwningGrid.GridLinesVisibility == DataGridGridLinesVisibility.All;

                    if (newVisibility != _bottomGridLine.IsVisible)
                    {
                        _bottomGridLine.IsVisible = newVisibility;
                    }
                    _bottomGridLine.Fill = OwningGrid.HorizontalGridLinesBrush;
                }

                foreach (DataGridCell cell in Cells)
                {
                    cell.EnsureGridLine(OwningGrid.ColumnsInternal.LastVisibleColumn);
                }
            }
        }

        // Set the proper style for the Header by walking up the Style hierarchy
        //TODO Styles
        internal void EnsureHeaderStyleAndVisibility(Styling.Style previousStyle)
        {
            if (_headerElement != null && OwningGrid != null)
            {
                _headerElement.IsVisible = OwningGrid.AreRowHeadersVisible;

                //if (OwningGrid.AreRowHeadersVisible)
                //{
                //    _headerElement.EnsureStyle(previousStyle);
                //    _headerElement.Visibility = Visibility.Visible;
                //}
                //else
                //{
                //    _headerElement.Visibility = Visibility.Collapsed;
                //}
            }
        }

        internal void EnsureHeaderVisibility()
        {
            if (_headerElement != null && OwningGrid != null)
            {
                _headerElement.IsVisible = OwningGrid.AreRowHeadersVisible;
            }
        }

        internal void InvalidateHorizontalArrange()
        {
            if (_cellsElement != null)
            {
                _cellsElement.InvalidateArrange();
            }
            if (_detailsElement != null)
            {
                _detailsElement.InvalidateArrange();
            }
        }

        internal void ResetGridLine()
        {
            _bottomGridLine = null;
        }

        #endregion

        #region Private
        
        private void DataGridCellCollection_CellAdded(object sender, DataGridCellEventArgs e)
        {
            _cellsElement?.Children.Add(e.Cell);
        }

        private void DataGridCellCollection_CellRemoved(object sender, DataGridCellEventArgs e)
        {
            _cellsElement?.Children.Remove(e.Cell);
        }
        
        private void DataGridRow_PointerPressed(PointerPressedEventArgs e)
        {
            if(e.MouseButton != MouseButton.Left)
            {
                return;
            }

            if (OwningGrid != null)
            {
                OwningGrid.IsDoubleClickRecordsClickOnCall(this);
                if (OwningGrid.UpdatedStateOnMouseLeftButtonDown)
                {
                    OwningGrid.UpdatedStateOnMouseLeftButtonDown = false;
                }
                else
                {
                    e.Handled = OwningGrid.UpdateStateOnMouseLeftButtonDown(e, -1, Slot, false);
                }
            }
        }


        #endregion

        #endregion

        #region Row Details

        private void OnRowDetailsChanged()
        {
            OwningGrid?.OnRowDetailsChanged();
        }

        // Returns the actual template that should be sued for Details: either explicity set on this row 
        // or inherited from the DataGrid
        private IDataTemplate ActualDetailsTemplate
        {
            get
            {
                Debug.Assert(OwningGrid != null);
                return DetailsTemplate ?? OwningGrid.RowDetailsTemplate;
            }
        }

        private bool ActualDetailsVisibility
        {
            get
            {
                if (OwningGrid == null)
                {
                    throw DataGridError.DataGrid.NoOwningGrid(GetType());
                }
                if (Index == -1)
                {
                    throw DataGridError.DataGridRow.InvalidRowIndexCannotCompleteOperation();
                }
                return OwningGrid.GetRowDetailsVisibility(Index);
            }
        }
        
        /*private Storyboard DetailsVisibleStoryboard
        {
            get
            {
                if (_detailsVisibleStoryboard == null && RootElement != null)
                {
                    _detailsVisibleStoryboard = RootElement.Resources[DATAGRIDROW_detailsVisibleTransition] as Storyboard;
                    if (_detailsVisibleStoryboard != null)
                    {
                        _detailsVisibleStoryboard.Completed += new EventHandler(DetailsVisibleStoryboard_Completed);
                        if (_detailsVisibleStoryboard.Children.Count > 0)
                        {
                            // If the user set a To value for the animation, we want to respect
                            _detailsHeightAnimation = _detailsVisibleStoryboard.Children[0] as DoubleAnimation;
                            if (_detailsHeightAnimation != null)
                            {
                                _detailsHeightAnimationToOverride = _detailsHeightAnimation.To;
                            }
                        }
                    }
                }
                return _detailsVisibleStoryboard;
            }
        } */

        private void UnloadDetailsTemplate(bool recycle)
        {
            if (_detailsElement != null)
            {
                if (_detailsContent != null)
                {
                    if (_detailsLoaded)
                    {
                        OwningGrid.OnUnloadingRowDetails(this, _detailsContent);
                    }
                    _detailsContent.DataContext = null;
                    if (!recycle)
                    {
                        _detailsContentSizeSubscription?.Dispose();
                        _detailsContentSizeSubscription = null;
                        _detailsContent = null;
                    }
                }

                if (!recycle)
                {
                    _detailsElement.Children.Clear();
                }
                _detailsElement.ContentHeight = 0;
            }
            if (!recycle)
            {
                _appliedDetailsTemplate = null;
                SetValueNoCallback(DetailsTemplateProperty, null);
            }

            _detailsLoaded = false;
            _appliedDetailsVisibility = null;
            SetValueNoCallback(AreDetailsVisibleProperty, false);
        }

        /*private void StopDetailsAnimation()
        {
            if (DetailsVisibleStoryboard != null)
            {
                DetailsVisibleStoryboard.Stop();
                _animatingDetails = false;
            }
        } */

        //TODO Animation
        internal void EnsureDetailsContentHeight()
        {
            if ((_detailsElement != null)
                && (_detailsContent != null)
                && (double.IsNaN(_detailsContent.Height))
                && (AreDetailsVisible)
                && (!double.IsNaN(_detailsDesiredHeight))
                && !DoubleUtil.AreClose(_detailsContent.Bounds.Height, _detailsDesiredHeight)
                && Slot != -1)
            {
                _detailsDesiredHeight = _detailsContent.Bounds.Height;
                //if (!_animatingDetails)
                if (true)
                {
                    _detailsElement.ContentHeight = _detailsDesiredHeight;
                }
            }
        } 

        // Makes sure the _detailsDesiredHeight is initialized.  We need to measure it to know what
        // height we want to animate to.  Subsequently, we just update that height in response to SizeChanged
        private void EnsureDetailsDesiredHeight()
        {
            Debug.Assert(_detailsElement != null && OwningGrid != null);

            if (_detailsContent != null)
            {
                Debug.Assert(_detailsElement.Children.Contains(_detailsContent));

                _detailsContent.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                _detailsDesiredHeight = _detailsContent.DesiredSize.Height;
            }
            else
            {
                _detailsDesiredHeight = 0;
            }
        }

        //TODO Cleanup
        double? _previousDetailsHeight = null;

        //TODO Animation 
        private void DetailsContent_SizeChanged(Rect newValue)
        {
            if (_previousDetailsHeight.HasValue)
            {
                var oldValue = _previousDetailsHeight.Value;
                _previousDetailsHeight = newValue.Height;
                if (newValue.Height != oldValue && newValue.Height != _detailsDesiredHeight)
                {

                    if (AreDetailsVisible && _appliedDetailsTemplate != null)
                    {
                        // Update the new desired height for RowDetails
                        _detailsDesiredHeight = newValue.Height;

                        //if (DetailsVisibleStoryboard != null)
                        //{
                        //    DetailsVisibleStoryboard.SkipToFill();
                        //    StopDetailsAnimation();
                        //}

                        _detailsElement.ContentHeight = newValue.Height;

                        // Calling this when details are not visible invalidates during layout when we have no work 
                        // to do.  In certain scenarios, this could cause a layout cycle
                        OnRowDetailsChanged();
                    }
                    //else if(_detailsContent != null)
                    //{
                    //    _detailsDesiredHeight = _detailsContent.DesiredSize.Height;
                    //}
                }
            }
            else
            {
                _previousDetailsHeight = newValue.Height;
            }
        }

        /*private void DetailsVisibleStoryboard_Completed(object sender, EventArgs e)
        {
            _animatingDetails = false;
            if (OwningGrid != null && (Slot != -1) && OwningGrid.IsSlotVisible(Slot))
            {
                if (AreDetailsVisible)
                {
                    Debug.Assert(!double.IsNaN(_detailsDesiredHeight));
                    Debug.Assert(_detailsContent != null);

                    // The height of the DetailsContents may have changed while we were animating its height
                    _detailsElement.ContentHeight = _detailsDesiredHeight;
                }
                OwningGrid.OnRowDetailsChanged();
            }
        } */

        //TODO Animation
        // Sets AreDetailsVisible on the row and animates if necessary
        internal void SetDetailsVisibilityInternal(bool isVisible, bool raiseNotification, bool animate)
        {
            Debug.Assert(OwningGrid != null);
            Debug.Assert(Index != -1);

            if (_appliedDetailsVisibility != isVisible)
            {
                if (_detailsElement == null)
                {
                    if (raiseNotification)
                    {
                        _detailsVisibilityNotificationPending = true;
                    }
                    return;
                }

                _appliedDetailsVisibility = isVisible;
                SetValueNoCallback(AreDetailsVisibleProperty, isVisible);

                //StopDetailsAnimation();

                // Applies a new DetailsTemplate only if it has changed either here or at the DataGrid level
                ApplyDetailsTemplate(initializeDetailsPreferredHeight: true);

                // no template to show
                if (_appliedDetailsTemplate == null)
                {
                    if (_detailsElement.ContentHeight > 0)
                    {
                        _detailsElement.ContentHeight = 0;
                    }
                    return;
                }

                if(false)
                { }
                //if (animate && DetailsVisibleStoryboard != null && _detailsHeightAnimation != null)
                //{
                //    if (AreDetailsVisible)
                //    {
                //        // Expand
                //        _detailsHeightAnimation.From = 0.0;
                //        _detailsHeightAnimation.To = _detailsHeightAnimationToOverride.HasValue ?
                //            _detailsHeightAnimationToOverride.Value :
                //            _detailsDesiredHeight;
                //        _checkDetailsContentHeight = true;
                //    }
                //    else
                //    {
                //        // Collapse
                //        _detailsHeightAnimation.From = _detailsElement.ActualHeight;
                //        _detailsHeightAnimation.To = 0.0;
                //    }
                //    _animatingDetails = true;
                //    DetailsVisibleStoryboard.Begin();
                //}
                else
                {
                    if (AreDetailsVisible)
                    {
                        // Set the details height directly
                        _detailsElement.ContentHeight = _detailsDesiredHeight;
                        _checkDetailsContentHeight = true;
                    }
                    else
                    {
                        _detailsElement.ContentHeight = 0;
                    }
                }

                OnRowDetailsChanged();

                if (raiseNotification)
                {
                    OwningGrid.OnRowDetailsVisibilityChanged(new DataGridRowDetailsEventArgs(this, _detailsContent));
                }
            }
        }

        internal void ApplyDetailsTemplate(bool initializeDetailsPreferredHeight)
        {
            if (_detailsElement != null && AreDetailsVisible)
            {
                IDataTemplate oldDetailsTemplate = _appliedDetailsTemplate;
                if (ActualDetailsTemplate != null && ActualDetailsTemplate != _appliedDetailsTemplate)
                {
                    if (_detailsContent != null)
                    {
                        _detailsContentSizeSubscription?.Dispose();
                        _detailsContentSizeSubscription = null;
                        if (_detailsLoaded)
                        {
                            OwningGrid.OnUnloadingRowDetails(this, _detailsContent);
                            _detailsLoaded = false;
                        }
                    }
                    _detailsElement.Children.Clear();

                    _detailsContent = ActualDetailsTemplate.Build(DataContext);
                    _appliedDetailsTemplate = ActualDetailsTemplate;

                    if (_detailsContent != null)
                    {
                        _detailsContentSizeSubscription =
                            _detailsContent.GetObservable(BoundsProperty)
                                           .Subscribe(DetailsContent_SizeChanged);
                        _detailsElement.Children.Add(_detailsContent);
                    }
                }

                if (_detailsContent != null && !_detailsLoaded)
                {
                    _detailsLoaded = true;
                    _detailsContent.DataContext = DataContext;
                    OwningGrid.OnLoadingRowDetails(this, _detailsContent);
                }
                if (initializeDetailsPreferredHeight && double.IsNaN(_detailsDesiredHeight) &&
                    _appliedDetailsTemplate != null && _detailsElement.Children.Count > 0)
                {
                    EnsureDetailsDesiredHeight();
                }
                else if (oldDetailsTemplate == null)
                {
                    _detailsDesiredHeight = double.NaN;
                    EnsureDetailsDesiredHeight();
                    _detailsElement.ContentHeight = _detailsDesiredHeight;
                }
            }
        } 



        #endregion

    }

    /*
    [TemplatePart(Name = DATAGRIDROW_elementBottomGridLine, Type = typeof(Rectangle))]
    [TemplatePart(Name = DATAGRIDROW_elementCells, Type = typeof(DataGridCellsPresenter))]
    [TemplatePart(Name = DATAGRIDROW_elementDetails, Type = typeof(DataGridDetailsPresenter))]
    [TemplatePart(Name = DATAGRIDROW_elementRoot, Type = typeof(Panel))]
    [TemplatePart(Name = DATAGRIDROW_elementRowHeader, Type = typeof(DataGridRowHeader))]

    [TemplateVisualState(Name = DATAGRIDROW_stateNormal, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROW_stateAlternate, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROW_stateNormalEditing, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROW_stateNormalEditingFocused, GroupName = VisualStates.GroupCommon)]

    [TemplateVisualState(Name = DATAGRIDROW_stateSelected, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROW_stateSelectedFocused, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROW_stateMouseOver, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROW_stateMouseOverEditing, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROW_stateMouseOverEditingFocused, GroupName = VisualStates.GroupCommon)]

    [TemplateVisualState(Name = DATAGRIDROW_stateMouseOverSelected, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROW_stateMouseOverSelectedFocused, GroupName = VisualStates.GroupCommon)]

    [TemplateVisualState(Name = VisualStates.StateInvalid, GroupName = VisualStates.GroupValidation)]
    [TemplateVisualState(Name = VisualStates.StateValid, GroupName = VisualStates.GroupValidation)]
    [StyleTypedProperty(Property = "HeaderStyle", StyleTargetType = typeof(DataGridRowHeader))]
    public partial class DataGridRow : Control
    */
    #region Constants

    /*

    private const string DATAGRIDROW_detailsVisibleTransition = "DetailsVisibleTransition";


    private const string DATAGRIDROW_stateAlternate = "NormalAlternatingRow";
    private const string DATAGRIDROW_stateAlternateLegacy = "Normal AlternatingRow";
    private const string DATAGRIDROW_stateMouseOver = "MouseOver";
    private const string DATAGRIDROW_stateMouseOverEditing = "MouseOverUnfocusedEditing";
    private const string DATAGRIDROW_stateMouseOverEditingLegacy = "MouseOver Unfocused Editing";
    private const string DATAGRIDROW_stateMouseOverEditingFocused = "MouseOverEditing";
    private const string DATAGRIDROW_stateMouseOverEditingFocusedLegacy = "MouseOver Editing";
    private const string DATAGRIDROW_stateMouseOverSelected = "MouseOverUnfocusedSelected";
    private const string DATAGRIDROW_stateMouseOverSelectedLegacy = "MouseOver Unfocused Selected";
    private const string DATAGRIDROW_stateMouseOverSelectedFocused = "MouseOverSelected";
    private const string DATAGRIDROW_stateMouseOverSelectedFocusedLegacy = "MouseOver Selected";
    private const string DATAGRIDROW_stateNormal = "Normal";
    private const string DATAGRIDROW_stateNormalEditing = "UnfocusedEditing";
    private const string DATAGRIDROW_stateNormalEditingLegacy = "Unfocused Editing";
    private const string DATAGRIDROW_stateNormalEditingFocused = "NormalEditing";
    private const string DATAGRIDROW_stateNormalEditingFocusedLegacy = "Normal Editing";
    private const string DATAGRIDROW_stateSelected = "UnfocusedSelected";
    private const string DATAGRIDROW_stateSelectedLegacy = "Unfocused Selected";
    private const string DATAGRIDROW_stateSelectedFocused = "NormalSelected";
    private const string DATAGRIDROW_stateSelectedFocusedLegacy = "Normal Selected";

    private const byte DATAGRIDROW_stateMouseOverCode = 0;
    private const byte DATAGRIDROW_stateMouseOverEditingCode = 1;
    private const byte DATAGRIDROW_stateMouseOverEditingFocusedCode = 2;
    private const byte DATAGRIDROW_stateMouseOverSelectedCode = 3;
    private const byte DATAGRIDROW_stateMouseOverSelectedFocusedCode = 4;
    private const byte DATAGRIDROW_stateNormalCode = 5;
    private const byte DATAGRIDROW_stateNormalEditingCode = 6;
    private const byte DATAGRIDROW_stateNormalEditingFocusedCode = 7;
    private const byte DATAGRIDROW_stateSelectedCode = 8;
    private const byte DATAGRIDROW_stateSelectedFocusedCode = 9;
    private const byte DATAGRIDROW_stateNullCode = 255;
    */

    #endregion Constants

    #region Data


    // Static arrays to handle state transitions:
    /*private static byte[] _idealStateMapping = new byte[] {
        DATAGRIDROW_stateNormalCode,
        DATAGRIDROW_stateNormalCode,
        DATAGRIDROW_stateMouseOverCode,
        DATAGRIDROW_stateMouseOverCode,
        DATAGRIDROW_stateNullCode,
        DATAGRIDROW_stateNullCode,
        DATAGRIDROW_stateNullCode,
        DATAGRIDROW_stateNullCode,
        DATAGRIDROW_stateSelectedCode,
        DATAGRIDROW_stateSelectedFocusedCode,
        DATAGRIDROW_stateMouseOverSelectedCode,
        DATAGRIDROW_stateMouseOverSelectedFocusedCode,
        DATAGRIDROW_stateNormalEditingCode,
        DATAGRIDROW_stateNormalEditingFocusedCode,
        DATAGRIDROW_stateMouseOverEditingCode,
        DATAGRIDROW_stateMouseOverEditingFocusedCode
    }; */

    /*private static byte[] _fallbackStateMapping = new byte[] {
        DATAGRIDROW_stateNormalCode, //DATAGRIDROW_stateMouseOverCode's fallback
        DATAGRIDROW_stateMouseOverEditingFocusedCode, //DATAGRIDROW_stateMouseOverEditingCode's fallback
        DATAGRIDROW_stateNormalEditingFocusedCode, //DATAGRIDROW_stateMouseOverEditingFocusedCode's fallback
        DATAGRIDROW_stateMouseOverSelectedFocusedCode, //DATAGRIDROW_stateMouseOverSelectedCode's fallback
        DATAGRIDROW_stateSelectedFocusedCode, //DATAGRIDROW_stateMouseOverSelectedFocusedCode's fallback
        DATAGRIDROW_stateNullCode, //DATAGRIDROW_stateNormalCode's fallback
        DATAGRIDROW_stateNormalEditingFocusedCode, //DATAGRIDROW_stateNormalEditingCode's fallback
        DATAGRIDROW_stateSelectedFocusedCode, //DATAGRIDROW_stateNormalEditingFocusedCode's fallback
        DATAGRIDROW_stateSelectedFocusedCode, //DATAGRIDROW_stateSelectedCode's fallback
        DATAGRIDROW_stateNormalCode //DATAGRIDROW_stateSelectedFocusedCode's fallback
    }; */

    // In SL 2, our state names had spaces.  Going forward, we are removing the spaces but still 
    // supporting the legacy state names
    /*private static string[] _legacyStateNames = new string[] {
        DATAGRIDROW_stateMouseOver,
        DATAGRIDROW_stateMouseOverEditingLegacy,
        DATAGRIDROW_stateMouseOverEditingFocusedLegacy,
        DATAGRIDROW_stateMouseOverSelectedLegacy,
        DATAGRIDROW_stateMouseOverSelectedFocusedLegacy,
        DATAGRIDROW_stateNormal,
        DATAGRIDROW_stateNormalEditingLegacy,
        DATAGRIDROW_stateNormalEditingFocusedLegacy,
        DATAGRIDROW_stateSelectedLegacy,
        DATAGRIDROW_stateSelectedFocusedLegacy
    }; */

    /*private static string[] _stateNames = new string[] {
        DATAGRIDROW_stateMouseOver,
        DATAGRIDROW_stateMouseOverEditing,
        DATAGRIDROW_stateMouseOverEditingFocused,
        DATAGRIDROW_stateMouseOverSelected,
        DATAGRIDROW_stateMouseOverSelectedFocused,
        DATAGRIDROW_stateNormal,
        DATAGRIDROW_stateNormalEditing,
        DATAGRIDROW_stateNormalEditingFocused,
        DATAGRIDROW_stateSelected,
        DATAGRIDROW_stateSelectedFocused
    }; */

    #endregion Data

    #region HeaderStyle

    //TODO Styles

    /// <summary>
    /// Gets or sets the style that is used when rendering the row header.
    /// </summary>
    /*public Style HeaderStyle
    {
        get { return GetValue(HeaderStyleProperty) as Style; }
        set { SetValue(HeaderStyleProperty, value); }
    } */

    /// <summary>
    /// Identifies the <see cref="P:Avalonia.Controls.DataGridRow.HeaderStyle" /> dependency property.
    /// </summary>
    /*public static readonly DependencyProperty HeaderStyleProperty =
        DependencyProperty.Register(
            "HeaderStyle",
            typeof(Style),
            typeof(DataGridRow),
            new PropertyMetadata(OnHeaderStylePropertyChanged));

    /*private static void OnHeaderStylePropertyChanged(AvaloniaObject d, DependencyPropertyChangedEventArgs e)
    {
        DataGridRow row = d as DataGridRow;
        if (row != null && row._headerElement != null)
        {
            row._headerElement.EnsureStyle(e.OldValue as Style);
        }
    } */
    #endregion HeaderStyle



    /// <summary>
    /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
    /// </summary>
    /*protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new DataGridRowAutomationPeer(this);
    } */



}
