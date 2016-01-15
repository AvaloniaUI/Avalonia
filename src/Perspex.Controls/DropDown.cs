// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Perspex.Controls.Generators;
using Perspex.Controls.Primitives;
using Perspex.Controls.Shapes;
using Perspex.Controls.Templates;
using Perspex.Input;
using Perspex.Layout;
using Perspex.Media;
using Perspex.VisualTree;

namespace Perspex.Controls
{
    public class DropDown : SelectingItemsControl, IContentControl
    {
        public static readonly PerspexProperty<object> ContentProperty =
            ContentControl.ContentProperty.AddOwner<DropDown>();

        public static readonly PerspexProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
            ContentControl.HorizontalContentAlignmentProperty.AddOwner<DropDown>();

        public static readonly PerspexProperty<VerticalAlignment> VerticalContentAlignmentProperty =
            ContentControl.VerticalContentAlignmentProperty.AddOwner<DropDown>();

        public static readonly PerspexProperty<bool> IsDropDownOpenProperty =
            PerspexProperty.RegisterDirect<DropDown, bool>(
                nameof(IsDropDownOpen),
                o => o.IsDropDownOpen,
                (o, v) => o.IsDropDownOpen = v);

        public static readonly PerspexProperty<object> SelectionBoxItemProperty =
            PerspexProperty.Register<DropDown, object>("SelectionBoxItem");

        private bool _isDropDownOpen;
        private Popup _popup;

        static DropDown()
        {
            FocusableProperty.OverrideDefaultValue<DropDown>(true);
            SelectedItemProperty.Changed.AddClassHandler<DropDown>(x => x.SelectedItemChanged);
        }

        public DropDown()
        {
            Bind(ContentProperty, GetObservable(SelectedItemProperty));
        }

        public object Content
        {
            get { return GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public HorizontalAlignment HorizontalContentAlignment
        {
            get { return GetValue(HorizontalContentAlignmentProperty); }
            set { SetValue(HorizontalContentAlignmentProperty, value); }
        }

        public VerticalAlignment VerticalContentAlignment
        {
            get { return GetValue(VerticalContentAlignmentProperty); }
            set { SetValue(VerticalContentAlignmentProperty, value); }
        }

        public bool IsDropDownOpen
        {
            get { return _isDropDownOpen; }
            set { SetAndRaise(IsDropDownOpenProperty, ref _isDropDownOpen, value); }
        }

        public object SelectionBoxItem
        {
            get { return GetValue(SelectionBoxItemProperty); }
            set { SetValue(SelectionBoxItemProperty, value); }
        }

        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new ItemContainerGenerator<DropDownItem>(this, DropDownItem.ContentProperty);
        }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
            this.UpdateSelectionBoxItem(this.SelectedItem);
        }

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

        protected override void OnPointerPressed(PointerPressEventArgs e)
        {
            if (!IsDropDownOpen)
            {
                if (((IVisual)e.Source).GetVisualAncestors().Last().GetType() != typeof(PopupRoot))
                {
                    IsDropDownOpen = true;
                    e.Handled = true;
                }
            }

            if (!e.Handled)
            {
                if (UpdateSelectionFromEventSource(e.Source))
                {
                    e.Handled = true;
                }
            }

            base.OnPointerPressed(e);
        }

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

        private void SelectedItemChanged(PerspexPropertyChangedEventArgs e)
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
    }
}
