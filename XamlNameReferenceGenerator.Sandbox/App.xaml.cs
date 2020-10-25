using Avalonia;
using Avalonia.Markup.Xaml;

namespace XamlNameReferenceGenerator.Sandbox
{
    public class App : Application
    {
        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            new SignUpView().Show();
            base.OnFrameworkInitializationCompleted();
        }
    }
}