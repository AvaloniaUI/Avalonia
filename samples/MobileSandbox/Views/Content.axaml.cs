using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MobileSandbox.Views
{
    public partial class Content : UserControl
    {
        public Content()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
