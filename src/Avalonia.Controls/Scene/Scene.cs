using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Controls
{
    [Unstable]
    public abstract class Scene : ContentControl
    {
        public readonly static StyledProperty<Point> InitialPositionProperty =
            AvaloniaProperty.Register<Scene, Point>(nameof(InitialPosition));

        static Scene()
        {
            TopLevel.AutoSafeAreaPaddingProperty.OverrideDefaultValue<Scene>(false);
        }

        public Point InitialPosition
        {
            get => GetValue(InitialPositionProperty);
            set => SetValue(InitialPositionProperty, value);
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
            if(Host == null)
                Logging.Logger.Sink?.Log(Logging.LogEventLevel.Warning, Logging.LogArea.Control, this, "Scene.Close() called without a host attached");

            Host?.Close();

            Host = null;
        }
    }
}
