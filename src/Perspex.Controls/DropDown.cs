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
            PerspexProperty.Register<DropDown, bool>("IsDropDownOpen");

        public static readonly PerspexProperty<object> SelectionBoxItemProperty =
            PerspexProperty.Register<DropDown, object>("SelectionBoxItem");

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
            get { return GetValue(IsDropDownOpenProperty); }
            set { SetValue(IsDropDownOpenProperty, value); }
        }

        public object SelectionBoxItem
        {
            get { return GetValue(SelectionBoxItemProperty); }
            set { SetValue(SelectionBoxItemProperty, value); }
        }

        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new ItemContainerGenerator<ListBoxItem>(this);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!e.Handled)
            {
                if (e.Key == Key.F4 ||
                    (e.Key == Key.Down && ((e.Modifiers & ModifierKeys.Alt) != 0)))
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
                }
            }

            base.OnPointerPressed(e);
        }

        protected override void OnTemplateApplied()
        {
            if (_popup != null)
            {
                _popup.Opened -= PopupOpened;
            }

            _popup = this.GetTemplateChild<Popup>("popup");
            _popup.Opened += PopupOpened;
        }

        private void PopupOpened(object sender, EventArgs e)
        {
            var selectedIndex = SelectedIndex;

            if (selectedIndex != -1)
            {
                var container = ItemContainerGenerator.ContainerFromIndex(selectedIndex);
                container.Focus();
            }
        }

        private void SelectedItemChanged(PerspexPropertyChangedEventArgs e)
        {
            var control = e.NewValue as IControl;

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
                SelectionBoxItem = e.NewValue;
            }
        }
    }
}
