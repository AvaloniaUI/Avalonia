using Avalonia;
using Avalonia.Markup.Xaml;
using Generators.Sandbox.ViewModels;

namespace Generators.Sandbox;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Resources.MergedDictionaries.Add(new global::Sandbox.MyResources());
    }

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
