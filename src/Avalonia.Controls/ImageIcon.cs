using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace Avalonia.Controls
{
    public class ImageIcon : IconElement
    {
        /// <summary>
        /// Defines the <see cref="Source"/> property.
        /// </summary>
        public static readonly StyledProperty<IImage> SourceProperty =
            Image.SourceProperty.AddOwner<ImageIcon>();

        /// <summary>
        /// Gets or sets the image that will be displayed.
        /// </summary>
        public IImage Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }
    }

    public class ImageIconSource : IconSource
    {
        /// <summary>
        /// Defines the <see cref="Source"/> property.
        /// </summary>
        public static readonly StyledProperty<IImage> SourceProperty =
            Image.SourceProperty.AddOwner<ImageIcon>();

        /// <summary>
        /// Gets or sets the image that will be displayed.
        /// </summary>
        public IImage Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public override IDataTemplate IconElementTemplate { get; } = new FuncDataTemplate<ImageIconSource>((source, _) => new ImageIcon
        {
            [!ForegroundProperty] = source[!ForegroundProperty],
            [!SourceProperty] = source[!SourceProperty]
        });
    }
}
