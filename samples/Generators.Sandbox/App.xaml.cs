using Avalonia;
using Avalonia.Markup.Xaml;
using Generators.Sandbox.ViewModels;

namespace Generators.Sandbox;

public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var view = new Views.SignUpView
        {
            ViewModel = new SignUpViewModel()
        };
        view.Show();
        base.OnFrameworkInitializationCompleted();
    }
}