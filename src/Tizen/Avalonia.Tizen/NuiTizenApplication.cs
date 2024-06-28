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

    private class SingleViewLifetime : ISingleViewApplicationLifetime, ISingleTopLevelApplicationLifetime
    {
        public NuiAvaloniaView View { get; }

        public SingleViewLifetime(NuiAvaloniaView view)
        {
            View = view;
        }

        public Control? MainView
        {
            get => View.Content;
            set => View.Content = value;
        }

        public TopLevel? TopLevel => View.TopLevel;
    }

    protected virtual AppBuilder CreateAppBuilder() => AppBuilder.Configure<TApp>().UseTizen();
    protected virtual AppBuilder CustomizeAppBuilder(AppBuilder builder) => builder;

    protected override void OnCreate()
    {
        Logger.TryGet(LogEventLevel.Debug, LogKey)?.Log(null, "Creating application");

        base.OnCreate();
        TizenThreadingInterface.MainloopContext = SynchronizationContext.Current!;

        Logger.TryGet(LogEventLevel.Debug, LogKey)?.Log(null, "Setup view");
        _lifetime = new SingleViewLifetime(new NuiAvaloniaView());

        _lifetime.View.HeightResizePolicy = ResizePolicyType.FillToParent;
        _lifetime.View.WidthResizePolicy = ResizePolicyType.FillToParent;
        _lifetime.View.OnSurfaceInit += ContinueSetupApplication;

        Window.Instance.RenderingBehavior = RenderingBehaviorType.Continuously;
        Window.Instance.GetDefaultLayer().Add(_lifetime.View);
        Window.Instance.KeyEvent += (_, e) => _lifetime?.View.KeyboardHandler.Handle(e);
    }

    private void ContinueSetupApplication()
    {
        SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

        Logger.TryGet(LogEventLevel.Debug, LogKey)?.Log(null, "App builder");
        var builder = CreateAppBuilder();

        TizenThreadingInterface.MainloopContext.Post(_ =>
        {
            builder = CustomizeAppBuilder(builder);
            builder.AfterApplicationSetup(_ => _lifetime!.View.Initialise());

            Logger.TryGet(LogEventLevel.Debug, LogKey)?.Log(null, "Setup lifetime");
            builder.SetupWithLifetime(_lifetime!);
        }, null);
    }
}
