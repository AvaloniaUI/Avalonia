using Avalonia;
using Avalonia.Markup.Xaml;
using XamlTestApplication.Views;

namespace XamlTestApplication
{
    public class XamlTestApp : Application
    {
        public XamlTestApp()
        {
            RegisterServices();
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
