// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Input;
using Avalonia.Media;
using System.Diagnostics;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Represents an individual <see cref="T:Avalonia.Controls.DataGrid" /> row header. 
    /// </summary>
    public class DataGridRowHeader : ContentControl
    {
        private const string DATAGRIDROWHEADER_elementRootName = "Root";
        private const double DATAGRIDROWHEADER_separatorThickness = 1;

        #region Data

        private Control _rootElement;

        #endregion Data

        #region Dependency Properties


        public static readonly StyledProperty<IBrush> SeparatorBrushProperty =
            AvaloniaProperty.Register<DataGridRowHeader, IBrush>(nameof(SeparatorBrush));

        public IBrush SeparatorBrush
        {
            get { return GetValue(SeparatorBrushProperty); }
            set { SetValue(SeparatorBrushProperty, value); }
        }


        public static readonly StyledProperty<bool> AreSeparatorsVisibleProperty =
            AvaloniaProperty.Register<DataGridRowHeader, bool>(
                nameof(AreSeparatorsVisible));

        /// <summary>
        /// Gets or sets a value indicating whether the row header separator lines are visible.
        /// </summary>
        public bool AreSeparatorsVisible
        {
            get { return GetValue(AreSeparatorsVisibleProperty); }
            set { SetValue(AreSeparatorsVisibleProperty, value); }
        }

        #endregion


        /// <summary>
        /// Initializes a new instance of the <see cref="T:Avalonia.Controls.Primitives.DataGridRowHeader" /> class. 
        /// </summary>
        public DataGridRowHeader()
        {
            AddHandler(PointerPressedEvent, DataGridRowHeader_PointerPressed, handledEventsToo: true);
        }

        #region Properties


        internal Control Owner
        {
            get;
            set;
        }

        private DataGridRow OwningRow => Owner as DataGridRow;

        private DataGridRowGroupHeader OwningRowGroupHeader => Owner as DataGridRowGroupHeader;

        private DataGrid OwningGrid
        {
            get
            {
                if (OwningRow != null)
                {
                    return OwningRow.OwningGrid;
                }
                else if (OwningRowGroupHeader != null)
                {
                    return OwningRowGroupHeader.OwningGrid;
                }
                return null;
            }
        }

        private int Slot
        {
            get
            {
                if (OwningRow != null)
                {
                    return OwningRow.Slot;
                }
                else if (OwningRowGroupHeader != null)
                {
                    return OwningRowGroupHeader.RowGroupInfo.Slot;
                }
                return -1;
            }
        }

        #endregion
        
        #region Protected Methods

        /// <summary>
        /// Builds the visual tree for the row header when a new template is applied. 
        /// </summary>
        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);

            _rootElement = e.NameScope.Find<Control>(DATAGRIDROWHEADER_elementRootName);
            if (_rootElement != null)
            {
                ApplyOwnerStatus();
            }
        } 

        /// <summary>
        /// Measures the children of a <see cref="T:Avalonia.Controls.Primitives.DataGridRowHeader" /> to prepare for arranging them during the <see cref="M:System.Windows.FrameworkElement.ArrangeOverride(System.Windows.Size)" /> pass.
        /// </summary>
        /// <param name="availableSize">
        /// The available size that this element can give to child elements. Indicates an upper limit that child elements should not exceed.
        /// </param>
        /// <returns>
        /// The size that the <see cref="T:Avalonia.Controls.Primitives.DataGridRowHeader" /> determines it needs during layout, based on its calculations of child object allocated sizes.
        /// </returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (OwningRow == null || OwningGrid == null)
            {
                return base.MeasureOverride(availableSize);
            }
            double measureHeight = double.IsNaN(OwningGrid.RowHeight) ? availableSize.Height : OwningGrid.RowHeight;
            double measureWidth = double.IsNaN(OwningGrid.RowHeaderWidth) ? availableSize.Width : OwningGrid.RowHeaderWidth;
            Size measuredSize = base.MeasureOverride(new Size(measureWidth, measureHeight));

            // Auto grow the row header or force it to a fixed width based on the DataGrid's setting
            if (!double.IsNaN(OwningGrid.RowHeaderWidth) || measuredSize.Width < OwningGrid.ActualRowHeaderWidth)
            {
                return new Size(OwningGrid.ActualRowHeaderWidth, measuredSize.Height);
            }

            return measuredSize;
        }

        #endregion Protected Methods


        #region Internal Methods

        //TODO Implement
        internal void ApplyOwnerStatus()
        {
            if (_rootElement != null && Owner != null && Owner.IsVisible)
            {
                //byte idealStateMappingIndex = 0;

                //if (OwningRow != null)
                //{
                //    if (OwningRow.IsValid)
                //    {
                //        VisualStates.GoToState(this, true, VisualStates.StateRowValid);
                //    }
                //    else
                //    {
                //        VisualStates.GoToState(this, true, VisualStates.StateRowInvalid, VisualStates.StateRowValid);
                //    }

                //    if (OwningGrid != null)
                //    {
                //        if (OwningGrid.CurrentSlot == OwningRow.Slot)
                //        {
                //            idealStateMappingIndex += 16;
                //        }
                //        if (OwningGrid.ContainsFocus)
                //        {
                //            idealStateMappingIndex += 1;
                //        }
                //    }
                //    if (OwningRow.IsSelected || OwningRow.IsEditing)
                //    {
                //        idealStateMappingIndex += 8;
                //    }
                //    if (OwningRow.IsEditing)
                //    {
                //        idealStateMappingIndex += 4;
                //    }
                //    if (OwningRow.IsMouseOver)
                //    {
                //        idealStateMappingIndex += 2;
                //    }
                //}
                //else if (OwningRowGroupHeader != null && OwningGrid != null && OwningGrid.CurrentSlot == OwningRowGroupHeader.RowGroupInfo.Slot)
                //{
                //    idealStateMappingIndex += 16;
                //}

                //byte stateCode = _idealStateMapping[idealStateMappingIndex];
                //Debug.Assert(stateCode != DATAGRIDROWHEADER_stateNullCode);

                //string storyboardName;
                //while (stateCode != DATAGRIDROWHEADER_stateNullCode)
                //{
                //    storyboardName = _stateNames[stateCode];
                //    if (VisualStateManager.GoToState(this, storyboardName, animate) || VisualStateManager.GoToState(this, _legacyStateNames[stateCode], animate))
                //    {
                //        break;
                //    }
                //    else
                //    {
                //        // The state wasn't implemented so fall back to the next one
                //        stateCode = _fallbackStateMapping[stateCode];
                //    }
                //}
            }
        }

        #endregion Internal Methods


        #region Private Methods

        protected override void OnPointerEnter(PointerEventArgs e)
        {
            if (OwningRow != null)
            {
                OwningRow.IsMouseOver = true;
            }

            base.OnPointerEnter(e);
        }
        protected override void OnPointerLeave(PointerEventArgs e)
        {
            if (OwningRow != null)
            {
                OwningRow.IsMouseOver = false;
            }

            base.OnPointerLeave(e);
        }

        //TODO TabStop
        private void DataGridRowHeader_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if(e.MouseButton != MouseButton.Left)
            {
                return;
            }

            if (OwningGrid != null)
            {
                if (!e.Handled)
                //if (!e.Handled && OwningGrid.IsTabStop)
                {
                    OwningGrid.Focus();
                }
                if (OwningRow != null)
                {
                    Debug.Assert(sender is DataGridRowHeader);
                    Debug.Assert(sender == this);
                    e.Handled = OwningGrid.UpdateStateOnMouseLeftButtonDown(e, -1, Slot, false);
                    OwningGrid.UpdatedStateOnMouseLeftButtonDown = true;
                }
            }
        } 

        #endregion Private Methods

    }

    /*
    [TemplatePart(Name = DATAGRIDROWHEADER_elementRootName, Type = typeof(FrameworkElement))]

    [TemplateVisualState(Name = DATAGRIDROWHEADER_stateNormal, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_stateNormalCurrentRow, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_stateNormalEditingRow, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_stateNormalEditingRowFocused, GroupName = VisualStates.GroupCommon)]

    [TemplateVisualState(Name = DATAGRIDROWHEADER_stateMouseOver, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_stateMouseOverCurrentRow, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_stateMouseOverEditingRow, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_stateMouseOverEditingRowFocused, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_stateMouseOverSelected, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_stateMouseOverSelectedFocused, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_stateMouseOverSelectedCurrentRow, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_stateMouseOverSelectedCurrentRowFocused, GroupName = VisualStates.GroupCommon)]

    [TemplateVisualState(Name = DATAGRIDROWHEADER_stateSelected, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_stateSelectedCurrentRow, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_stateSelectedCurrentRowFocused, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_stateSelectedFocused, GroupName = VisualStates.GroupCommon)]

    [TemplateVisualState(Name = VisualStates.StateRowInvalid, GroupName = VisualStates.GroupValidation)]
    [TemplateVisualState(Name = VisualStates.StateRowValid, GroupName = VisualStates.GroupValidation)]
    public partial class DataGridRowHeader : ContentControl
    */

    #region Styles

    //TODO Styles

    /// <summary>
    /// Ensures that the correct Style is applied to this object.
    /// </summary>
    /// <param name="previousStyle">Caller's previous associated Style</param>
    /*internal void EnsureStyle(Style previousStyle)
    {
        if (Style != null
            && (OwningRow != null && Style != OwningRow.HeaderStyle)
            && (OwningRowGroupHeader != null && Style != OwningRowGroupHeader.HeaderStyle)
            && (OwningGrid != null && Style != OwningGrid.RowHeaderStyle)
            && (Style != previousStyle))
        {
            return;
        }

        Style style = null;
        if (OwningRow != null)
        {
            style = OwningRow.HeaderStyle;
        }
        if (style == null && OwningGrid != null)
        {
            style = OwningGrid.RowHeaderStyle;
        }
        SetStyleWithType(style);
    } */

    #endregion
    
    #region Constants
    /*
    private const string DATAGRIDROWHEADER_stateMouseOver = "MouseOver";
    private const string DATAGRIDROWHEADER_stateMouseOverCurrentRow = "MouseOverCurrentRow";
    private const string DATAGRIDROWHEADER_stateMouseOverCurrentRowLegacy = "MouseOver CurrentRow";
    private const string DATAGRIDROWHEADER_stateMouseOverEditingRow = "MouseOverUnfocusedEditingRow";
    private const string DATAGRIDROWHEADER_stateMouseOverEditingRowLegacy = "MouseOver Unfocused EditingRow";
    private const string DATAGRIDROWHEADER_stateMouseOverEditingRowFocused = "MouseOverEditingRow";
    private const string DATAGRIDROWHEADER_stateMouseOverEditingRowFocusedLegacy = "MouseOver EditingRow";
    private const string DATAGRIDROWHEADER_stateMouseOverSelected = "MouseOverUnfocusedSelected";
    private const string DATAGRIDROWHEADER_stateMouseOverSelectedLegacy = "MouseOver Unfocused Selected";
    private const string DATAGRIDROWHEADER_stateMouseOverSelectedCurrentRow = "MouseOverUnfocusedCurrentRowSelected";
    private const string DATAGRIDROWHEADER_stateMouseOverSelectedCurrentRowLegacy = "MouseOver Unfocused CurrentRow Selected";
    private const string DATAGRIDROWHEADER_stateMouseOverSelectedCurrentRowFocused = "MouseOverCurrentRowSelected";
    private const string DATAGRIDROWHEADER_stateMouseOverSelectedCurrentRowFocusedLegacy = "MouseOver CurrentRow Selected";
    private const string DATAGRIDROWHEADER_stateMouseOverSelectedFocused = "MouseOverSelected";
    private const string DATAGRIDROWHEADER_stateMouseOverSelectedFocusedLegacy = "MouseOver Selected";
    private const string DATAGRIDROWHEADER_stateNormal = "Normal";
    private const string DATAGRIDROWHEADER_stateNormalCurrentRow = "NormalCurrentRow";
    private const string DATAGRIDROWHEADER_stateNormalCurrentRowLegacy = "Normal CurrentRow";
    private const string DATAGRIDROWHEADER_stateNormalEditingRow = "UnfocusedEditingRow";
    private const string DATAGRIDROWHEADER_stateNormalEditingRowLegacy = "Unfocused EditingRow";
    private const string DATAGRIDROWHEADER_stateNormalEditingRowFocusedLegacy = "NormalEditingRow";
    private const string DATAGRIDROWHEADER_stateNormalEditingRowFocused = "Normal EditingRow";
    private const string DATAGRIDROWHEADER_stateSelected = "UnfocusedSelected";
    private const string DATAGRIDROWHEADER_stateSelectedLegacy = "Unfocused Selected";
    private const string DATAGRIDROWHEADER_stateSelectedCurrentRow = "UnfocusedCurrentRowSelected";
    private const string DATAGRIDROWHEADER_stateSelectedCurrentRowLegacy = "Unfocused CurrentRow Selected";
    private const string DATAGRIDROWHEADER_stateSelectedCurrentRowFocused = "NormalCurrentRowSelected";
    private const string DATAGRIDROWHEADER_stateSelectedCurrentRowFocusedLegacy = "Normal CurrentRow Selected";
    private const string DATAGRIDROWHEADER_stateSelectedFocused = "NormalSelected";
    private const string DATAGRIDROWHEADER_stateSelectedFocusedLegacy = "Normal Selected";

    private const byte DATAGRIDROWHEADER_stateMouseOverCode = 0;
    private const byte DATAGRIDROWHEADER_stateMouseOverCurrentRowCode = 1;
    private const byte DATAGRIDROWHEADER_stateMouseOverEditingRowCode = 2;
    private const byte DATAGRIDROWHEADER_stateMouseOverEditingRowFocusedCode = 3;
    private const byte DATAGRIDROWHEADER_stateMouseOverSelectedCode = 4;
    private const byte DATAGRIDROWHEADER_stateMouseOverSelectedCurrentRowCode = 5;
    private const byte DATAGRIDROWHEADER_stateMouseOverSelectedCurrentRowFocusedCode = 6;
    private const byte DATAGRIDROWHEADER_stateMouseOverSelectedFocusedCode = 7;
    private const byte DATAGRIDROWHEADER_stateNormalCode = 8;
    private const byte DATAGRIDROWHEADER_stateNormalCurrentRowCode = 9;
    private const byte DATAGRIDROWHEADER_stateNormalEditingRowCode = 10;
    private const byte DATAGRIDROWHEADER_stateNormalEditingRowFocusedCode = 11;
    private const byte DATAGRIDROWHEADER_stateSelectedCode = 12;
    private const byte DATAGRIDROWHEADER_stateSelectedCurrentRowCode = 13;
    private const byte DATAGRIDROWHEADER_stateSelectedCurrentRowFocusedCode = 14;
    private const byte DATAGRIDROWHEADER_stateSelectedFocusedCode = 15;
    private const byte DATAGRIDROWHEADER_stateNullCode = 255;
    */

    /*private static byte[] _fallbackStateMapping = new byte[] {
        DATAGRIDROWHEADER_stateNormalCode,
        DATAGRIDROWHEADER_stateNormalCurrentRowCode,
        DATAGRIDROWHEADER_stateMouseOverEditingRowFocusedCode,
        DATAGRIDROWHEADER_stateNormalEditingRowFocusedCode,
        DATAGRIDROWHEADER_stateMouseOverSelectedFocusedCode,
        DATAGRIDROWHEADER_stateMouseOverSelectedCurrentRowFocusedCode,
        DATAGRIDROWHEADER_stateSelectedFocusedCode,
        DATAGRIDROWHEADER_stateSelectedFocusedCode,
        DATAGRIDROWHEADER_stateNullCode,
        DATAGRIDROWHEADER_stateNormalCode,
        DATAGRIDROWHEADER_stateNormalEditingRowFocusedCode,
        DATAGRIDROWHEADER_stateSelectedCurrentRowFocusedCode,
        DATAGRIDROWHEADER_stateSelectedFocusedCode,
        DATAGRIDROWHEADER_stateSelectedCurrentRowFocusedCode,
        DATAGRIDROWHEADER_stateNormalCurrentRowCode,
        DATAGRIDROWHEADER_stateNormalCode,
    }; */

    /*private static byte[] _idealStateMapping = new byte[] {
        DATAGRIDROWHEADER_stateNormalCode,
        DATAGRIDROWHEADER_stateNormalCode,
        DATAGRIDROWHEADER_stateMouseOverCode,
        DATAGRIDROWHEADER_stateMouseOverCode,
        DATAGRIDROWHEADER_stateNullCode,
        DATAGRIDROWHEADER_stateNullCode,
        DATAGRIDROWHEADER_stateNullCode,
        DATAGRIDROWHEADER_stateNullCode,
        DATAGRIDROWHEADER_stateSelectedCode,
        DATAGRIDROWHEADER_stateSelectedFocusedCode,
        DATAGRIDROWHEADER_stateMouseOverSelectedCode,
        DATAGRIDROWHEADER_stateMouseOverSelectedFocusedCode,
        DATAGRIDROWHEADER_stateNormalEditingRowCode,
        DATAGRIDROWHEADER_stateNormalEditingRowFocusedCode,
        DATAGRIDROWHEADER_stateMouseOverEditingRowCode,
        DATAGRIDROWHEADER_stateMouseOverEditingRowFocusedCode,
        DATAGRIDROWHEADER_stateNormalCurrentRowCode,
        DATAGRIDROWHEADER_stateNormalCurrentRowCode,
        DATAGRIDROWHEADER_stateMouseOverCurrentRowCode,
        DATAGRIDROWHEADER_stateMouseOverCurrentRowCode,
        DATAGRIDROWHEADER_stateNullCode,
        DATAGRIDROWHEADER_stateNullCode,
        DATAGRIDROWHEADER_stateNullCode,
        DATAGRIDROWHEADER_stateNullCode,
        DATAGRIDROWHEADER_stateSelectedCurrentRowCode,
        DATAGRIDROWHEADER_stateSelectedCurrentRowFocusedCode,
        DATAGRIDROWHEADER_stateMouseOverSelectedCurrentRowCode,
        DATAGRIDROWHEADER_stateMouseOverSelectedCurrentRowFocusedCode,
        DATAGRIDROWHEADER_stateNormalEditingRowCode,
        DATAGRIDROWHEADER_stateNormalEditingRowFocusedCode,
        DATAGRIDROWHEADER_stateMouseOverEditingRowCode,
        DATAGRIDROWHEADER_stateMouseOverEditingRowFocusedCode
    };*/

    // In SL 2, our state names had spaces.  Going forward, we are removing the spaces but still 
    // supporting the legacy state names
    /*private static string[] _legacyStateNames = new string[]
    {
        DATAGRIDROWHEADER_stateMouseOver,
        DATAGRIDROWHEADER_stateMouseOverCurrentRowLegacy,
        DATAGRIDROWHEADER_stateMouseOverEditingRowLegacy,
        DATAGRIDROWHEADER_stateMouseOverEditingRowFocusedLegacy,
        DATAGRIDROWHEADER_stateMouseOverSelectedLegacy,
        DATAGRIDROWHEADER_stateMouseOverSelectedCurrentRowLegacy,
        DATAGRIDROWHEADER_stateMouseOverSelectedCurrentRowFocusedLegacy,
        DATAGRIDROWHEADER_stateMouseOverSelectedFocusedLegacy,
        DATAGRIDROWHEADER_stateNormal,
        DATAGRIDROWHEADER_stateNormalCurrentRowLegacy,
        DATAGRIDROWHEADER_stateNormalEditingRowLegacy,
        DATAGRIDROWHEADER_stateNormalEditingRowFocusedLegacy,
        DATAGRIDROWHEADER_stateSelectedLegacy,
        DATAGRIDROWHEADER_stateSelectedCurrentRowLegacy,
        DATAGRIDROWHEADER_stateSelectedCurrentRowFocusedLegacy,
        DATAGRIDROWHEADER_stateSelectedFocusedLegacy
    }; */

    /*private static string[] _stateNames = new string[]
    {
        DATAGRIDROWHEADER_stateMouseOver,
        DATAGRIDROWHEADER_stateMouseOverCurrentRow,
        DATAGRIDROWHEADER_stateMouseOverEditingRow,
        DATAGRIDROWHEADER_stateMouseOverEditingRowFocused,
        DATAGRIDROWHEADER_stateMouseOverSelected,
        DATAGRIDROWHEADER_stateMouseOverSelectedCurrentRow,
        DATAGRIDROWHEADER_stateMouseOverSelectedCurrentRowFocused,
        DATAGRIDROWHEADER_stateMouseOverSelectedFocused,
        DATAGRIDROWHEADER_stateNormal,
        DATAGRIDROWHEADER_stateNormalCurrentRow,
        DATAGRIDROWHEADER_stateNormalEditingRow,
        DATAGRIDROWHEADER_stateNormalEditingRowFocused,
        DATAGRIDROWHEADER_stateSelected,
        DATAGRIDROWHEADER_stateSelectedCurrentRow,
        DATAGRIDROWHEADER_stateSelectedCurrentRowFocused,
        DATAGRIDROWHEADER_stateSelectedFocused
    };*/

    #endregion Constants



    /// <summary>
    /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
    /// </summary>
    /*protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new DataGridRowHeaderAutomationPeer(this);
    } */



}

