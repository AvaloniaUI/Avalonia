using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Tizen.NUI;
using Window = Tizen.NUI.Window;
using Avalonia.Logging;

namespace Avalonia.Tizen;

public class NuiTizenApplication<TApp> : NUIApplication
    where TApp : Application, new()
{
    private const string LogKey = "TIZENAPP";

    private SingleViewLifetime? _lifetime;

    class SingleViewLifetime : ISingleViewApplicationLifetime
    {
        public NuiAvaloniaView View { get; }

        public SingleViewLifetime(NuiAvaloniaView view)
        {
            View = view;
        }

        public Control MainView
        {
            get => View.Content;
            set => View.Content = value;
        }
    }

    protected virtual AppBuilder CustomizeAppBuilder(AppBuilder builder) => builder;

    protected override void OnCreate()
    {
        Logger.TryGet(LogEventLevel.Debug, LogKey)?.Log(null, "Creating application");

        base.OnCreate();
#pragma warning disable CS8601 // Possible null reference assignment.
        TizenThreadingInterface.MainloopContext = SynchronizationContext.Current;
#pragma warning restore CS8601 // Possible null reference assignment.

        Logger.TryGet(LogEventLevel.Debug, LogKey)?.Log(null, "Setup view");
        _lifetime = new SingleViewLifetime(new NuiAvaloniaView());

        _lifetime.View.HeightResizePolicy = ResizePolicyType.FillToParent;
        _lifetime.View.WidthResizePolicy = ResizePolicyType.FillToParent;

        Window.Instance.GetDefaultLayer().Add(_lifetime.View);
        Window.Instance.RenderingBehavior = RenderingBehaviorType.Continuously;
        Window.Instance.KeyEvent += (s, e) => _lifetime?.View?.KeyboardHandler.Handle(e);

        Logger.TryGet(LogEventLevel.Debug, LogKey)?.Log(null, "App builder");
        var builder = AppBuilder.Configure<TApp>().UseTizen();
        CustomizeAppBuilder(builder);

        builder.AfterSetup(_ => _lifetime.View.Initialise());

        Logger.TryGet(LogEventLevel.Debug, LogKey)?.Log(null, "Setup lifetime");
        builder.SetupWithLifetime(_lifetime);
    }
}
