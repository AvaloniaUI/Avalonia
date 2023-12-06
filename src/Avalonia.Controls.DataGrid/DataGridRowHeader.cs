// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Media;
using System.Diagnostics;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Represents an individual <see cref="T:Avalonia.Controls.DataGrid" /> row header. 
    /// </summary>
    [TemplatePart(DATAGRIDROWHEADER_elementRootName, typeof(Control))]
    [PseudoClasses(":invalid", ":selected", ":editing", ":current")]
    public class DataGridRowHeader : ContentControl
    {
        private const string DATAGRIDROWHEADER_elementRootName = "PART_Root";
        private Control _rootElement;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Avalonia.Controls.Primitives.DataGridRowHeader" /> class. 
        /// </summary>
        public DataGridRowHeader()
        {
            AddHandler(PointerPressedEvent, DataGridRowHeader_PointerPressed, handledEventsToo: true);
        }

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

        /// <summary>
        /// Builds the visual tree for the row header when a new template is applied. 
        /// </summary>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            _rootElement = e.NameScope.Find<Control>(DATAGRIDROWHEADER_elementRootName);
            if (_rootElement != null)
            {
                UpdatePseudoClasses();
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

        internal void UpdatePseudoClasses()
        {
            if (_rootElement != null && Owner != null && Owner.IsVisible)
            {
                if (OwningRow != null)
                {
                    PseudoClasses.Set(":invalid", !OwningRow.IsValid);

                    PseudoClasses.Set(":selected", OwningRow.IsSelected);

                    PseudoClasses.Set(":editing", OwningRow.IsEditing);

                    if (OwningGrid != null)
                    {
                        PseudoClasses.Set(":current", OwningRow.Slot == OwningGrid.CurrentSlot);
                    }
                }
                else if (OwningRowGroupHeader != null && OwningGrid != null)
                {
                    PseudoClasses.Set(":current", OwningRowGroupHeader.RowGroupInfo.Slot == OwningGrid.CurrentSlot);
                }
            }
        }

        protected override void OnPointerEntered(PointerEventArgs e)
        {
            if (OwningRow != null)
            {
                OwningRow.IsMouseOver = true;
            }

            base.OnPointerEntered(e);
        }
        protected override void OnPointerExited(PointerEventArgs e)
        {
            if (OwningRow != null)
            {
                OwningRow.IsMouseOver = false;
            }

            base.OnPointerExited(e);
        }

        //TODO TabStop
        private void DataGridRowHeader_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (OwningGrid == null)
            {
                return;
            }

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
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
                }
            }
            else if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                if (!e.Handled)
                {
                    OwningGrid.Focus();
                }
                if (OwningRow != null)
                {
                    Debug.Assert(sender is DataGridRowHeader);
                    Debug.Assert(sender == this);
                    e.Handled = OwningGrid.UpdateStateOnMouseRightButtonDown(e, -1, Slot, false);
                }
            }
        } 

    }

}

