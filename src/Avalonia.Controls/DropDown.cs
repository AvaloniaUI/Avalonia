// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{

    /// <summary>
    /// A drop-down list control.
    /// </summary>
    public class DropDown : SelectingItemsControl
    {
        /// <summary>
        /// Defines the <see cref="IsDropDownOpen"/> property.
        /// </summary>
        public static readonly DirectProperty<DropDown, bool> IsDropDownOpenProperty =
            AvaloniaProperty.RegisterDirect<DropDown, bool>(
                nameof(IsDropDownOpen),
                o => o.IsDropDownOpen,
                (o, v) => o.IsDropDownOpen = v);

        /// <summary>
        /// Defines the <see cref="MaxDropDownHeight"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MaxDropDownHeightProperty =
            AvaloniaProperty.Register<DropDown, double>(nameof(MaxDropDownHeight), 200);

        /// <summary>
        /// Defines the <see cref="SelectionBoxItem"/> property.
        /// </summary>
        public static readonly DirectProperty<DropDown, object> SelectionBoxItemProperty =
            AvaloniaProperty.RegisterDirect<DropDown, object>(nameof(SelectionBoxItem), o => o.SelectionBoxItem);

        private bool _isDropDownOpen;
        private Popup _popup;
        private object _selectionBoxItem;

        /// <summary>
        /// Initializes static members of the <see cref="DropDown"/> class.
        /// </summary>
        static DropDown()
        {
            FocusableProperty.OverrideDefaultValue<DropDown>(true);
            SelectedItemProperty.Changed.AddClassHandler<DropDown>(x => x.SelectedItemChanged);
            KeyDownEvent.AddClassHandler<DropDown>(x => x.OnKeyDown, Interactivity.RoutingStrategies.Tunnel);
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

        /// <inheritdoc/>
        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new ItemContainerGenerator<DropDownItem>(
                this,
                DropDownItem.ContentProperty,
                DropDownItem.ContentTemplateProperty);
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
                    if (++SelectedIndex >= ItemCount)
                        SelectedIndex = 0;

                    e.Handled = true;
                }
                else if (e.Key == Key.Up)
                {
                    if (--SelectedIndex < 0)
                        SelectedIndex = ItemCount - 1;

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
            }

            _popup = e.NameScope.Get<Popup>("PART_Popup");
            _popup.Opened += PopupOpened;
        }

        private void PopupOpened(object sender, EventArgs e)
        {
            var selectedIndex = SelectedIndex;

            if (selectedIndex != -1)
            {
                var container = ItemContainerGenerator.ContainerFromIndex(selectedIndex);
                container?.Focus();
            }
        }

        private void SelectedItemChanged(AvaloniaPropertyChangedEventArgs e)
        {
            UpdateSelectionBoxItem(e.NewValue);
        }

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
                SelectionBoxItem = item;
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
    }
}
