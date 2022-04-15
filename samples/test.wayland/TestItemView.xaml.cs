using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BindingDemo
{
    public class TestItemView : UserControl
    {
        public TestItemView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
