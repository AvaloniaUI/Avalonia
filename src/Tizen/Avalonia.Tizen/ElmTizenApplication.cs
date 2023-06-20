using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ElmSharp;
using Tizen.Applications;
using Window = ElmSharp.Window;

namespace Avalonia.Tizen;
public class ElmTizenApplication<TApp> : CoreUIApplication
    where TApp : Application, new()
{
    private ElmAvaloniaView _view;
    private ElmTizenApplication<TApp>.SingleViewLifetime _lifetime;
    private Window _window;

    class SingleViewLifetime : ISingleViewApplicationLifetime
    {
        public ElmAvaloniaView View;

        public Control MainView
        {
            get => View.Content;
            set => View.Content = value;
        }
    }

    protected virtual AppBuilder CustomizeAppBuilder(AppBuilder builder) => builder;

    protected override void OnCreate()
    {
        base.OnCreate();

        _window = new Window("Avalonia");
        //_window.BackButtonPressed += OnBackButtonPressed;
        _window.AvailableRotations = DisplayRotation.Degree_0 | DisplayRotation.Degree_180 | DisplayRotation.Degree_270 | DisplayRotation.Degree_90;
        _window.Active();
        _window.Show();

        _lifetime = new SingleViewLifetime();
        _lifetime.View = _view = new ElmAvaloniaView(_window);
        _view.RenderingMode = Platform.RenderingMode.Continuously;
        //_view.IgnorePixelScaling = true;
        _view.Show();

        var conformant = new Conformant(_window);
        conformant.Show();
        conformant.SetContent(_view);

        var builder = AppBuilder.Configure<TApp>().UseTizen();
        CustomizeAppBuilder(builder);

        builder.AfterSetup(_ =>
        {
            _view.Initialise();
        });

        builder.SetupWithLifetime(_lifetime);
    }
}
