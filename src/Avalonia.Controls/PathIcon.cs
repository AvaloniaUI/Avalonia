using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Avalonia.Controls
{
    public class PathIcon : TemplatedControl
    {
        static PathIcon()
        {
            AffectsRender<PathIcon>(SourceProperty);
        }

        public static readonly StyledProperty<Geometry> SourceProperty =
            AvaloniaProperty.Register<PathIcon, Geometry>(nameof(Source));

        public Geometry Source
        {
            get { return GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }
    }
}
