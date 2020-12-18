using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class AcrylicPage : UserControl
    {
        public static readonly StyledProperty<bool> ButtonEnableProperty = AvaloniaProperty.Register<AcrylicPage, bool>("ButtonEnable");

        public AcrylicPage()
        {
            this.InitializeComponent();
            this.DataContext = this;
            
            
        }

        public bool ButtonEnable
        {
            get { return GetValue(ButtonEnableProperty); }
            set { SetValue(ButtonEnableProperty, value); }
        }


        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
