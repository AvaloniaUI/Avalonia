using Avalonia.Media;

namespace Avalonia.Controls
{
    public class PathIcon : IconElement
    {
        static PathIcon()
        {
            AffectsRender<PathIcon>(DataProperty);
        }

        public static readonly StyledProperty<Geometry> DataProperty =
            AvaloniaProperty.Register<PathIcon, Geometry>(nameof(Data));

        public Geometry Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }
    }
}
