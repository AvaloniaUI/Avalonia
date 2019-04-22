// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using System;
using System.Diagnostics;
using System.Reactive.Linq;

namespace Avalonia.Controls
{
    public class DataGridRowGroupHeader : TemplatedControl
    {
        private const string DATAGRIDROWGROUPHEADER_expanderButton = "ExpanderButton";
        private const string DATAGRIDROWGROUPHEADER_indentSpacer = "IndentSpacer";
        private const string DATAGRIDROWGROUPHEADER_itemCountElement = "ItemCountElement";
        private const string DATAGRIDROWGROUPHEADER_propertyNameElement = "PropertyNameElement";

        private bool _areIsCheckedHandlersSuspended;
        private ToggleButton _expanderButton;
        private DataGridRowHeader _headerElement;
        private Control _indentSpacer;
        private TextBlock _itemCountElement;
        private TextBlock _propertyNameElement;
        private Panel _rootElement;
        private double _totalIndent;

        public static readonly StyledProperty<bool> IsItemCountVisibleProperty =
            AvaloniaProperty.Register<DataGridRowGroupHeader, bool>(nameof(IsItemCountVisible));

        /// <summary>
        /// Gets or sets a value that indicates whether the item count is visible.
        /// </summary>
        public bool IsItemCountVisible
        {
            get { return GetValue(IsItemCountVisibleProperty); }
            set { SetValue(IsItemCountVisibleProperty, value); }
        }

        public static readonly StyledProperty<string> PropertyNameProperty =
            AvaloniaProperty.Register<DataGridRowGroupHeader, string>(nameof(PropertyName));

        /// <summary>
        /// Gets or sets the name of the property that this <see cref="T:Avalonia.Controls.DataGrid" /> row is bound to. 
        /// </summary>
        public string PropertyName
        {
            get { return GetValue(PropertyNameProperty); }
            set { SetValue(PropertyNameProperty, value); }
        }

        public static readonly StyledProperty<bool> IsPropertyNameVisibleProperty =
            AvaloniaProperty.Register<DataGridRowGroupHeader, bool>(nameof(IsPropertyNameVisible));

        /// <summary>
        /// Gets or sets a value that indicates whether the property name is visible.
        /// </summary>
        public bool IsPropertyNameVisible
        {
            get { return GetValue(IsPropertyNameVisibleProperty); }
            set { SetValue(IsPropertyNameVisibleProperty, value); }
        }

        public static readonly StyledProperty<double> SublevelIndentProperty =
            AvaloniaProperty.Register<DataGridRowGroupHeader, double>(
                nameof(SublevelIndent),
                defaultValue: DataGrid.DATAGRID_defaultRowGroupSublevelIndent,
                validate: ValidateSublevelIndent);

        private static double ValidateSublevelIndent(DataGridRowGroupHeader header, double value)
        {
            // We don't need to revert to the old value if our input is bad because we never read this property value
            if (double.IsNaN(value))
            {
                throw DataGridError.DataGrid.ValueCannotBeSetToNAN(nameof(SublevelIndent));
            }
            else if (double.IsInfinity(value))
            {
                throw DataGridError.DataGrid.ValueCannotBeSetToInfinity(nameof(SublevelIndent));
            }
            else if (value < 0)
            {
                throw DataGridError.DataGrid.ValueMustBeGreaterThanOrEqualTo(nameof(value), nameof(SublevelIndent), 0);
            }

            return value;
        }

        /// <summary>
        /// Gets or sets a value that indicates the amount that the 
        /// children of the <see cref="T:Avalonia.Controls.RowGroupHeader" /> are indented. 
        /// </summary>
        public double SublevelIndent
        {
            get { return GetValue(SublevelIndentProperty); }
            set { SetValue(SublevelIndentProperty, value); }
        }

        private void OnSublevelIndentChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (OwningGrid != null)
            {
                OwningGrid.OnSublevelIndentUpdated(this, (double)e.NewValue);
            }
        }

        static DataGridRowGroupHeader()
        {
            SublevelIndentProperty.Changed.AddClassHandler<DataGridRowGroupHeader>(x => x.OnSublevelIndentChanged);
        }

        /// <summary>
        /// Constructs a DataGridRowGroupHeader
        /// </summary>
        public DataGridRowGroupHeader()
        {
            //DefaultStyleKey = typeof(DataGridRowGroupHeader);
            AddHandler(InputElement.PointerPressedEvent, (s, e) => DataGridRowGroupHeader_PointerPressed(e), handledEventsToo: true);
        }

        internal DataGridRowHeader HeaderCell
        {
            get
            {
                return _headerElement;
            }
        }

        private bool IsCurrent
        {
            get
            {
                Debug.Assert(OwningGrid != null);
                return (RowGroupInfo.Slot == OwningGrid.CurrentSlot);
            }
        }

        private bool IsMouseOver
        {
            get;
            set;
        }

        internal bool IsRecycled
        {
            get;
            set;
        }

        internal int Level
        {
            get;
            set;
        }

        internal DataGrid OwningGrid
        {
            get;
            set;
        }

        internal DataGridRowGroupInfo RowGroupInfo
        {
            get;
            set;
        }

        internal double TotalIndent
        {
            set
            {
                _totalIndent = value;
                if (_indentSpacer != null)
                {
                    _indentSpacer.Width = _totalIndent;
                }
            }
        }

        private IDisposable _expanderButtonSubscription;

        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            _rootElement = e.NameScope.Find<Panel>(DataGridRow.DATAGRIDROW_elementRoot);

            _expanderButtonSubscription?.Dispose();
            _expanderButton = e.NameScope.Find<ToggleButton>(DATAGRIDROWGROUPHEADER_expanderButton);
            if(_expanderButton != null)
            {
                EnsureExpanderButtonIsChecked();
                _expanderButtonSubscription =
                    _expanderButton.GetObservable(ToggleButton.IsCheckedProperty)
                                   .Skip(1)
                                   .Subscribe(v => OnExpanderButtonIsCheckedChanged(v));
            }

            _headerElement = e.NameScope.Find<DataGridRowHeader>(DataGridRow.DATAGRIDROW_elementRowHeader);
            if(_headerElement != null)
            {
                _headerElement.Owner = this;
                EnsureHeaderVisibility();
            }

            _indentSpacer = e.NameScope.Find<Control>(DATAGRIDROWGROUPHEADER_indentSpacer);
            if(_indentSpacer != null)
            {
                _indentSpacer.Width = _totalIndent;
            }

            _itemCountElement = e.NameScope.Find<TextBlock>(DATAGRIDROWGROUPHEADER_itemCountElement);
            _propertyNameElement = e.NameScope.Find<TextBlock>(DATAGRIDROWGROUPHEADER_propertyNameElement);
            UpdateTitleElements();

            base.OnTemplateApplied(e);
        }

        internal void ApplyHeaderStatus()
        {
            if (_headerElement != null && OwningGrid.AreRowHeadersVisible)
            {
                _headerElement.ApplyOwnerStatus();
            }
        }

        //TODO Implement
        internal void ApplyState(bool useTransitions)
        {

        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (OwningGrid == null)
            {
                return base.ArrangeOverride(finalSize);
            }

            Size size = base.ArrangeOverride(finalSize);
            if (_rootElement != null)
            {
                if (OwningGrid.AreRowGroupHeadersFrozen)
                {
                    foreach (Control child in _rootElement.Children)
                    {
                        child.Clip = null;
                    }
                }
                else
                {
                    double frozenLeftEdge = 0;
                    foreach (Control child in _rootElement.Children)
                    {
                        if (DataGridFrozenGrid.GetIsFrozen(child) && child.IsVisible)
                        {
                            TranslateTransform transform = new TranslateTransform();
                            // Automatic layout rounding doesn't apply to transforms so we need to Round this
                            transform.X = Math.Round(OwningGrid.HorizontalOffset);
                            child.RenderTransform = transform;

                            double childLeftEdge = child.Translate(this, new Point(child.Bounds.Width, 0)).X - transform.X;
                            frozenLeftEdge = Math.Max(frozenLeftEdge, childLeftEdge + OwningGrid.HorizontalOffset);
                        }
                    }
                    // Clip the non-frozen elements so they don't overlap the frozen ones
                    foreach (Control child in _rootElement.Children)
                    {
                        if (!DataGridFrozenGrid.GetIsFrozen(child))
                        {
                            EnsureChildClip(child, frozenLeftEdge);
                        }
                    }
                }
            }
            return size;
        }

        internal void ClearFrozenStates()
        {
            if (_rootElement != null)
            {
                foreach (Control child in _rootElement.Children)
                {
                    child.RenderTransform = null;
                }
            }
        }

        //TODO TabStop
        private void DataGridRowGroupHeader_PointerPressed(PointerPressedEventArgs e)
        {
            if (OwningGrid != null && e.MouseButton == MouseButton.Left)
            {
                if (OwningGrid.IsDoubleClickRecordsClickOnCall(this) && !e.Handled)
                {
                    ToggleExpandCollapse(!RowGroupInfo.IsVisible, true);
                    e.Handled = true;
                }
                else
                {
                    //if (!e.Handled && OwningGrid.IsTabStop)
                    if (!e.Handled)
                    {
                        OwningGrid.Focus();
                    }
                    e.Handled = OwningGrid.UpdateStateOnMouseLeftButtonDown(e, OwningGrid.CurrentColumnIndex, RowGroupInfo.Slot, allowEdit: false);
                }
            }
        }

        private void EnsureChildClip(Visual child, double frozenLeftEdge)
        {
            double childLeftEdge = child.Translate(this, new Point(0, 0)).X;
            if (frozenLeftEdge > childLeftEdge)
            {
                double xClip = Math.Round(frozenLeftEdge - childLeftEdge);
                var rg = new RectangleGeometry();
                rg.Rect = 
                    new Rect(xClip, 0, 
                        Math.Max(0, child.Bounds.Width - xClip), 
                        child.Bounds.Height);
                child.Clip = rg;
            }
            else
            {
                child.Clip = null;
            }
        }

        internal void EnsureExpanderButtonIsChecked()
        {
            if (_expanderButton != null && RowGroupInfo != null && RowGroupInfo.CollectionViewGroup != null &&
                RowGroupInfo.CollectionViewGroup.ItemCount != 0)
            {
                SetIsCheckedNoCallBack(RowGroupInfo.IsVisible);
            }
        }

        //TODO Styles
        //internal void EnsureHeaderStyleAndVisibility(Style previousStyle)
        internal void EnsureHeaderVisibility()
        {
            if (_headerElement != null && OwningGrid != null)
            {
                _headerElement.IsVisible = OwningGrid.AreColumnHeadersVisible;
            }
        }

        private void OnExpanderButtonIsCheckedChanged(bool? value)
        {
            if(!_areIsCheckedHandlersSuspended)
            {
                ToggleExpandCollapse(value ?? false, true);
            }
        }

        internal void LoadVisualsForDisplay()
        {
            EnsureExpanderButtonIsChecked();
            EnsureHeaderVisibility();
            ApplyState(useTransitions: false);
            ApplyHeaderStatus();
        }

        protected override void OnPointerEnter(PointerEventArgs e)
        {
            if (IsEnabled)
            {
                IsMouseOver = true;
                ApplyState(useTransitions: true);
            }

            base.OnPointerEnter(e);
        }

        protected override void OnPointerLeave(PointerEventArgs e)
        {
            if (IsEnabled)
            {
                IsMouseOver = false;
                ApplyState(useTransitions: true);
            }

            base.OnPointerLeave(e);
        }

        private void SetIsCheckedNoCallBack(bool value)
        {
            if (_expanderButton != null && _expanderButton.IsChecked != value)
            {
                _areIsCheckedHandlersSuspended = true;
                try
                {
                    _expanderButton.IsChecked = value;
                }
                finally
                {
                    _areIsCheckedHandlersSuspended = false;
                }
            }
        }

        internal void ToggleExpandCollapse(bool isVisible, bool setCurrent)
        {
            if (RowGroupInfo.CollectionViewGroup.ItemCount != 0)
            {
                if (OwningGrid == null)
                {
                    // Do these even if the OwningGrid is null in case it could improve the Designer experience for a standalone DataGridRowGroupHeader
                    RowGroupInfo.IsVisible = isVisible;
                }
                else if(RowGroupInfo.IsVisible != isVisible)
                {
                    OwningGrid.OnRowGroupHeaderToggled(this, isVisible, setCurrent);
                }

                EnsureExpanderButtonIsChecked();

                ApplyState(true /*useTransitions*/);
            }
        }

        internal void UpdateTitleElements()
        {
            if (_propertyNameElement != null)
            {
                string txt;
                if (string.IsNullOrWhiteSpace(PropertyName))
                    txt = String.Empty;
                else
                    txt = String.Format("{0}:", PropertyName);
                _propertyNameElement.Text = txt;
            }
            if (_itemCountElement != null && RowGroupInfo != null && RowGroupInfo.CollectionViewGroup != null)
            {
                string formatString;
                if(RowGroupInfo.CollectionViewGroup.ItemCount == 1)
                    formatString = "({0} Item)";
                else
                    formatString = "({0} Items)";

                _itemCountElement.Text = String.Format(formatString, RowGroupInfo.CollectionViewGroup.ItemCount);
            }
        }

    }
}