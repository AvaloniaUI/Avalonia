#nullable enable
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Metadata;

namespace Avalonia.Controls
{
    public class IconSourceElement : IconElement
    {
        public static readonly StyledProperty<IconSource?> IconSourceProperty =
            AvaloniaProperty.Register<IconSourceElement, IconSource?>(nameof(IconSource));

        [Content]
        public IconSource? IconSource
        {
            get => GetValue(IconSourceProperty);
            set => SetValue(IconSourceProperty, value);
        }
    }

    public abstract class IconSource : AvaloniaObject
    {
        public static StyledProperty<IBrush?> ForegroundProperty =
            TemplatedControl.ForegroundProperty.AddOwner<IconSource>();

        public IBrush? Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public abstract IDataTemplate IconElementTemplate { get; }
    }
}
