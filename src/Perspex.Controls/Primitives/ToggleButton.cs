





namespace Perspex.Controls.Primitives
{
    using System;
    using Perspex.Interactivity;

    public class ToggleButton : Button
    {
        public static readonly PerspexProperty<bool> IsCheckedProperty =
            PerspexProperty.Register<ToggleButton, bool>("IsChecked");

        static ToggleButton()
        {
            Control.PseudoClass(IsCheckedProperty, ":checked");
        }

        public ToggleButton()
        {
        }

        public bool IsChecked
        {
            get { return this.GetValue(IsCheckedProperty); }
            set { this.SetValue(IsCheckedProperty, value); }
        }

        protected override void OnClick(RoutedEventArgs e)
        {
            this.Toggle();
        }

        protected virtual void Toggle()
        {
            this.IsChecked = !this.IsChecked;
        }
    }
}
