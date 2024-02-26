// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Controls.Metadata;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using System;
using System.Diagnostics;
using Avalonia.Reactive;

namespace Avalonia.Controls
{
    [TemplatePart(DATAGRIDROWGROUPHEADER_expanderButton,      typeof(ToggleButton))]
    [TemplatePart(DATAGRIDROWGROUPHEADER_indentSpacer,        typeof(Control))]
    [TemplatePart(DATAGRIDROWGROUPHEADER_itemCountElement,    typeof(TextBlock))]
    [TemplatePart(DATAGRIDROWGROUPHEADER_propertyNameElement, typeof(TextBlock))]
    [TemplatePart(DataGridRow.DATAGRIDROW_elementRoot,        typeof(Panel))]
    [TemplatePart(DataGridRow.DATAGRIDROW_elementRowHeader,   typeof(DataGridRowHeader))]
    [PseudoClasses(":pressed", ":current", ":expanded")]
    public class DataGridRowGroupHeader : TemplatedControl
    {
        private const string DATAGRIDROWGROUPHEADER_expanderButton = "PART_ExpanderButton";
        private const string DATAGRIDROWGROUPHEADER_indentSpacer = "PART_IndentSpacer";
        private const string DATAGRIDROWGROUPHEADER_itemCountElement = "PART_ItemCountElement";
        private const string DATAGRIDROWGROUPHEADER_propertyNameElement = "PART_PropertyNameElement";

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


        public static readonly StyledProperty<string> ItemCountFormatProperty =
            AvaloniaProperty.Register<DataGridRowGroupHeader, string>(nameof(ItemCountFormat));

        /// <summary>
        /// Gets or sets a value that indicates number format of items count
        /// </summary>
        public string ItemCountFormat
        {
            get { return GetValue(ItemCountFormatProperty); }
            set { SetValue(ItemCountFormatProperty, value); }
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
                validate: IsValidSublevelIndent);

        private static bool IsValidSublevelIndent(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0;
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
            SublevelIndentProperty.Changed.AddClassHandler<DataGridRowGroupHeader>((x,e) => x.OnSublevelIndentChanged(e));
            PressedMixin.Attach<DataGridRowGroupHeader>();
            IsTabStopProperty.OverrideDefaultValue<DataGridRowGroupHeader>(false);
        }

        /// <summary>
        /// Constructs a DataGridRowGroupHeader
        /// </summary>
        public DataGridRowGroupHeader()
        {
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

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
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
        }

        internal void ApplyHeaderStatus()
        {
            if (_headerElement != null && OwningGrid.AreRowHeadersVisible)
            {
                _headerElement.UpdatePseudoClasses();
            }
        }

        internal void UpdatePseudoClasses()
        {
            PseudoClasses.Set(":current", IsCurrent);

            if (RowGroupInfo?.CollectionViewGroup != null)
            {
                PseudoClasses.Set(":expanded", RowGroupInfo.IsVisible && RowGroupInfo.CollectionViewGroup.ItemCount > 0);
            }
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
            if (OwningGrid == null)
            {
                return;
            }
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                if (OwningGrid.IsDoubleClickRecordsClickOnCall(this) && !e.Handled)
                {
                    ToggleExpandCollapse(!RowGroupInfo.IsVisible, true);
                    e.Handled = true;
                }
                else
                {
                    if (!e.Handled && OwningGrid.IsTabStop)
                    {
                        OwningGrid.Focus();
                    }
                    e.Handled = OwningGrid.UpdateStateOnMouseLeftButtonDown(e, OwningGrid.CurrentColumnIndex, RowGroupInfo.Slot, allowEdit: false);
                }
            }
            else if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                if (!e.Handled)
                {
                    OwningGrid.Focus();
                }
                e.Handled = OwningGrid.UpdateStateOnMouseRightButtonDown(e, OwningGrid.CurrentColumnIndex, RowGroupInfo.Slot, allowEdit: false);
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

        internal void EnsureHeaderVisibility()
        {
            if (_headerElement != null && OwningGrid != null)
            {
                _headerElement.IsVisible = OwningGrid.AreRowHeadersVisible;
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
            UpdatePseudoClasses();
            ApplyHeaderStatus();
        }

        protected override void OnPointerEntered(PointerEventArgs e)
        {
            if (IsEnabled)
            {
                IsMouseOver = true;
                UpdatePseudoClasses();
            }

            base.OnPointerEntered(e);
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            if (IsEnabled)
            {
                IsMouseOver = false;
                UpdatePseudoClasses();
            }

            base.OnPointerExited(e);
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

                UpdatePseudoClasses();
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
                if (RowGroupInfo.CollectionViewGroup.ItemCount == 1)
                    formatString = (ItemCountFormat == null ? "({0} Item)" : ItemCountFormat);
                else
                    formatString = (ItemCountFormat == null ? "({0} Items)" : ItemCountFormat);

                _itemCountElement.Text = String.Format(formatString, RowGroupInfo.CollectionViewGroup.ItemCount);
            }
        }

    }
}
