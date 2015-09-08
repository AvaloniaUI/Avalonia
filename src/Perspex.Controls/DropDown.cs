





namespace Perspex.Controls
{
    using System;
    using System.Linq;
    using Generators;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Shapes;
    using Perspex.Controls.Templates;
    using Perspex.Input;
    using Perspex.Layout;
    using Perspex.Media;
    using Perspex.VisualTree;

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

        private Popup popup;

        static DropDown()
        {
            FocusableProperty.OverrideDefaultValue<DropDown>(true);
            SelectedItemProperty.Changed.AddClassHandler<DropDown>(x => x.SelectedItemChanged);
        }

        public DropDown()
        {
            this.Bind(ContentProperty, this.GetObservable(DropDown.SelectedItemProperty));
        }

        public object Content
        {
            get { return this.GetValue(ContentProperty); }
            set { this.SetValue(ContentProperty, value); }
        }

        public HorizontalAlignment HorizontalContentAlignment
        {
            get { return this.GetValue(HorizontalContentAlignmentProperty); }
            set { this.SetValue(HorizontalContentAlignmentProperty, value); }
        }

        public VerticalAlignment VerticalContentAlignment
        {
            get { return this.GetValue(VerticalContentAlignmentProperty); }
            set { this.SetValue(VerticalContentAlignmentProperty, value); }
        }

        public bool IsDropDownOpen
        {
            get { return this.GetValue(IsDropDownOpenProperty); }
            set { this.SetValue(IsDropDownOpenProperty, value); }
        }

        public object SelectionBoxItem
        {
            get { return this.GetValue(SelectionBoxItemProperty); }
            set { this.SetValue(SelectionBoxItemProperty, value); }
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
                    this.IsDropDownOpen = !this.IsDropDownOpen;
                    e.Handled = true;
                }
                else if (this.IsDropDownOpen && (e.Key == Key.Escape || e.Key == Key.Enter))
                {
                    this.IsDropDownOpen = false;
                    e.Handled = true;
                }
            }
        }

        protected override void OnPointerPressed(PointerPressEventArgs e)
        {
            if (!this.IsDropDownOpen)
            {
                if (((IVisual)e.Source).GetVisualAncestors().Last().GetType() != typeof(PopupRoot))
                {
                    this.IsDropDownOpen = true;
                }
            }

            base.OnPointerPressed(e);
        }

        protected override void OnTemplateApplied()
        {
            if (this.popup != null)
            {
                this.popup.Opened -= this.PopupOpened;
            }

            this.popup = this.GetTemplateChild<Popup>("popup");
            this.popup.Opened += this.PopupOpened;
        }

        private void PopupOpened(object sender, EventArgs e)
        {
            var selectedIndex = this.SelectedIndex;

            if (selectedIndex != -1)
            {
                var container = this.ItemContainerGenerator.ContainerFromIndex(selectedIndex);
                container.Focus();
            }
        }

        private void SelectedItemChanged(PerspexPropertyChangedEventArgs e)
        {
            var control = e.NewValue as IControl;

            if (control != null)
            {
                this.SelectionBoxItem = new Rectangle
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
                this.SelectionBoxItem = e.NewValue;
            }
        }
    }
}
