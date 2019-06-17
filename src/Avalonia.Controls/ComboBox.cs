// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// A drop-down list control.
    /// </summary>
    public class ComboBox : SelectingItemsControl
    {
        /// <summary>
        /// The default value for the <see cref="ItemsControl.ItemsPanel"/> property.
        /// </summary>
        private static readonly FuncTemplate<IPanel> DefaultPanel =
            new FuncTemplate<IPanel>(() => new VirtualizingStackPanel());

        /// <summary>
        /// Defines the <see cref="IsDropDownOpen"/> property.
        /// </summary>
        public static readonly DirectProperty<ComboBox, bool> IsDropDownOpenProperty =
            AvaloniaProperty.RegisterDirect<ComboBox, bool>(
                nameof(IsDropDownOpen),
                o => o.IsDropDownOpen,
                (o, v) => o.IsDropDownOpen = v);

        /// <summary>
        /// Defines the <see cref="MaxDropDownHeight"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MaxDropDownHeightProperty =
            AvaloniaProperty.Register<ComboBox, double>(nameof(MaxDropDownHeight), 200);

        /// <summary>
        /// Defines the <see cref="SelectionBoxItem"/> property.
        /// </summary>
        public static readonly DirectProperty<ComboBox, object> SelectionBoxItemProperty =
            AvaloniaProperty.RegisterDirect<ComboBox, object>(nameof(SelectionBoxItem), o => o.SelectionBoxItem);

        /// <summary>
        /// Defines the <see cref="VirtualizationMode"/> property.
        /// </summary>
        public static readonly StyledProperty<ItemVirtualizationMode> VirtualizationModeProperty =
            ItemsPresenter.VirtualizationModeProperty.AddOwner<ComboBox>();

        private bool _isDropDownOpen;
        private Popup _popup;
        private object _selectionBoxItem;
        private IDisposable _subscriptionsOnOpen;

        /// <summary>
        /// Initializes static members of the <see cref="ComboBox"/> class.
        /// </summary>
        static ComboBox()
        {
            ItemsPanelProperty.OverrideDefaultValue<ComboBox>(DefaultPanel);
            FocusableProperty.OverrideDefaultValue<ComboBox>(true);
            SelectedItemProperty.Changed.AddClassHandler<ComboBox>(x => x.SelectedItemChanged);
            KeyDownEvent.AddClassHandler<ComboBox>(x => x.OnKeyDown, Interactivity.RoutingStrategies.Tunnel);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the dropdown is currently open.
        /// </summary>
        public bool IsDropDownOpen
        {
            get { return _isDropDownOpen; }
            set { SetAndRaise(IsDropDownOpenProperty, ref _isDropDownOpen, value); }
        }

        /// <summary>
        /// Gets or sets the maximum height for the dropdown list.
        /// </summary>
        public double MaxDropDownHeight
        {
            get { return GetValue(MaxDropDownHeightProperty); }
            set { SetValue(MaxDropDownHeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets the item to display as the control's content.
        /// </summary>
        protected object SelectionBoxItem
        {
            get { return _selectionBoxItem; }
            set { SetAndRaise(SelectionBoxItemProperty, ref _selectionBoxItem, value); }
        }

        /// <summary>
        /// Gets or sets the virtualization mode for the items.
        /// </summary>
        public ItemVirtualizationMode VirtualizationMode
        {
            get { return GetValue(VirtualizationModeProperty); }
            set { SetValue(VirtualizationModeProperty, value); }
        }

        /// <inheritdoc/>
        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new ItemContainerGenerator<ComboBoxItem>(
                this,
                ComboBoxItem.ContentProperty,
                ComboBoxItem.ContentTemplateProperty);
        }

        /// <inheritdoc/>
        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
            this.UpdateSelectionBoxItem(this.SelectedItem);
        }

        /// <inheritdoc/>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled)
                return;

            if (e.Key == Key.F4 ||
                ((e.Key == Key.Down || e.Key == Key.Up) && ((e.Modifiers & InputModifiers.Alt) != 0)))
            {
                IsDropDownOpen = !IsDropDownOpen;
                e.Handled = true;
            }
            else if (IsDropDownOpen && e.Key == Key.Escape)
            {
                IsDropDownOpen = false;
                e.Handled = true;
            }
            else if (IsDropDownOpen && e.Key == Key.Enter)
            {
                SelectFocusedItem();
                IsDropDownOpen = false;
                e.Handled = true;
            }
            else if (!IsDropDownOpen)
            {
                if (e.Key == Key.Down)
                {
                    SelectNext();
                    e.Handled = true;
                }
                else if (e.Key == Key.Up)
                {
                    SelectPrev();
                    e.Handled = true;
                }
            }
            else if (IsDropDownOpen && SelectedIndex < 0 && ItemCount > 0 &&
                      (e.Key == Key.Up || e.Key == Key.Down))
            {
                var firstChild = Presenter?.Panel?.Children.FirstOrDefault(c => CanFocus(c));
                if (firstChild != null)
                {
                    firstChild.Focus(NavigationMethod.Directional);
                    e.Handled = true;
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);

            if (!e.Handled)
            {
                if (!IsDropDownOpen)
                {
                    if (IsFocused)
                    {
                        if (e.Delta.Y < 0)
                            SelectNext();
                        else
                            SelectPrev();

                        e.Handled = true;
                    }
                }
                else
                {
                    e.Handled = true;
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (!e.Handled)
            {
                if (_popup?.PopupRoot != null && ((IVisual)e.Source).GetVisualRoot() == _popup?.PopupRoot)
                {
                    if (UpdateSelectionFromEventSource(e.Source))
                    {
                        _popup?.Close();
                        e.Handled = true;
                    }
                }
                else
                {
                    IsDropDownOpen = !IsDropDownOpen;
                    e.Handled = true;
                }
            }

            base.OnPointerPressed(e);
        }

        /// <inheritdoc/>
        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            if (_popup != null)
            {
                _popup.Opened -= PopupOpened;
                _popup.Closed -= PopupClosed;
            }

            _popup = e.NameScope.Get<Popup>("PART_Popup");
            _popup.Opened += PopupOpened;
            _popup.Closed += PopupClosed;

            base.OnTemplateApplied(e);
        }

        internal void ItemFocused(ComboBoxItem dropDownItem)
        {
            if (IsDropDownOpen && dropDownItem.IsFocused && dropDownItem.IsArrangeValid)
            {
                dropDownItem.BringIntoView();
            }
        }

        private void PopupClosed(object sender, EventArgs e)
        {
            _subscriptionsOnOpen?.Dispose();
            _subscriptionsOnOpen = null;

            if (CanFocus(this))
            {
                Focus();
            }
        }

        private void PopupOpened(object sender, EventArgs e)
        {
            TryFocusSelectedItem();

            _subscriptionsOnOpen?.Dispose();
            _subscriptionsOnOpen = null;

            var toplevel = this.GetVisualRoot() as TopLevel;
            if (toplevel != null)
            {
                _subscriptionsOnOpen = toplevel.AddHandler(PointerWheelChangedEvent, (s, ev) =>
                {
                    //eat wheel scroll event outside dropdown popup while it's open
                    if (IsDropDownOpen && (ev.Source as IVisual).GetVisualRoot() == toplevel)
                    {
                        ev.Handled = true;
                    }
                }, Interactivity.RoutingStrategies.Tunnel);
            }
        }

        private void SelectedItemChanged(AvaloniaPropertyChangedEventArgs e)
        {
            UpdateSelectionBoxItem(e.NewValue);
            TryFocusSelectedItem();
        }

        private void TryFocusSelectedItem()
        {
            var selectedIndex = SelectedIndex;
            if (IsDropDownOpen && selectedIndex != -1)
            {
                var container = ItemContainerGenerator.ContainerFromIndex(selectedIndex);

                if (container == null && SelectedItems.Count > 0)
                {
                    ScrollIntoView(SelectedItems[0]);
                    container = ItemContainerGenerator.ContainerFromIndex(selectedIndex);
                }

                if (container != null && CanFocus(container))
                {
                    container.Focus();
                }
            }
        }

        private bool CanFocus(IControl control) => control.Focusable && control.IsEnabledCore && control.IsVisible;

        private void UpdateSelectionBoxItem(object item)
        {
            var contentControl = item as IContentControl;

            if (contentControl != null)
            {
                item = contentControl.Content;
            }

            var control = item as IControl;

            if (control != null)
            {
                control.Measure(Size.Infinity);

                SelectionBoxItem = new Rectangle
                {
                    Width = control.DesiredSize.Width,
                    Height = control.DesiredSize.Height,
                    Fill = new VisualBrush
                    {
                        Visual = control,
                        Stretch = Stretch.None,
                        AlignmentX = AlignmentX.Left,
                    }
                };
            }
            else
            {
                var selector = MemberSelector;
                SelectionBoxItem = selector != null ? selector.Select(item) : item;
            }
        }

        private void SelectFocusedItem()
        {
            foreach (ItemContainerInfo dropdownItem in ItemContainerGenerator.Containers)
            {
                if (dropdownItem.ContainerControl.IsFocused)
                {
                    SelectedIndex = dropdownItem.Index;
                    break;
                }
            }
        }

        private void SelectNext()
        {
            int next = SelectedIndex + 1;

            if (next >= ItemCount)
                next = 0;

            SelectedIndex = next;
        }

        private void SelectPrev()
        {
            int prev = SelectedIndex - 1;

            if (prev < 0)
                prev = ItemCount - 1;

            SelectedIndex = prev;
        }
    }
}
