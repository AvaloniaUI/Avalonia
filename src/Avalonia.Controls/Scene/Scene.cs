using System;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Metadata;

namespace Avalonia.Controls
{
    [Unstable]
    public abstract class Scene : ContentControl
    {
        public readonly static StyledProperty<Point> InitialPositionProperty =
            AvaloniaProperty.Register<Scene, Point>(nameof(InitialPosition));

        public static readonly RoutedEvent<ApplicationLifetimeStateEventArgs> SaveStateEvent =
            RoutedEvent.Register<Scene, ApplicationLifetimeStateEventArgs>(nameof(SaveState), RoutingStrategies.Direct);

        public static readonly RoutedEvent<ApplicationLifetimeStateEventArgs> RestoreStateEvent =
            RoutedEvent.Register<Scene, ApplicationLifetimeStateEventArgs>(nameof(RestoreState), RoutingStrategies.Direct);

        static Scene()
        {
            TopLevel.AutoSafeAreaPaddingProperty.OverrideDefaultValue<Scene>(false);
        }

        public Point InitialPosition
        {
            get => GetValue(InitialPositionProperty);
            set => SetValue(InitialPositionProperty, value);
        }

        public event EventHandler<ApplicationLifetimeStateEventArgs>? SaveState
        {
            add => AddHandler(SaveStateEvent, value);
            remove => RemoveHandler(SaveStateEvent, value);
        }

        public event EventHandler<ApplicationLifetimeStateEventArgs>? RestoreState
        {
            add => AddHandler(RestoreStateEvent, value);
            remove => RemoveHandler(RestoreStateEvent, value);
        }

        internal ISceneHost? Host { get; private set; }

        public Task<bool> ShowAsync()
        {
            var manager = AvaloniaLocator.Current.GetService<ISceneManager>();
            return manager?.ShowScene(this) ?? Task.FromResult(false);
        }

        internal void AttachHost(ISceneHost host)
        {
            Host = host;
        }

        public void Close()
        {
            if (Host == null)
                Logging.Logger.Sink?.Log(Logging.LogEventLevel.Warning, Logging.LogArea.Control, this, "Scene.Close() called without a host attached");

            Host?.Close();

            Host = null;
        }
    }
}
