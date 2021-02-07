using Avalonia.Markup.Xaml;
using Avalonia.NameGenerator.Sandbox.Views;

namespace Avalonia.NameGenerator.Sandbox
{
    public class App : Application
    {
        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            var view = new SignUpView();
            view.Show();
            base.OnFrameworkInitializationCompleted();
        }
    }
}