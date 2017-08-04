using System;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    public class ComboBox : SelectingItemsControl
    {
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
        /// Defines the <see cref="Text"/> property.
        /// </summary>
        public static readonly DirectProperty<ComboBox, string> TextProperty =
            AvaloniaProperty.RegisterDirect<ComboBox, string>(
                nameof(Text),
                o => o._text,
                (o, v) => o.Text = v);

        private string _text;
        private bool _isDropDownOpen;
        private Popup _popup;

        /// <summary>
        /// Initializes static members of the <see cref="ComboBox"/> class.
        /// </summary>
        static ComboBox()
        {
            FocusableProperty.OverrideDefaultValue<ComboBox>(true);
            SelectedItemProperty.Changed.AddClassHandler<ComboBox>(x => x.SelectedItemChanged);
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
        /// Gets or sets the text displayed in the <see cref="ComboBox"/>.
        /// </summary>
        public string Text
        {
            get { return GetValue(TextProperty); }
            set { SetAndRaise(TextProperty, ref _text, value); }
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
            this.UpdateSelectionBoxText(this.SelectedItem);
        }

        /// <inheritdoc/>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!e.Handled)
            {
                if (e.Key == Key.F4 ||
                    (e.Key == Key.Down && ((e.Modifiers & InputModifiers.Alt) != 0)))
                {
                    IsDropDownOpen = !IsDropDownOpen;
                    e.Handled = true;
                }
                else if (IsDropDownOpen && (e.Key == Key.Escape || e.Key == Key.Enter))
                {
                    IsDropDownOpen = false;
                    e.Handled = true;
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (!IsDropDownOpen && ((IVisual)e.Source).GetVisualRoot() != typeof(PopupRoot))
            {
                IsDropDownOpen = true;
                e.Handled = true;
            }

            if (!e.Handled)
            {
                if (UpdateSelectionFromEventSource(e.Source))
                {
                    _popup?.Close();
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
            UpdateSelectionBoxText(e.NewValue);
        }

        private void UpdateSelectionBoxText(object item)
        {
            if (item != null)
            {
                if (item is ComboBoxItem)
                {
                    item = ((ComboBoxItem)item).Content;
                    Contract.Requires<NotSupportedException>(!(item is IControl));
                }
                
                Text = Convert.ToString(item);
            }
        }
    }
}
