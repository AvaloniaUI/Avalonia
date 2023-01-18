using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace ControlCatalog.Pages
{
    public class ThemePage : UserControl
    {
        public static ThemeVariant Pink { get; } = new("Pink", ThemeVariant.Light);
        
        public ThemePage()
        {
            AvaloniaXamlLoader.Load(this);

            var selector = this.FindControl<ComboBox>("Selector")!;
            var themeVariantScope = this.FindControl<ThemeVariantScope>("ThemeVariantScope")!;

            selector.Items = new[]
            {
                new ThemeVariant("Default"),
                ThemeVariant.Dark,
                ThemeVariant.Light,
                Pink
            };
            selector.SelectedIndex = 0;

            selector.SelectionChanged += (_, _) =>
            {
                if (selector.SelectedItem is ThemeVariant theme)
                {
                    themeVariantScope.RequestedThemeVariant = theme;
                }
            };
        }
    }
}
