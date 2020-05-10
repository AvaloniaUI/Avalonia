using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia;

namespace ControlCatalog.Pages
{
    public class AutoCompleteBoxPage : UserControl
    {

        public static readonly StyledProperty<bool> SideBarEnabledProperty =
            AvaloniaProperty.Register<AutoCompleteBoxPage, bool>(nameof(SideBarEnabled), false);

        public bool SideBarEnabled
        {
            get => GetValue(SideBarEnabledProperty);
            set => SetValue(SideBarEnabledProperty, value);
        }

        public AutoCompleteBoxPage()
        {
            this.InitializeComponent();
            this.FindControl<Button>("sidebar_button").Click += delegate
            {
                SideBarEnabled = !SideBarEnabled;
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
